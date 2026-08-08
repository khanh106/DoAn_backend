using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Products
            .Include(p => p.FruitType)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default)
        => await _db.Products
            .Include(p => p.FruitType)
            .Where(p => p.FruitType.ProcessorId == processorId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task AddAsync(Product entity, CancellationToken ct = default)
        => await _db.Products.AddAsync(entity, ct);

    public void Update(Product entity)
        => _db.Products.Update(entity);
}