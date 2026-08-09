using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Shipments.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Shipments.Commands;

/// <summary>
/// TASK 09 - Mục 9.1: Handler Processor tạo vận đơn cho Parent Batch (gọi SC shipParent).
///   1. Validate Batch tồn tại &amp; Processor sở hữu &amp; ở PACKAGED (BR-18).
///   2. Validate Retailer tồn tại &amp; có role RETAILER &amp; APPROVED.
///   3. Upload Metadata JSON vận chuyển lên IPFS ➔ (MetadataURI, DataHash).
///   4. Processor gọi SC shipParent(batchId, metadataURI, dataHash).
///   5. Lưu Shipment (AssetType=PARENT) + cập nhật Batch.CurrentStage = STAGE_SHIPPING.
/// </summary>
public class ShipParentCommandHandler
    : IRequestHandler<ShipParentCommand, ShipmentResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<ShipParentCommandHandler> _logger;

    public ShipParentCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        ILogger<ShipParentCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<ShipmentResponseDto> Handle(
        ShipParentCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // ========== 1. Validate input ==========
        if (req.Input is null)
            throw new ValidationException("Thiếu thông tin vận chuyển.");
        if (string.IsNullOrWhiteSpace(req.Input.PickupLocation))
            throw new ValidationException("PickupLocation không được trống.");
        if (string.IsNullOrWhiteSpace(req.Input.Destination))
            throw new ValidationException("Destination không được trống.");
        if (string.IsNullOrWhiteSpace(req.Input.CarrierInfo))
            throw new ValidationException("CarrierInfo không được trống.");
        if (string.IsNullOrWhiteSpace(req.Input.ShippingCode))
            throw new ValidationException("ShippingCode không được trống.");
        if (req.Input.Weight <= 0)
            throw new ValidationException("Weight phải > 0.");
        if (req.Input.RetailerId == Guid.Empty)
            throw new ValidationException("RetailerId không hợp lệ.");

        // ========== 2. Validate Batch (BR-18) ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền vận chuyển Batch của Processor khác.");

        if (batch.CurrentStage != BatchStage.PACKAGED)
            throw new ValidationException(
                $"Batch hiện ở trạng thái {batch.CurrentStage}, " +
                "không thể vận chuyển (yêu cầu PACKAGED - BR-18).");

        // ========== 3. Validate Retailer ==========
        var retailer = await _uow.Users.GetByIdAsync(req.Input.RetailerId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Retailer {req.Input.RetailerId}.");

        if (retailer.Role?.RoleName != RoleType.RETAILER)
            throw new ValidationException("Người dùng được chỉ định không phải RETAILER.");
        if (retailer.Status != UserStatus.APPROVED)
            throw new ValidationException("Retailer chưa được duyệt tài khoản.");

        // ========== 4. Upload Metadata JSON lên IPFS ==========
        var now = DateTime.UtcNow;
        var metadata = new
        {
            assetType = "PARENT",
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            shippedByProcessorId = processorId,
            retailerId = retailer.Id,
            retailerName = retailer.FullName,
            pickupLocation = req.Input.PickupLocation,
            destination = req.Input.Destination,
            carrierInfo = req.Input.CarrierInfo,
            shippingCode = req.Input.ShippingCode,
            shippingDate = req.Input.ShippingDate,
            expectedDate = req.Input.ExpectedDate,
            weight = req.Input.Weight,
            createdAt = now,
        };

        var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
            metadata,
            fileName: $"ship-parent-{batch.BatchCode}-{now:yyyyMMddHHmmss}.json",
            ct: ct);

        // ========== 5. Gọi SC: shipParent(batchId, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.ShipParentAsync(
                batchId: batch.Id.ToString(),
                metadataURI: metadataURI,
                dataHash: dataHash,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "shipParent on-chain thất bại cho Batch {BatchId}.", batch.Id);
            throw;
        }

        // ========== 6. Lưu Shipment + cập nhật CurrentStage ==========
        var shipment = new Shipment
        {
            AssetType = AssetType.PARENT,
            BatchId = batch.Id,
            RetailerId = retailer.Id,
            PickupLocation = req.Input.PickupLocation.Trim(),
            Destination = req.Input.Destination.Trim(),
            CarrierInfo = req.Input.CarrierInfo.Trim(),
            ShippingCode = req.Input.ShippingCode.Trim(),
            ShippingDate = req.Input.ShippingDate,
            ExpectedDate = req.Input.ExpectedDate,
            ReceivedDate = null,
            Weight = req.Input.Weight,
            MetadataURI = metadataURI,
            DataHash = dataHash,
            ShipTransactionHash = txHash,
        };
        await _uow.Shipments.AddAsync(shipment, ct);

        batch.CurrentStage = BatchStage.STAGE_SHIPPING;
        _uow.Batches.Update(batch);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ShipParent OK: Batch {BatchCode} ➔ Retailer {Retailer}, TxHash={TxHash}, NewStage={Stage}",
            batch.BatchCode, retailer.FullName, txHash, batch.CurrentStage);

        return new ShipmentResponseDto(
            ShipmentId: shipment.Id,
            AssetType: AssetType.PARENT.ToString(),
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            SubBatchId: null,
            SubBatchCode: null,
            RetailerId: retailer.Id,
            RetailerName: retailer.FullName,
            PickupLocation: shipment.PickupLocation,
            Destination: shipment.Destination,
            CarrierInfo: shipment.CarrierInfo,
            ShippingCode: shipment.ShippingCode,
            ShippingDate: shipment.ShippingDate,
            ExpectedDate: shipment.ExpectedDate,
            Weight: shipment.Weight,
            MetadataURI: shipment.MetadataURI,
            DataHash: shipment.DataHash,
            ShipTransactionHash: shipment.ShipTransactionHash,
            CurrentStage: batch.CurrentStage.ToString(),
            CreatedAt: shipment.CreatedAt);
    }
}