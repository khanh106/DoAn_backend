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
/// TASK 09 - Mục 9.1: Handler Processor tạo vận đơn cho SubBatch (gọi SC shipSub).
///   1. Validate SubBatch tồn tại &amp; Processor sở hữu Parent &amp; SubBatch ở PACKAGED (BR-18).
///   2. Validate Retailer tồn tại &amp; có role RETAILER &amp; APPROVED.
///   3. Upload Metadata JSON vận chuyển lên IPFS ➔ (MetadataURI, DataHash).
///   4. Processor gọi SC shipSub(subBatchId, metadataURI, dataHash).
///   5. Lưu Shipment (AssetType=SUB) + cập nhật SubBatch.CurrentStage = STAGE_SHIPPING.
/// </summary>
public class ShipSubCommandHandler
    : IRequestHandler<ShipSubCommand, ShipmentResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<ShipSubCommandHandler> _logger;

    public ShipSubCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        ILogger<ShipSubCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<ShipmentResponseDto> Handle(
        ShipSubCommand req, CancellationToken ct)
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

        // ========== 2. Validate SubBatch (BR-18) ==========
        var subBatch = await _uow.SubBatches.GetByIdAsync(req.SubBatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy SubBatch {req.SubBatchId}.");

        var parentBatch = subBatch.ParentBatch
            ?? throw new NotFoundException($"Không tìm thấy Parent Batch.");

        if (parentBatch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền vận chuyển SubBatch của Processor khác.");

        if (subBatch.CurrentStage != BatchStage.PACKAGED)
            throw new ValidationException(
                $"SubBatch hiện ở trạng thái {subBatch.CurrentStage}, " +
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
            assetType = "SUB",
            subBatchId = subBatch.Id,
            subBatchCode = subBatch.SubBatchCode,
            parentBatchId = parentBatch.Id,
            parentBatchCode = parentBatch.BatchCode,
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
            fileName: $"ship-sub-{subBatch.SubBatchCode}-{now:yyyyMMddHHmmss}.json",
            ct: ct);

        // ========== 5. Gọi SC: shipSub(subBatchId, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.ShipSubAsync(
                subBatchId: subBatch.Id.ToString(),
                metadataURI: metadataURI,
                dataHash: dataHash,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "shipSub on-chain thất bại cho SubBatch {SubBatchId}.", subBatch.Id);
            throw;
        }

        // ========== 6. Lưu Shipment + cập nhật CurrentStage ==========
        var shipment = new Shipment
        {
            AssetType = AssetType.SUB,
            SubBatchId = subBatch.Id,
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

        subBatch.CurrentStage = BatchStage.STAGE_SHIPPING;
        _uow.SubBatches.Update(subBatch);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ShipSub OK: SubBatch {SubBatchCode} ➔ Retailer {Retailer}, TxHash={TxHash}, NewStage={Stage}",
            subBatch.SubBatchCode, retailer.FullName, txHash, subBatch.CurrentStage);

        return new ShipmentResponseDto(
            ShipmentId: shipment.Id,
            AssetType: AssetType.SUB.ToString(),
            BatchId: null,
            BatchCode: parentBatch.BatchCode,
            SubBatchId: subBatch.Id,
            SubBatchCode: subBatch.SubBatchCode,
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
            CurrentStage: subBatch.CurrentStage.ToString(),
            CreatedAt: shipment.CreatedAt);
    }
}