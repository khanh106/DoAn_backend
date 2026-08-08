using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class FarmAreaRepository : IFarmAreaRepository
{
    private readonly ApplicationDbContext _db;

    public FarmAreaRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<FarmArea?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.FarmAreas.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<FarmArea>> GetByProcessorIdAsync(
        Guid processorId,
        string? province = null,
        string? district = null,
        string? ward = null,
        string? plantingCode = null,
        CancellationToken ct = default)
    {
        var query = _db.FarmAreas.Where(x => x.ProcessorId == processorId);

        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(x => x.Province == province);
        if (!string.IsNullOrWhiteSpace(district))
            query = query.Where(x => x.District == district);
        if (!string.IsNullOrWhiteSpace(ward))
            query = query.Where(x => x.Ward == ward);
        if (!string.IsNullOrWhiteSpace(plantingCode))
            query = query.Where(x => x.PlantingCode == plantingCode);

        return await query.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task AddAsync(FarmArea entity, CancellationToken ct = default)
        => await _db.FarmAreas.AddAsync(entity, ct);

    public void Update(FarmArea entity)
        => _db.FarmAreas.Update(entity);
}