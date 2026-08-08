using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
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

    public async Task AddAsync(Batch entity, CancellationToken ct = default)
        => await _db.Batches.AddAsync(entity, ct);

    public void Update(Batch entity)
        => _db.Batches.Update(entity);

    public void Remove(Batch entity)
        => _db.Batches.Remove(entity);
}
