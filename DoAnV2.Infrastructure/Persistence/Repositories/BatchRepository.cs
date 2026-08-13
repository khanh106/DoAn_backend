using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class BatchRepository : IBatchRepository
{
    private readonly ApplicationDbContext _db;

    public BatchRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Batch?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Batches
            .Include(b => b.BatchWorkers)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    /// <summary>
    /// Update scalar columns (MetadataURI, DataHash, UpdatedAt) của Batch bằng raw SQL
    /// để tránh EF tracking navigation properties. Nếu dùng entity tracking thông thường,
    /// khi reload entity kèm navigation nhưng không load FarmArea/FruitType/...,
    /// EF sẽ set FK về null trong change tracker → ném "association has been severed".
    /// </summary>
    public async Task UpdateMetadataAsync(Guid batchId, string metadataURI, string dataHash, DateTime updatedAt, CancellationToken ct = default)
    {
        await _db.Batches
            .Where(b => b.Id == batchId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.MetadataURI, metadataURI)
                .SetProperty(b => b.DataHash, dataHash)
                .SetProperty(b => b.UpdatedAt, updatedAt), ct);
    }

    /// <summary>
    /// Xóa Batch + tất cả BatchWorkers liên quan bằng raw SQL DELETE,
    /// bypass hoàn toàn EF change tracker để tránh lỗi "association has been severed".
    /// </summary>
    public async Task DeleteBatchWithWorkersAsync(Guid batchId, CancellationToken ct = default)
    {
        // Xóa workers trước (FK constraint)
        await _db.BatchWorkers
            .Where(w => w.BatchId == batchId)
            .ExecuteDeleteAsync(ct);

        // Sau đó xóa batch
        await _db.Batches
            .Where(b => b.Id == batchId)
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>Lookup batch by Guid kèm Include Workers + FruitType + Product + FarmArea + Processor.</summary>
    public async Task<Batch?> GetByIdWithWorkersAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Batches
            .Include(x => x.BatchWorkers).ThenInclude(w => w.User)
            .Include(x => x.FruitType)
            .Include(x => x.Product)
            .Include(x => x.FarmArea)
            .Include(x => x.RepresentativeWorker)
            .Include(x => x.Processor)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    /// <summary>Lookup batch by BatchCode.</summary>
    public Task<Batch?> GetByBatchCodeAsync(string batchCode, CancellationToken ct = default)
        => _db.Batches.FirstOrDefaultAsync(x => x.BatchCode == batchCode, ct);

    public Task<bool> BatchCodeExistsAsync(string batchCode, CancellationToken ct = default)
        => _db.Batches.AnyAsync(x => x.BatchCode == batchCode, ct);

    public Task<Batch?> GetByIdWithFullChainAsync(Guid id, CancellationToken ct = default)
        => _db.Batches
            .Include(x => x.BatchWorkers).ThenInclude(w => w.User)
            .Include(x => x.FruitType)
            .Include(x => x.Product)
            .Include(x => x.FarmArea)
            .Include(x => x.RepresentativeWorker)
            .Include(x => x.Processor)
            .Include(x => x.CultivationLogs)
                .ThenInclude(c => c.User)
            .Include(x => x.Harvests)
                .ThenInclude(h => h.RepresentativeUser)
            .Include(x => x.Processings)
            .Include(x => x.SubBatches)
            .Include(x => x.Inspections)
            .Include(x => x.Packagings)
            .Include(x => x.Shipments)
                .ThenInclude(s => s.Retailer)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Batch entity, CancellationToken ct = default)
        => await _db.Batches.AddAsync(entity, ct);

    public void Update(Batch entity)
        => _db.Batches.Update(entity);

    public void Remove(Batch entity)
        => _db.Batches.Remove(entity);

    public async Task<BatchStats> GetStatsAsync(CancellationToken ct = default)
    {
        var all = await _db.Batches
            .Select(x => x.CurrentStage)
            .ToListAsync(ct);

        return new BatchStats(
            Total: all.Count,
            InProduction: all.Count(s => s == BatchStage.STAGE_PLANTING),
            Harvested: all.Count(s => s == BatchStage.STAGE_HARVESTED),
            Packaged: all.Count(s => s == BatchStage.PACKAGED),
            ReadyForSale: all.Count(s => s == BatchStage.READY_FOR_SALE));
    }

    // === BỔ SUNG HÀM MỚI ===
    public async Task<IReadOnlyList<Batch>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default)
        => await _db.Batches
            .Include(x => x.BatchWorkers).ThenInclude(w => w.User)
            .Include(x => x.FruitType)
            .Include(x => x.Product)
            .Include(x => x.FarmArea)
            .Include(x => x.RepresentativeWorker)
            .Include(x => x.Processor)
            .Include(x => x.BlockchainTransactions)
            .Where(x => x.ProcessorId == processorId &&
                       x.BlockchainTransactions.Any(t => t.FunctionName == "createBatch" && t.Status == TransactionStatus.SUCCESS))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
}