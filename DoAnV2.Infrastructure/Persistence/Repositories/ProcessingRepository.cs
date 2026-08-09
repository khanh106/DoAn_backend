using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai IProcessingRepository (TASK 07 - Mục 7.1).
/// </summary>
public class ProcessingRepository : IProcessingRepository
{
    private readonly ApplicationDbContext _db;

    public ProcessingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Processing?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Processings
            .Include(x => x.Batch)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Processing>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default)
    {
        return await _db.Processings
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Processing entity, CancellationToken ct = default)
        => await _db.Processings.AddAsync(entity, ct);
}
