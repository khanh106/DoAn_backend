using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Queues;
using DoAnV2.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Infrastructure.Services.Blockchain;

/// <summary>
/// BackgroundService đọc job từ queue và xử lý blockchain:
///   1. Load batch từ DB (đã được handler tạo sẵn).
///   2. Lấy thông tin Processor, Workers, Representative.
///   3. Gọi Smart Contract: createBatch → assignWorker (từng worker) → setRepresentative.
///   4. Cập nhật BlockchainSyncStatus = CONFIRMED hoặc FAILED.
/// </summary>
public sealed class BlockchainJobProcessor : BackgroundService
{
    private readonly IBlockchainJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlockchainJobProcessor> _logger;

    public BlockchainJobProcessor(
        IBlockchainJobQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BlockchainJobProcessor> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BlockchainJobProcessor] Bắt đầu lắng nghe blockchain jobs.");

        await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job.BatchId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BlockchainJobProcessor] Job cho batch {BatchId} thất bại.", job.BatchId);
            }
        }
    }

    private async Task ProcessJobAsync(Guid batchId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var blockchain = scope.ServiceProvider.GetRequiredService<IBlockchainService>();
        var walletService = scope.ServiceProvider.GetRequiredService<IWalletService>();
        var walletOptions = scope.ServiceProvider
 .GetRequiredService<Microsoft.Extensions.Options.IOptions<DoAnV2.Application.Common.Options.WalletOptions>>().Value;

        // Include BatchWorkers để có thể lấy UserId cho assignWorker.
var batch = await uow.Batches.GetByIdWithWorkersAsync(batchId, ct);
        if (batch is null)
        {
            _logger.LogWarning("[BlockchainJobProcessor] Batch {BatchId} không tồn tại, bỏ qua.", batchId);
            return;
        }

        if (batch.BlockchainSyncStatus == BlockchainSyncStatus.CONFIRMED)
        {
            _logger.LogInformation("[BlockchainJobProcessor] Batch {BatchId} đã CONFIRMED, bỏ qua.", batchId);
            return;
        }

        try
        {
            // 1. Lấy Processor & PrivateKey
            var processor = await uow.Users.GetByIdAsync(batch.ProcessorId, ct);
            if (processor is null)
                throw new InvalidOperationException($"Processor {batch.ProcessorId} không tồn tại.");

            string? processorPrivateKey = null;
            if (!string.IsNullOrWhiteSpace(processor.EncryptedPrivateKey))
                processorPrivateKey = walletService.DecryptPrivateKey(
                    processor.EncryptedPrivateKey, walletOptions.EncryptionKey);

            // 2. createBatch on-chain
            var fruitType = await uow.FruitTypes.GetByIdAsync(batch.FruitTypeId, ct);
            var createTxHash = await blockchain.CreateBatchAsync(
                batchId: batch.Id.ToString(),
                batchCode: batch.BatchCode,
                fruitType: fruitType?.Code ?? "",
                metadataURI: batch.MetadataURI ?? "",
                dataHash: batch.DataHash ?? "",
                signerPrivateKey: processorPrivateKey,
                ct: ct);

            batch.CreateBatchTxHash = createTxHash;
            uow.Batches.Update(batch);
            await uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[BlockchainJobProcessor] Batch {BatchCode} createBatch OK. TxHash={TxHash}",
                batch.BatchCode, createTxHash);

            // 3. assignWorker cho từng worker
            var workerIds = batch.BatchWorkers.Select(bw => bw.UserId).Distinct().ToList();
            var workers = await uow.Users.GetByIdsAsync(workerIds, ct);

            foreach (var w in workers)
            {
                if (string.IsNullOrWhiteSpace(w.WalletAddress))
                {
                    _logger.LogWarning(
                        "[BlockchainJobProcessor] Worker {UserId} chưa có WalletAddress - bỏ qua.",
                        w.Id);
                    continue;
                }

                try
                {
                    await blockchain.AssignWorkerAsync(
                        batchId: batch.Id.ToString(),
                        workerAddress: w.WalletAddress,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
 "[BlockchainJobProcessor] assignWorker thất bại cho {UserId}.", w.Id);
                }
            }

            // 4. setRepresentative
            var repUser = workers.FirstOrDefault(w => w.Id == batch.RepresentativeWorkerId);
            if (repUser is not null && !string.IsNullOrWhiteSpace(repUser.WalletAddress))
            {
                try
                {
                    await blockchain.SetRepresentativeAsync(
                        batchId: batch.Id.ToString(),
                        repAddress: repUser.WalletAddress,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[BlockchainJobProcessor] setRepresentative thất bại cho {UserId}.",
                        repUser.Id);
                }
            }

            // 5. Đánh dấu CONFIRMED
            batch.BlockchainSyncStatus = BlockchainSyncStatus.CONFIRMED;
            batch.BlockchainSyncedAt = DateTime.UtcNow;
            batch.BlockchainSyncError = null;
            uow.Batches.Update(batch);
            await uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[BlockchainJobProcessor] Batch {BatchCode} đã đồng bộ blockchain thành công.",
                batch.BatchCode);
        }
        catch (Exception ex)
{
    // Nếu batch đã bị xóa (IPFS fail, rollback), không update gì cả.
    var fresh = await uow.Batches.GetByIdAsync(batchId, ct);
    if (fresh is null)
    {
        _logger.LogWarning(ex,
            "[BlockchainJobProcessor] Batch {BatchId} đã bị xóa khỏi DB (IPFS rollback) — bỏ qua.",
            batchId);
        return;
    }

    fresh.BlockchainSyncStatus = BlockchainSyncStatus.FAILED;
    fresh.BlockchainSyncError = ex.Message;
    uow.Batches.Update(fresh);
    await uow.SaveChangesAsync(CancellationToken.None);

    _logger.LogError(ex,
        "[BlockchainJobProcessor] Batch {BatchCode} đồng bộ blockchain thất bại.",
        fresh.BatchCode);
}
    }
}