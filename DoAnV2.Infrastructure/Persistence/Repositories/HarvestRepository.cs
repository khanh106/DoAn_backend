using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai IHarvestRepository (TASK 06 - Mục 6.2 & 6.3).
/// Harvest là bản ghi xác nhận thu hoạch hoặc tiếp nhận sau thu hoạch - có gọi Smart Contract.
/// </summary>
public class HarvestRepository : IHarvestRepository
{
    private readonly ApplicationDbContext _db;

    public HarvestRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Harvest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Harvests
            .Include(x => x.Batch)
            .Include(x => x.RepresentativeUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Harvest>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default)
    {
        return await _db.Harvests
            .Include(x => x.RepresentativeUser)
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.HarvestDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Harvest entity, CancellationToken ct = default)
        => await _db.Harvests.AddAsync(entity, ct);
}