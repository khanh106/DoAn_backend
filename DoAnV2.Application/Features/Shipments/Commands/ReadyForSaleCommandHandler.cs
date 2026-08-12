using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Shipments.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Shipments.Commands;

/// <summary>
/// TASK 09 - Mục 9.2: Handler Retailer xác nhận đưa sản phẩm lên kệ bán (gọi SC readyParent / readySub).
///   1. Validate Shipment tồn tại &amp; thuộc Retailer hiện tại.
///   2. Validate đã nhận hàng trước đó (ReceivedDate không null) &amp; Batch/SubBatch đang ở RECEIVED_AT_RETAILER.
///   3. Upload xác nhận sẵn sàng bán JSON lên IPFS ➔ (MetadataURI, DataHash).
///   4. Retailer gọi SC readyParent hoặc readySub tương ứng.
///   5. Cập nhật Shipment.Ready* + ReadyForSaleDate + Batch.CurrentStage = READY_FOR_SALE.
/// </summary>
public class ReadyForSaleCommandHandler
    : IRequestHandler<ReadyForSaleCommand, RetailerActionResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<ReadyForSaleCommandHandler> _logger;

    public ReadyForSaleCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<ReadyForSaleCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
        _logger = logger;
    }

    public async Task<RetailerActionResponseDto> Handle(
        ReadyForSaleCommand req, CancellationToken ct)
    {
        var retailerId = Guard.RequireRetailer(_currentUser);

        // ========== 1. Validate Shipment ==========
        var shipment = await _uow.Shipments.GetByIdAsync(req.ShipmentId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Shipment {req.ShipmentId}.");

        if (shipment.RetailerId != retailerId)
            throw new ForbiddenException("Shipment này không được giao cho bạn.");

        if (!shipment.ReceivedDate.HasValue)
            throw new ValidationException("Shipment chưa được xác nhận tiếp nhận.");
        if (shipment.ReadyForSaleDate.HasValue)
            throw new ValidationException("Shipment đã được đưa lên kệ trước đó.");

        // ========== Lấy và giải mã Private Key của ví Retailer ==========
        var retailerUser = await _uow.Users.GetByIdAsync(retailerId, ct)
            ?? throw new NotFoundException($"Không tìm thấy thông tin tài khoản Retailer {retailerId}.");

        string? signerPrivateKey = null;
        if (!string.IsNullOrWhiteSpace(retailerUser.EncryptedPrivateKey))
        {
            signerPrivateKey = _walletService.DecryptPrivateKey(
                retailerUser.EncryptedPrivateKey, _walletOptions.EncryptionKey);
        }

        // ========== 2. Xác định asset + validate stage ==========
        switch (shipment.AssetType)
        {
            case AssetType.PARENT:
            {
                var batch = shipment.Batch
                    ?? await _uow.Batches.GetByIdAsync(shipment.BatchId!.Value, ct)
                    ?? throw new NotFoundException("Không tìm thấy Batch.");

                if (batch.CurrentStage != BatchStage.RECEIVED_AT_RETAILER)
                    throw new ValidationException(
                        $"Batch hiện ở trạng thái {batch.CurrentStage}, " +
                        "không thể đưa lên kệ (yêu cầu RECEIVED_AT_RETAILER).");

                var now = DateTime.UtcNow;
                var metadata = new
                {
                    action = "READY_FOR_SALE",
                    assetType = "PARENT",
                    batchId = batch.Id,
                    batchCode = batch.BatchCode,
                    shipmentId = shipment.Id,
                    shippingCode = shipment.ShippingCode,
                    retailerId = retailerId,
                    receivedAt = shipment.ReceivedDate,
                    readyAt = now,
                };

                var (readyMetaUri, readyHash) = await _ipfs.UploadJsonAsync(
                    metadata,
                    fileName: $"ready-parent-{batch.BatchCode}-{now:yyyyMMddHHmmss}.json",
                    ct: ct);

                string txHash;
                try
                {
                    txHash = await _blockchain.ReadyParentAsync(
                        batchId: batch.Id.ToString(),
                        metadataURI: readyMetaUri,
                        dataHash: readyHash,
                        signerPrivateKey: signerPrivateKey,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "readyParent on-chain thất bại cho Shipment {ShipmentId}.", shipment.Id);
                    throw;
                }

                shipment.ReadyMetadataURI = readyMetaUri;
                shipment.ReadyDataHash = readyHash;
                shipment.ReadyTransactionHash = txHash;
                shipment.ReadyForSaleDate = now;
                _uow.Shipments.Update(shipment);

                batch.CurrentStage = BatchStage.READY_FOR_SALE;
                _uow.Batches.Update(batch);

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "ReadyParent OK: Shipment {ShipmentId} ➔ Batch {BatchCode}, TxHash={TxHash}, NewStage={Stage}",
                    shipment.Id, batch.BatchCode, txHash, batch.CurrentStage);

                return new RetailerActionResponseDto(
                    ShipmentId: shipment.Id,
                    AssetType: AssetType.PARENT.ToString(),
                    BatchId: batch.Id,
                    BatchCode: batch.BatchCode,
                    SubBatchId: null,
                    SubBatchCode: null,
                    CurrentStage: batch.CurrentStage.ToString(),
                    ReceiveMetadataURI: shipment.ReceiveMetadataURI,
                    ReceiveDataHash: shipment.ReceiveDataHash,
                    ReceiveTransactionHash: shipment.ReceiveTransactionHash,
                    ReceivedDate: shipment.ReceivedDate,
                    ReadyMetadataURI: shipment.ReadyMetadataURI,
                    ReadyDataHash: shipment.ReadyDataHash,
                    ReadyTransactionHash: shipment.ReadyTransactionHash,
                    ReadyForSaleDate: shipment.ReadyForSaleDate,
                    TransactionHash: txHash,
                    UpdatedAt: shipment.UpdatedAt);
            }

            case AssetType.SUB:
            {
                var subBatch = shipment.SubBatch
                    ?? await _uow.SubBatches.GetByIdAsync(shipment.SubBatchId!.Value, ct)
                    ?? throw new NotFoundException("Không tìm thấy SubBatch.");

                var parentBatch = subBatch.ParentBatch
                    ?? throw new NotFoundException("Không tìm thấy Parent Batch.");

                if (subBatch.CurrentStage != BatchStage.RECEIVED_AT_RETAILER)
                    throw new ValidationException(
                        $"SubBatch hiện ở trạng thái {subBatch.CurrentStage}, " +
                        "không thể đưa lên kệ (yêu cầu RECEIVED_AT_RETAILER).");

                var now = DateTime.UtcNow;
                var metadata = new
                {
                    action = "READY_FOR_SALE",
                    assetType = "SUB",
                    subBatchId = subBatch.Id,
                    subBatchCode = subBatch.SubBatchCode,
                    parentBatchId = parentBatch.Id,
                    parentBatchCode = parentBatch.BatchCode,
                    shipmentId = shipment.Id,
                    shippingCode = shipment.ShippingCode,
                    retailerId = retailerId,
                    receivedAt = shipment.ReceivedDate,
                    readyAt = now,
                };

                var (readyMetaUri, readyHash) = await _ipfs.UploadJsonAsync(
                    metadata,
                    fileName: $"ready-sub-{subBatch.SubBatchCode}-{now:yyyyMMddHHmmss}.json",
                    ct: ct);

                string txHash;
                try
                {
                    txHash = await _blockchain.ReadySubAsync(
                        subBatchId: subBatch.Id.ToString(),
                        metadataURI: readyMetaUri,
                        dataHash: readyHash,
                        signerPrivateKey: signerPrivateKey,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "readySub on-chain thất bại cho Shipment {ShipmentId}.", shipment.Id);
                    throw;
                }

                shipment.ReadyMetadataURI = readyMetaUri;
                shipment.ReadyDataHash = readyHash;
                shipment.ReadyTransactionHash = txHash;
                shipment.ReadyForSaleDate = now;
                _uow.Shipments.Update(shipment);

                subBatch.CurrentStage = BatchStage.READY_FOR_SALE;
                _uow.SubBatches.Update(subBatch);

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "ReadySub OK: Shipment {ShipmentId} ➔ SubBatch {SubBatchCode}, TxHash={TxHash}, NewStage={Stage}",
                    shipment.Id, subBatch.SubBatchCode, txHash, subBatch.CurrentStage);

                return new RetailerActionResponseDto(
                    ShipmentId: shipment.Id,
                    AssetType: AssetType.SUB.ToString(),
                    BatchId: null,
                    BatchCode: parentBatch.BatchCode,
                    SubBatchId: subBatch.Id,
                    SubBatchCode: subBatch.SubBatchCode,
                    CurrentStage: subBatch.CurrentStage.ToString(),
                    ReceiveMetadataURI: shipment.ReceiveMetadataURI,
                    ReceiveDataHash: shipment.ReceiveDataHash,
                    ReceiveTransactionHash: shipment.ReceiveTransactionHash,
                    ReceivedDate: shipment.ReceivedDate,
                    ReadyMetadataURI: shipment.ReadyMetadataURI,
                    ReadyDataHash: shipment.ReadyDataHash,
                    ReadyTransactionHash: shipment.ReadyTransactionHash,
                    ReadyForSaleDate: shipment.ReadyForSaleDate,
                    TransactionHash: txHash,
                    UpdatedAt: shipment.UpdatedAt);
            }

            default:
                throw new ValidationException($"AssetType {shipment.AssetType} không hỗ trợ.");
        }
    }
}
