using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Harvests.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Harvests.Commands;

/// <summary>
/// TASK 06 - Mục 6.3: Handler Processor tiếp nhận lô sau thu hoạch (gọi SC receiveBatch).
///   1. Validate lô tồn tại &amp; đang ở STAGE_HARVESTED (BR-11).
///   2. Validate Processor sở hữu Batch.
///   3. Upload thông tin tiếp nhận lên IPFS ➔ (MetadataURI, DataHash).
///   4. Processor gọi SC receiveBatch(batchId, metadataURI, dataHash).
///   5. Cập nhật Batch.CurrentStage = STAGE_RECEIVED.
/// </summary>
public class ReceiveBatchCommandHandler
    : IRequestHandler<ReceiveBatchCommand, ReceiveBatchResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<ReceiveBatchCommandHandler> _logger;

    public ReceiveBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        ILogger<ReceiveBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<ReceiveBatchResponseDto> Handle(
        ReceiveBatchCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // ========== 1. Validate input ==========
        if (req.Quantity <= 0)
            throw new ValidationException("Quantity phải > 0.");
        if (string.IsNullOrWhiteSpace(req.Unit))
            throw new ValidationException("Unit không được trống.");
        if (string.IsNullOrWhiteSpace(req.DeliveryPerson))
            throw new ValidationException("DeliveryPerson không được trống.");
        if (string.IsNullOrWhiteSpace(req.ConditionNote))
            throw new ValidationException("ConditionNote không được trống.");

        // ========== 2. Validate Batch + Stage (BR-11) ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền tiếp nhận Batch của Processor khác.");

        if (batch.CurrentStage != BatchStage.STAGE_HARVESTED)
            throw new ValidationException(
                $"Batch hiện ở trạng thái {batch.CurrentStage}, không thể tiếp nhận (chỉ chấp nhận STAGE_HARVESTED).");

        // ========== 3. Upload Metadata lên IPFS ==========
        var metadata = new
        {
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            receivedByProcessorId = processorId,
            receivedDate = req.ReceivedDate,
            quantity = req.Quantity,
            unit = req.Unit,
            deliveryPerson = req.DeliveryPerson,
            conditionNote = req.ConditionNote,
            createdAt = DateTime.UtcNow,
        };

        var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
            metadata,
            fileName: $"receive-{batch.BatchCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
            ct: ct);

        // ========== 4. Gọi SC: receiveBatch(batchId, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.ReceiveBatchAsync(
                batchId: batch.Id.ToString(),
                metadataURI: metadataURI,
                dataHash: dataHash,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "receiveBatch on-chain thất bại cho Batch {BatchId}.", batch.Id);
            throw;
        }

        // ========== 5. Lưu Harvest record (loại: receive) + cập nhật stage ==========
        var receive = new Harvest
        {
            BatchId = batch.Id,
            // Receive thực hiện bởi Processor - không có representative user trong DB,
            // nhưng schema yêu cầu RepresentativeUserId NOT NULL ➔ dùng Processor.
            RepresentativeUserId = processorId,
            HarvestDate = req.ReceivedDate,
            Quantity = req.Quantity,
            Unit = req.Unit.Trim(),
            InitialQuality = $"RECEIVED | DeliveryPerson={req.DeliveryPerson} | ConditionNote={req.ConditionNote}",
            MetadataURI = metadataURI,
            DataHash = dataHash,
        };
        await _uow.Harvests.AddAsync(receive, ct);

        batch.CurrentStage = BatchStage.STAGE_RECEIVED;
        _uow.Batches.Update(batch);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ReceiveBatch OK: Batch {BatchCode}, TxHash={TxHash}, NewStage={Stage}",
            batch.BatchCode, txHash, batch.CurrentStage);

        return new ReceiveBatchResponseDto(
            ReceiveId: receive.Id,
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            ReceivedByUserId: processorId,
            ReceivedByUserName: _currentUser.Email ?? string.Empty,
            ReceivedDate: req.ReceivedDate,
            Quantity: req.Quantity,
            Unit: req.Unit,
            DeliveryPerson: req.DeliveryPerson,
            ConditionNote: req.ConditionNote,
            MetadataURI: metadataURI,
            DataHash: dataHash,
            CurrentStage: batch.CurrentStage.ToString(),
            TransactionHash: txHash,
            CreatedAt: receive.CreatedAt);
    }
}