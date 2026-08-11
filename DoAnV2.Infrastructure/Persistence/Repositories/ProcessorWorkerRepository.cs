using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class ProcessorWorkerRepository : IProcessorWorkerRepository
{
    private readonly ApplicationDbContext _db;

    public ProcessorWorkerRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<ProcessorWorker?> GetAsync(Guid processorId, Guid workerId, CancellationToken ct = default)
        => _db.ProcessorWorkers
            .Include(w => w.Processor)
            .Include(w => w.Worker)
            .FirstOrDefaultAsync(w => w.ProcessorId == processorId && w.WorkerId == workerId, ct);

    public Task<ProcessorWorker?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ProcessorWorkers
            .Include(w => w.Processor)
            .Include(w => w.Worker)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<ProcessorWorker>> GetByProcessorIdAsync(Guid processorId, CoopWorkerLinkStatus? status = null, CancellationToken ct = default)
    {
        var q = _db.ProcessorWorkers
            .Include(w => w.Worker)
            .Where(w => w.ProcessorId == processorId);
        if (status.HasValue) q = q.Where(w => w.Status == status.Value);
        return await q.OrderByDescending(w => w.InvitedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProcessorWorker>> GetByWorkerIdAsync(Guid workerId, CoopWorkerLinkStatus? status = null, CancellationToken ct = default)
    {
        var q = _db.ProcessorWorkers
            .Include(w => w.Processor)
            .Where(w => w.WorkerId == workerId);
        if (status.HasValue) q = q.Where(w => w.Status == status.Value);
        return await q.OrderByDescending(w => w.InvitedAt).ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(Guid processorId, Guid workerId, CancellationToken ct = default)
        => _db.ProcessorWorkers.AnyAsync(w => w.ProcessorId == processorId && w.WorkerId == workerId, ct);

    public async Task AddAsync(ProcessorWorker entity, CancellationToken ct = default)
        => await _db.ProcessorWorkers.AddAsync(entity, ct);

    public void Update(ProcessorWorker entity) => _db.ProcessorWorkers.Update(entity);
    public void Remove(ProcessorWorker entity) => _db.ProcessorWorkers.Remove(entity);
}
