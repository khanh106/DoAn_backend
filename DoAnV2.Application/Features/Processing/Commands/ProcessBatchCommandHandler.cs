using System.Text.Json;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Processing.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using ProcessingEntity = DoAnV2.Domain.Entities.Processing;

namespace DoAnV2.Application.Features.Processing.Commands;

/// <summary>
/// TASK 07 - Mục 7.1: Handler Processor ghi nhận công đoạn Sơ chế (gọi SC processBatch).
///   1. Validate lô tồn tại &amp; đang ở STAGE_RECEIVED (BR-12).
///   2. Validate Processor sở hữu Batch.
///   3. Upload từng ảnh (nếu có) + Metadata JSON lên IPFS ➔ (MetadataURI, DataHash).
///   4. Processor gọi SC processBatch(batchId, metadataURI, dataHash).
///   5. Lưu bản ghi Processing + chuyển Batch.CurrentStage = STAGE_PROCESSED.
/// </summary>
public class ProcessBatchCommandHandler
    : IRequestHandler<ProcessBatchCommand, ProcessBatchResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<ProcessBatchCommandHandler> _logger;

    public ProcessBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        ILogger<ProcessBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<ProcessBatchResponseDto> Handle(
        ProcessBatchCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // ========== 1. Validate input ==========
        if (string.IsNullOrWhiteSpace(req.ProcessType))
            throw new ValidationException("ProcessType không được trống.");
        if (string.IsNullOrWhiteSpace(req.Description))
            throw new ValidationException("Description không được trống.");

        // ========== 2. Validate Batch + Stage (BR-12) ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền sơ chế Batch của Processor khác.");

        if (batch.CurrentStage != BatchStage.STAGE_RECEIVED)
            throw new ValidationException(
                $"Batch hiện ở trạng thái {batch.CurrentStage}, không thể sơ chế (chỉ chấp nhận STAGE_RECEIVED).");

        // ========== 3. Upload ảnh (nếu có) lên IPFS ==========
        var imageUrls = new List<string>();
        var imageHashes = new List<string>();

        if (req.Images is not null && req.Images.Count > 0)
        {
            foreach (var img in req.Images)
            {
                if (img is null || img.Length == 0) continue;
                var (url, hash) = await _ipfs.UploadFileAsync(img, ct);
                imageUrls.Add(url);
                imageHashes.Add(hash);
            }
        }

        // ========== 4. Upload Metadata JSON lên IPFS ==========
        var metadata = new
        {
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            processedByProcessorId = processorId,
            processType = req.ProcessType,
            description = req.Description,
            startDate = req.StartDate,
            endDate = req.EndDate,
            imageCount = imageUrls.Count,
            imageUrls = imageUrls,
            imageHashes = imageHashes,
            createdAt = DateTime.UtcNow,
        };

        var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
            metadata,
            fileName: $"process-{batch.BatchCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
            ct: ct);

        // ========== 5. Gọi SC: processBatch(batchId, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.ProcessBatchAsync(
                batchId: batch.Id.ToString(),
                metadataURI: metadataURI,
                dataHash: dataHash,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "processBatch on-chain thất bại cho Batch {BatchId}.", batch.Id);
            throw;
        }

        // ========== 6. Lưu Processing record + cập nhật stage ==========
        var processing = new ProcessingEntity
        {
            BatchId = batch.Id,
            ProcessType = req.ProcessType.Trim(),
            Description = req.Description.Trim(),
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            MetadataURI = metadataURI,
            DataHash = dataHash,
            ImageUrlsJson = JsonSerializer.Serialize(imageUrls),
        };
        await _uow.Processings.AddAsync(processing, ct);

        batch.CurrentStage = BatchStage.STAGE_PROCESSED;
        _uow.Batches.Update(batch);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ProcessBatch OK: Batch {BatchCode}, TxHash={TxHash}, NewStage={Stage}",
            batch.BatchCode, txHash, batch.CurrentStage);

        return new ProcessBatchResponseDto(
            ProcessingId: processing.Id,
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            ProcessedByUserId: processorId,
            ProcessedByUserName: _currentUser.Email ?? string.Empty,
            ProcessType: processing.ProcessType,
            Description: processing.Description,
            StartDate: processing.StartDate,
            EndDate: processing.EndDate,
            ImageUrls: imageUrls,
            MetadataURI: metadataURI,
            DataHash: dataHash,
            CurrentStage: batch.CurrentStage.ToString(),
            TransactionHash: txHash,
            CreatedAt: processing.CreatedAt);
    }
}
