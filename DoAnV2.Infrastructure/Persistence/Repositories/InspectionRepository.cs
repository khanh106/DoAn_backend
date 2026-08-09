using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai IInspectionRepository (TASK 08 - Mục 8.1).
/// Áp dụng cho Parent Batch hoặc SubBatch. Đạt → PACKAGED; Không đạt → dừng (BR-14, BR-15).
/// </summary>
public class InspectionRepository : IInspectionRepository
{
    private readonly ApplicationDbContext _db;

    public InspectionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Inspection?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Inspections
            .Include(x => x.Batch)
            .Include(x => x.SubBatch)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Inspection>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default)
    {
        return await _db.Inspections
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.InspectionDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Inspection>> GetBySubBatchIdAsync(
        Guid subBatchId, CancellationToken ct = default)
    {
        return await _db.Inspections
            .Where(x => x.SubBatchId == subBatchId)
            .OrderByDescending(x => x.InspectionDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Inspection entity, CancellationToken ct = default)
        => await _db.Inspections.AddAsync(entity, ct);
}
