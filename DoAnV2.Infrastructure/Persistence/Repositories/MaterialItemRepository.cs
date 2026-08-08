using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class MaterialItemRepository : IMaterialItemRepository
{
    private readonly ApplicationDbContext _db;

    public MaterialItemRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<MaterialItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.MaterialItems.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<MaterialItem>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default)
        => await _db.MaterialItems
            .Where(x => x.ProcessorId == processorId)
            .OrderBy(x => x.ItemType)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

    public async Task AddAsync(MaterialItem entity, CancellationToken ct = default)
        => await _db.MaterialItems.AddAsync(entity, ct);

    public void Update(MaterialItem entity)
        => _db.MaterialItems.Update(entity);
}