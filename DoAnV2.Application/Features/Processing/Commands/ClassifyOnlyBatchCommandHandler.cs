using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Processing.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Processing.Commands;

/// <summary>
/// TASK 07 - Mục 7.2: Handler phân loại KHÔNG tách lô (gọi SC classifyOnlyBatch).
/// </summary>
public class ClassifyOnlyBatchCommandHandler
    : IRequestHandler<ClassifyOnlyBatchCommand, ClassifyOnlyResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<ClassifyOnlyBatchCommandHandler> _logger;

    public ClassifyOnlyBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<ClassifyOnlyBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
        _logger = logger;
    }

    public async Task<ClassifyOnlyResponseDto> Handle(
        ClassifyOnlyBatchCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // ========== 1. Validate input ==========
        if (string.IsNullOrWhiteSpace(req.ClassificationNote))
            throw new ValidationException("ClassificationNote không được trống.");
        if (req.GradeDetails is null || req.GradeDetails.Count == 0)
            throw new ValidationException("Phải có ít nhất 1 GradeDetail.");

        foreach (var g in req.GradeDetails)
        {
            if (string.IsNullOrWhiteSpace(g.Grade))
                throw new ValidationException("Grade không được trống trong GradeDetail.");
            if (g.Quantity <= 0)
                throw new ValidationException($"Quantity của Grade '{g.Grade}' phải > 0.");
        }

        // ========== 2. Validate Batch + Stage (BR-12) ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền phân loại Batch của Processor khác.");

        if (batch.CurrentStage != BatchStage.STAGE_PROCESSED)
            throw new ValidationException(
                $"Batch hiện ở trạng thái {batch.CurrentStage}, không thể phân loại (chỉ chấp nhận STAGE_PROCESSED).");

        // ========== 3. Upload Metadata lên IPFS ==========
        var metadata = new
        {
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            classifiedByProcessorId = processorId,
            classificationNote = req.ClassificationNote,
            gradeDetails = req.GradeDetails.Select(g => new
            {
                grade = g.Grade,
                quantity = g.Quantity,
                note = g.Note,
            }),
            totalGradeQuantity = req.GradeDetails.Sum(g => g.Quantity),
            createdAt = DateTime.UtcNow,
        };

        var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
            metadata,
            fileName: $"classify-only-{batch.BatchCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
            ct: ct);

        // ========== 3.5. Lấy và giải mã Private Key của ví Processor ==========
        var processorUser = await _uow.Users.GetByIdAsync(processorId, ct)
            ?? throw new NotFoundException($"Không tìm thấy thông tin tài khoản Processor {processorId}.");

        string? signerPrivateKey = null;
        if (!string.IsNullOrWhiteSpace(processorUser.EncryptedPrivateKey))
        {
            signerPrivateKey = _walletService.DecryptPrivateKey(
                processorUser.EncryptedPrivateKey, _walletOptions.EncryptionKey);
        }

        // ========== 4. Gọi SC: classifyOnlyBatch(batchId, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.ClassifyOnlyBatchAsync(
                batchId: batch.Id.ToString(),
                metadataURI: metadataURI,
                dataHash: dataHash,
                signerPrivateKey: signerPrivateKey,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "classifyOnlyBatch on-chain thất bại cho Batch {BatchId}.", batch.Id);
            throw;
        }

        // ========== 5. Cập nhật stage ==========
        batch.CurrentStage = BatchStage.STAGE_SORTED;
        _uow.Batches.Update(batch);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ClassifyOnlyBatch OK: Batch {BatchCode}, TxHash={TxHash}, NewStage={Stage}",
            batch.BatchCode, txHash, batch.CurrentStage);

        return new ClassifyOnlyResponseDto(
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            ClassifiedByUserId: processorId,
            ClassifiedByUserName: _currentUser.Email ?? string.Empty,
            ClassificationNote: req.ClassificationNote,
            GradeDetails: req.GradeDetails,
            MetadataURI: metadataURI,
            DataHash: dataHash,
            CurrentStage: batch.CurrentStage.ToString(),
            TransactionHash: txHash,
            CreatedAt: DateTime.UtcNow);
    }
}
