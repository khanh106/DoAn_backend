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
/// TASK 09 - Mục 9.2: Handler Retailer xác nhận tiếp nhận lô hàng (gọi SC receiveParent / receiveSub).
///   1. Validate Shipment tồn tại &amp; thuộc Retailer hiện tại (BR-18).
///   2. Validate Batch/SubBatch đang ở STAGE_SHIPPING (BR-18).
///   3. Upload biên bản tiếp nhận JSON lên IPFS ➔ (MetadataURI, DataHash).
///   4. Retailer gọi SC receiveParent hoặc receiveSub tương ứng.
///   5. Cập nhật Shipment.Receive* + ReceivedDate + Batch.CurrentStage = RECEIVED_AT_RETAILER.
/// </summary>
public class ReceiveShipmentCommandHandler
    : IRequestHandler<ReceiveShipmentCommand, RetailerActionResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<ReceiveShipmentCommandHandler> _logger;

    public ReceiveShipmentCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<ReceiveShipmentCommandHandler> logger)
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
        ReceiveShipmentCommand req, CancellationToken ct)
    {
        var retailerId = Guard.RequireRetailer(_currentUser);

        // ========== 1. Validate Shipment ==========
        var shipment = await _uow.Shipments.GetByIdAsync(req.ShipmentId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Shipment {req.ShipmentId}.");

        if (shipment.RetailerId != retailerId)
            throw new ForbiddenException("Shipment này không được giao cho bạn.");

        if (shipment.ReceivedDate.HasValue)
            throw new ValidationException("Shipment đã được xác nhận tiếp nhận trước đó.");

        // ========== Lấy và giải mã Private Key của ví Retailer ==========
        var retailerUser = await _uow.Users.GetByIdAsync(retailerId, ct)
            ?? throw new NotFoundException($"Không tìm thấy thông tin tài khoản Retailer {retailerId}.");

        string? signerPrivateKey = null;
        if (!string.IsNullOrWhiteSpace(retailerUser.EncryptedPrivateKey))
        {
            signerPrivateKey = _walletService.DecryptPrivateKey(
                retailerUser.EncryptedPrivateKey, _walletOptions.EncryptionKey);
        }

        // ========== 2. Xác định asset + validate stage (BR-18) ==========
        string? batchCode;
        string? subBatchCode;
        string txAssetId;
        object metadata;

        switch (shipment.AssetType)
        {
            case AssetType.PARENT:
            {
                var batch = shipment.Batch
                    ?? await _uow.Batches.GetByIdAsync(shipment.BatchId!.Value, ct)
                    ?? throw new NotFoundException("Không tìm thấy Batch.");

                if (batch.CurrentStage != BatchStage.STAGE_SHIPPING)
                    throw new ValidationException(
                        $"Batch hiện ở trạng thái {batch.CurrentStage}, " +
                        "không thể nhận (yêu cầu STAGE_SHIPPING - BR-18).");

                batchCode = batch.BatchCode;
                subBatchCode = null;
                txAssetId = batch.Id.ToString();
                metadata = new
                {
                    action = "RECEIVE_AT_RETAILER",
                    assetType = "PARENT",
                    batchId = batch.Id,
                    batchCode = batch.BatchCode,
                    shipmentId = shipment.Id,
                    shippingCode = shipment.ShippingCode,
                    retailerId = retailerId,
                    receivedAt = DateTime.UtcNow,
                };

                // ========== 3. Upload Metadata JSON lên IPFS ==========
                var (receiveMetaUri, receiveHash) = await _ipfs.UploadJsonAsync(
                    metadata,
                    fileName: $"receive-parent-{batch.BatchCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
                    ct: ct);

                // ========== 4. Gọi SC: receiveParent(batchId, metadataURI, dataHash) ==========
                string txHash;
                try
                {
                    txHash = await _blockchain.ReceiveParentAsync(
                        batchId: batch.Id.ToString(),
                        metadataURI: receiveMetaUri,
                        dataHash: receiveHash,
                        signerPrivateKey: signerPrivateKey,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "receiveParent on-chain thất bại cho Shipment {ShipmentId}.", shipment.Id);
                    throw;
                }

                // ========== 5. Cập nhật Shipment + CurrentStage ==========
                shipment.ReceiveMetadataURI = receiveMetaUri;
                shipment.ReceiveDataHash = receiveHash;
                shipment.ReceiveTransactionHash = txHash;
                shipment.ReceivedDate = DateTime.UtcNow;
                _uow.Shipments.Update(shipment);

                batch.CurrentStage = BatchStage.RECEIVED_AT_RETAILER;
                _uow.Batches.Update(batch);

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "ReceiveParent OK: Shipment {ShipmentId} ➔ Batch {BatchCode}, TxHash={TxHash}, NewStage={Stage}",
                    shipment.Id, batch.BatchCode, txHash, batch.CurrentStage);

                return new RetailerActionResponseDto(
                    ShipmentId: shipment.Id,
                    AssetType: AssetType.PARENT.ToString(),
                    BatchId: batch.Id,
                    BatchCode: batchCode,
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

                if (subBatch.CurrentStage != BatchStage.STAGE_SHIPPING)
                    throw new ValidationException(
                        $"SubBatch hiện ở trạng thái {subBatch.CurrentStage}, " +
                        "không thể nhận (yêu cầu STAGE_SHIPPING - BR-18).");

                batchCode = parentBatch.BatchCode;
                subBatchCode = subBatch.SubBatchCode;
                txAssetId = subBatch.Id.ToString();
                metadata = new
                {
                    action = "RECEIVE_AT_RETAILER",
                    assetType = "SUB",
                    subBatchId = subBatch.Id,
                    subBatchCode = subBatch.SubBatchCode,
                    parentBatchId = parentBatch.Id,
                    parentBatchCode = parentBatch.BatchCode,
                    shipmentId = shipment.Id,
                    shippingCode = shipment.ShippingCode,
                    retailerId = retailerId,
                    receivedAt = DateTime.UtcNow,
                };

                var (receiveMetaUri, receiveHash) = await _ipfs.UploadJsonAsync(
                    metadata,
                    fileName: $"receive-sub-{subBatch.SubBatchCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
                    ct: ct);

                string txHash;
                try
                {
                    txHash = await _blockchain.ReceiveSubAsync(
                        subBatchId: subBatch.Id.ToString(),
                        metadataURI: receiveMetaUri,
                        dataHash: receiveHash,
                        signerPrivateKey: signerPrivateKey,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "receiveSub on-chain thất bại cho Shipment {ShipmentId}.", shipment.Id);
                    throw;
                }

                shipment.ReceiveMetadataURI = receiveMetaUri;
                shipment.ReceiveDataHash = receiveHash;
                shipment.ReceiveTransactionHash = txHash;
                shipment.ReceivedDate = DateTime.UtcNow;
                _uow.Shipments.Update(shipment);

                subBatch.CurrentStage = BatchStage.RECEIVED_AT_RETAILER;
                _uow.SubBatches.Update(subBatch);

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "ReceiveSub OK: Shipment {ShipmentId} ➔ SubBatch {SubBatchCode}, TxHash={TxHash}, NewStage={Stage}",
                    shipment.Id, subBatch.SubBatchCode, txHash, subBatch.CurrentStage);

                return new RetailerActionResponseDto(
                    ShipmentId: shipment.Id,
                    AssetType: AssetType.SUB.ToString(),
                    BatchId: null,
                    BatchCode: batchCode,
                    SubBatchId: subBatch.Id,
                    SubBatchCode: subBatchCode,
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
