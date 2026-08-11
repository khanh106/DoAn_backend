using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Processing.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Processing.Commands;

/// <summary>
/// TASK 07 - Mục 7.3: Handler phân loại CÓ tách lô (gọi SC splitBatch).
/// </summary>
public class SplitBatchCommandHandler
    : IRequestHandler<SplitBatchCommand, SplitBatchResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<SplitBatchCommandHandler> _logger;

    public SplitBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<SplitBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
        _logger = logger;
    }

    public async Task<SplitBatchResponseDto> Handle(
        SplitBatchCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // ========== 1. Validate input ==========
        if (req.SubBatches is null || req.SubBatches.Count == 0)
            throw new ValidationException("Phải có ít nhất 1 SubBatch.");

        // Check SubBatchCode unique trong request
        var codes = req.SubBatches.Select(s => s.SubBatchCode.Trim()).ToList();
        if (codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codes.Count)
            throw new ValidationException("Các SubBatchCode trong request phải khác nhau.");

        // Check SubBatchCode từng cái + quantity > 0
        foreach (var s in req.SubBatches)
        {
            if (string.IsNullOrWhiteSpace(s.SubBatchCode))
                throw new ValidationException("SubBatchCode không được trống.");
            if (string.IsNullOrWhiteSpace(s.Classification))
                throw new ValidationException("Classification không được trống.");
            if (s.Quantity <= 0)
                throw new ValidationException($"Quantity của SubBatch '{s.SubBatchCode}' phải > 0.");
        }

        // ========== 2. Validate Batch + Stage (BR-12) ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền tách lô của Processor khác.");

        if (batch.CurrentStage != BatchStage.STAGE_PROCESSED)
            throw new ValidationException(
                $"Batch hiện ở trạng thái {batch.CurrentStage}, không thể tách lô (chỉ chấp nhận STAGE_PROCESSED).");

        // ========== 3. BR-13: Tổng quantity SubBatch <= ExpectedQuantity ==========
        var totalSubQty = req.SubBatches.Sum(s => s.Quantity);
        if (totalSubQty > batch.ExpectedQuantity)
            throw new ValidationException(
                $"Tổng sản lượng SubBatch ({totalSubQty}) vượt quá sản lượng lô gốc ({batch.ExpectedQuantity}).");

        // ========== 4. Check SubBatchCode chưa tồn tại trong DB ==========
        foreach (var code in codes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await _uow.SubBatches.SubBatchCodeExistsAsync(code, ct))
                throw new ConflictException($"SubBatchCode '{code}' đã tồn tại trong hệ thống.");
        }

        // ========== 5. Tạo SubBatch entities + Upload Metadata cho từng cái ==========
        var subBatchEntities = new List<SubBatch>();
        var subBatchIds = new List<string>();
        var metadataURIs = new List<string>();
        var dataHashes = new List<string>();
        var subBatchDtos = new List<SubBatchResponseDto>();

        var now = DateTime.UtcNow;

        foreach (var input in req.SubBatches)
        {
            var sub = new SubBatch
            {
                SubBatchCode = input.SubBatchCode.Trim(),
                ParentBatchId = batch.Id,
                Classification = input.Classification.Trim(),
                Quantity = input.Quantity,
                CurrentStage = BatchStage.STAGE_SORTED,
            };
            subBatchEntities.Add(sub);
            subBatchIds.Add(sub.Id.ToString());

            // Upload Metadata cho SubBatch
            var metadata = new
            {
                subBatchId = sub.Id,
                subBatchCode = sub.SubBatchCode,
                parentBatchId = batch.Id,
                parentBatchCode = batch.BatchCode,
                classification = sub.Classification,
                quantity = sub.Quantity,
                splitByProcessorId = processorId,
                createdAt = now,
            };

            var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
                metadata,
                fileName: $"subbatch-{sub.SubBatchCode}-{now:yyyyMMddHHmmss}.json",
                ct: ct);

            sub.MetadataURI = metadataURI;
            sub.DataHash = dataHash;

            metadataURIs.Add(metadataURI);
            dataHashes.Add(dataHash);

            subBatchDtos.Add(new SubBatchResponseDto(
                Id: sub.Id,
                SubBatchCode: sub.SubBatchCode,
                ParentBatchId: sub.ParentBatchId,
                ParentBatchCode: batch.BatchCode,
                Classification: sub.Classification,
                Quantity: sub.Quantity,
                CurrentStage: new BatchStageInfo(sub.CurrentStage.ToString()),
                MetadataURI: metadataURI,
                DataHash: dataHash,
                CreatedAt: sub.CreatedAt));
        }

        // ========== 6. Lưu SubBatch vào DB trước (để có Id) ==========
        foreach (var sub in subBatchEntities)
        {
            await _uow.SubBatches.AddAsync(sub, ct);
        }
        await _uow.SaveChangesAsync(ct);

        // ========== 6.5. Lấy và giải mã Private Key của ví Processor ==========
        var processorUser = await _uow.Users.GetByIdAsync(processorId, ct)
            ?? throw new NotFoundException($"Không tìm thấy thông tin tài khoản Processor {processorId}.");

        string? signerPrivateKey = null;
        if (!string.IsNullOrWhiteSpace(processorUser.EncryptedPrivateKey))
        {
            signerPrivateKey = _walletService.DecryptPrivateKey(
                processorUser.EncryptedPrivateKey, _walletOptions.EncryptionKey);
        }

        // ========== 7. Gọi SC: splitBatch(batchId, subBatchIds[], metadataURIs[], dataHashes[]) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.SplitBatchAsync(
                batchId: batch.Id.ToString(),
                subBatchIds: subBatchIds.ToArray(),
                metadataURIs: metadataURIs.ToArray(),
                dataHashes: dataHashes.ToArray(),
                signerPrivateKey: signerPrivateKey,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "splitBatch on-chain thất bại cho Batch {BatchId}. SubBatches đã được lưu vào DB.",
                batch.Id);
            throw;
        }

        // ========== 8. Cập nhật ParentBatch.CurrentStage ==========
        batch.CurrentStage = BatchStage.STAGE_SORTED;
        _uow.Batches.Update(batch);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "SplitBatch OK: Batch {BatchCode} ➔ {Count} SubBatches, TxHash={TxHash}, NewStage={Stage}",
            batch.BatchCode, subBatchEntities.Count, txHash, batch.CurrentStage);

        return new SplitBatchResponseDto(
            ParentBatchId: batch.Id,
            ParentBatchCode: batch.BatchCode,
            SplitByUserId: processorId,
            SplitByUserName: _currentUser.Email ?? string.Empty,
            TotalSubBatchQuantity: totalSubQty,
            SubBatches: subBatchDtos,
            CurrentStage: batch.CurrentStage.ToString(),
            TransactionHash: txHash,
            CreatedAt: now);
    }
}
