using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class DistributorRepository : IDistributorRepository
{
    private readonly ApplicationDbContext _db;

    public DistributorRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Distributor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Distributors.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Distributor>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default)
        => await _db.Distributors
            .Where(x => x.ProcessorId == processorId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Distributor entity, CancellationToken ct = default)
        => await _db.Distributors.AddAsync(entity, ct);

    public void Update(Distributor entity)
        => _db.Distributors.Update(entity);

    public void Delete(Distributor entity)
        => _db.Distributors.Remove(entity);
}
