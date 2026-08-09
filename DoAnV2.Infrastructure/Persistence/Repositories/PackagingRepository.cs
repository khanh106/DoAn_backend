using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai IPackagingRepository (TASK 08 - Mục 8.2).
/// Chỉ sau khi kiểm định đạt (INSPECTION_PASSED) mới được đóng gói (BR-14).
/// </summary>
public class PackagingRepository : IPackagingRepository
{
    private readonly ApplicationDbContext _db;

    public PackagingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Packaging?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Packagings
            .Include(x => x.Batch)
            .Include(x => x.SubBatch)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Packaging>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default)
    {
        return await _db.Packagings
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.PackDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Packaging>> GetBySubBatchIdAsync(
        Guid subBatchId, CancellationToken ct = default)
    {
        return await _db.Packagings
            .Where(x => x.SubBatchId == subBatchId)
            .OrderByDescending(x => x.PackDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Packaging entity, CancellationToken ct = default)
        => await _db.Packagings.AddAsync(entity, ct);
}
