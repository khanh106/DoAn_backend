using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai ISubBatchRepository (TASK 07 - Mục 7.3, BR-16).
/// </summary>
public class SubBatchRepository : ISubBatchRepository
{
    private readonly ApplicationDbContext _db;

    public SubBatchRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<SubBatch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.SubBatches
            .Include(x => x.ParentBatch)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<SubBatch?> GetBySubBatchCodeAsync(string subBatchCode, CancellationToken ct = default)
        => _db.SubBatches.FirstOrDefaultAsync(x => x.SubBatchCode == subBatchCode, ct);

    public Task<bool> SubBatchCodeExistsAsync(string subBatchCode, CancellationToken ct = default)
        => _db.SubBatches.AnyAsync(x => x.SubBatchCode == subBatchCode, ct);

    public async Task<IReadOnlyList<SubBatch>> GetByParentBatchIdAsync(
        Guid parentBatchId, CancellationToken ct = default)
    {
        return await _db.SubBatches
            .Where(x => x.ParentBatchId == parentBatchId)
            .OrderBy(x => x.SubBatchCode)
            .ToListAsync(ct);
    }

    public Task<SubBatch?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => _db.SubBatches
            .Include(x => x.ParentBatch)
                .ThenInclude(pb => pb.FarmArea)
            .Include(x => x.ParentBatch)
                .ThenInclude(pb => pb.FruitType)
            .Include(x => x.ParentBatch)
                .ThenInclude(pb => pb.Product)
            .Include(x => x.ParentBatch)
                .ThenInclude(pb => pb.BatchWorkers)
                    .ThenInclude(bw => bw.User)
            .Include(x => x.ParentBatch)
                .ThenInclude(pb => pb.RepresentativeWorker)
            .Include(x => x.ParentBatch)
                .ThenInclude(pb => pb.CultivationLogs)
            .Include(x => x.ParentBatch)
                .ThenInclude(pb => pb.Harvests)
            .Include(x => x.ParentBatch)
                .ThenInclude(pb => pb.Processings)
            .Include(x => x.Inspections)
            .Include(x => x.Packagings)
            .Include(x => x.Shipments)
                .ThenInclude(s => s.Retailer)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(SubBatch entity, CancellationToken ct = default)
        => await _db.SubBatches.AddAsync(entity, ct);

    public void Update(SubBatch entity) => _db.SubBatches.Update(entity);
}
