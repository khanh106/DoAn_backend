using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class BatchWorkerRepository : IBatchWorkerRepository
{
    private readonly ApplicationDbContext _db;

    public BatchWorkerRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<BatchWorker?> GetAsync(Guid batchId, Guid userId, CancellationToken ct = default)
        => _db.BatchWorkers
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.BatchId == batchId && w.UserId == userId, ct);

    public async Task<IReadOnlyList<BatchWorker>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default)
        => await _db.BatchWorkers
            .Include(w => w.User)
            .Where(w => w.BatchId == batchId)
            .OrderBy(w => w.AssignedDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BatchWorker>> GetByUserIdAsync(
        Guid userId,
        WorkerAssignmentStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _db.BatchWorkers
            .Include(w => w.Batch).ThenInclude(b => b.FruitType)
            .Include(w => w.Batch).ThenInclude(b => b.Product)
            .Include(w => w.Batch).ThenInclude(b => b.FarmArea)
            .Include(w => w.Batch).ThenInclude(b => b.Processor)
            .Where(w => w.UserId == userId);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        return await query
            .OrderByDescending(w => w.AssignedDate)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(Guid batchId, Guid userId, CancellationToken ct = default)
        => _db.BatchWorkers.AnyAsync(w => w.BatchId == batchId && w.UserId == userId, ct);

    public Task<bool> HasRepresentativeAsync(Guid batchId, CancellationToken ct = default)
        => _db.BatchWorkers.AnyAsync(w => w.BatchId == batchId && w.IsRepresentative, ct);

    public async Task AddAsync(BatchWorker entity, CancellationToken ct = default)
        => await _db.BatchWorkers.AddAsync(entity, ct);

    public void Update(BatchWorker entity)
        => _db.BatchWorkers.Update(entity);

    public void Remove(BatchWorker entity)
        => _db.BatchWorkers.Remove(entity);
}
