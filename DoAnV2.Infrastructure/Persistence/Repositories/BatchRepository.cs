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
        => _db.Batches.FirstOrDefaultAsync(x => x.Id == id, ct);

    // === BỔ SUNG HÀM MỚI VÀO ĐÂY ===
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

    public Task<Batch?> GetByIdWithWorkersAsync(Guid id, CancellationToken ct = default)
        => _db.Batches
            .Include(x => x.BatchWorkers).ThenInclude(w => w.User)
            .Include(x => x.FruitType)
            .Include(x => x.Product)
            .Include(x => x.FarmArea)
            .Include(x => x.RepresentativeWorker)
            .Include(x => x.Processor)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

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
}
