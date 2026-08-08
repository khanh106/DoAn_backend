using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Harvests.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Harvests.Commands;

/// <summary>
/// TASK 06 - Mục 6.2: Handler xác nhận thu hoạch lô (gọi SC harvestBatch).
///   1. Validate Batch tồn tại &amp; đang ở STAGE_PLANTING (BR-09).
///   2. Phân quyền:
///      - Lô chỉ có 1 Worker ➔ Worker đó ký.
///      - Lô có nhiều Worker ➔ BẮT BUỘC Người đại diện (RepresentativeWorker) ký (BR-09, BR-10).
///   3. Tổng hợp nhật ký canh tác + dữ liệu thu hoạch ➔ Upload IPFS Metadata.
///   4. Decrypt EncryptedPrivateKey của Representative ➔ Ký giao dịch SC harvestBatch.
///   5. Cập nhật bảng Harvest + Batch.CurrentStage = STAGE_HARVESTED.
/// </summary>
public class HarvestBatchCommandHandler
    : IRequestHandler<HarvestBatchCommand, HarvestBatchResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<HarvestBatchCommandHandler> _logger;

    public HarvestBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<HarvestBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
        _logger = logger;
    }

    public async Task<HarvestBatchResponseDto> Handle(
        HarvestBatchCommand req, CancellationToken ct)
    {
        var callerId = Guard.RequireFarmer(_currentUser);

        // ========== 1. Validate input ==========
        if (req.Quantity <= 0)
            throw new ValidationException("Quantity phải > 0.");
        if (string.IsNullOrWhiteSpace(req.Unit))
            throw new ValidationException("Unit không được trống.");
        if (string.IsNullOrWhiteSpace(req.InitialQuality))
            throw new ValidationException("InitialQuality không được trống.");

        // ========== 2. Load Batch + Workers (BR-09: stage PLANTING) ==========
        var batch = await _uow.Batches.GetByIdWithWorkersAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.CurrentStage != BatchStage.STAGE_PLANTING)
            throw new ValidationException(
                $"Batch hiện ở trạng thái {batch.CurrentStage}, không thể thu hoạch (chỉ chấp nhận STAGE_PLANTING).");

        var workers = batch.BatchWorkers
            .Where(bw => bw.User != null)
            .Select(bw => bw.User!)
            .ToList();
        if (workers.Count == 0)
            throw new ValidationException("Batch chưa có Worker nào được phân công.");

        // ========== 3. Phân quyền ký (BR-09, BR-10) ==========
        //   - Lô 1 worker ➔ worker đó ký.
///   - Lô nhiều worker ➔ BẮT BUỘC RepresentativeWorker ký.
        User signer;
        if (workers.Count == 1)
        {
            signer = workers[0];
            if (signer.Id != callerId)
                throw new ForbiddenException("Bạn không phải Worker duy nhất của lô này.");
        }
        else
        {
            if (!batch.RepresentativeWorkerId.HasValue)
                throw new ValidationException("Batch có nhiều Worker nhưng chưa có Người đại diện.");
            signer = workers.FirstOrDefault(w => w.Id == batch.RepresentativeWorkerId.Value)
                ?? throw new ValidationException("Không tìm thấy thông tin Người đại diện.");

            if (signer.Id != callerId)
                throw new ForbiddenException(
                    "Batch có nhiều công nhân - chỉ Người đại diện mới có quyền xác nhận thu hoạch.");
        }

        if (string.IsNullOrWhiteSpace(signer.EncryptedPrivateKey))
            throw new ValidationException(
                "Người đại diện chưa có Custodial Wallet - không thể ký giao dịch on-chain.");

        // ========== 4. Tổng hợp toàn bộ nhật ký canh tác ==========
        var logs = await _uow.CultivationLogs.GetByBatchIdAsync(batch.Id, ct);
        var logsSummary = logs.Select(l => new
        {
            l.ActivityType,
            l.Description,
            l.LogDate,
            l.ImageUrlsJson,
            WorkerId = l.UserId,
            WorkerName = l.User?.FullName ?? string.Empty,
        }).ToList();

        // ========== 5. Upload Metadata JSON lên IPFS ==========
        var metadata = new
        {
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            representativeWorkerId = signer.Id,
            representativeWorkerName = signer.FullName,
            harvestDate = req.HarvestDate,
            quantity = req.Quantity,
            unit = req.Unit,
            initialQuality = req.InitialQuality,
            notes = req.Notes,
            cultivationLogs = logsSummary,
            cultivationLogCount = logs.Count,
            createdAt = DateTime.UtcNow,
        };

        var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
            metadata,
            fileName: $"harvest-{batch.BatchCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
            ct: ct);

        // ========== 6. Decrypt private key của Representative ==========
        var signerPrivateKey = _walletService.DecryptPrivateKey(
            signer.EncryptedPrivateKey, _walletOptions.EncryptionKey);

        // ========== 7. Gọi SC: harvestBatch(batchId, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.HarvestBatchAsync(
                batchId: batch.Id.ToString(),
                metadataURI: metadataURI,
                dataHash: dataHash,
                signerPrivateKey: signerPrivateKey,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "harvestBatch on-chain thất bại cho Batch {BatchId}.", batch.Id);
            throw;
        }

        // ========== 8. Lưu Harvest + cập nhật Batch.CurrentStage ==========
        var harvest = new Harvest
        {
            BatchId = batch.Id,
            RepresentativeUserId = signer.Id,
            HarvestDate = req.HarvestDate,
            Quantity = req.Quantity,
            Unit = req.Unit.Trim(),
            InitialQuality = req.InitialQuality.Trim(),
            MetadataURI = metadataURI,
            DataHash = dataHash,
        };
        await _uow.Harvests.AddAsync(harvest, ct);

        batch.CurrentStage = BatchStage.STAGE_HARVESTED;
        _uow.Batches.Update(batch);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "HarvestBatch OK: Batch {BatchCode}, TxHash={TxHash}, NewStage={Stage}",
            batch.BatchCode, txHash, batch.CurrentStage);

        return new HarvestBatchResponseDto(
            HarvestId: harvest.Id,
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            RepresentativeUserId: signer.Id,
            RepresentativeUserName: signer.FullName,
            HarvestDate: harvest.HarvestDate,
            Quantity: harvest.Quantity,
            Unit: harvest.Unit,
            InitialQuality: harvest.InitialQuality,
            Notes: req.Notes,
            MetadataURI: metadataURI,
            DataHash: dataHash,
            CurrentStage: batch.CurrentStage.ToString(),
            TransactionHash: txHash,
            CreatedAt: harvest.CreatedAt);
    }
}