using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class FruitTypeRepository : IFruitTypeRepository
{
    private readonly ApplicationDbContext _db;

    public FruitTypeRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<FruitType?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.FruitTypes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<FruitType>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default)
        => await _db.FruitTypes
            .Where(x => x.ProcessorId == processorId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<bool> CodeExistsForProcessorAsync(string code, Guid processorId, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _db.FruitTypes.AsQueryable()
            .Where(x => x.ProcessorId == processorId && x.Code == code);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(ct);
    }

    public async Task AddAsync(FruitType entity, CancellationToken ct = default)
        => await _db.FruitTypes.AddAsync(entity, ct);

    public void Update(FruitType entity)
        => _db.FruitTypes.Update(entity);
}