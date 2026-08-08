using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai ICultivationLogRepository (TASK 06 - Mục 6.1).
/// Lưu OFF-CHAIN, không gọi Smart Contract cho mỗi log (BR-07, BR-08).
/// </summary>
public class CultivationLogRepository : ICultivationLogRepository
{
    private readonly ApplicationDbContext _db;

    public CultivationLogRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<CultivationLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.CultivationLogs
            .Include(x => x.User)
            .Include(x => x.Batch)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<CultivationLog>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default)
    {
        return await _db.CultivationLogs
            .Include(x => x.User)
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.LogDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(CultivationLog entity, CancellationToken ct = default)
        => await _db.CultivationLogs.AddAsync(entity, ct);
}