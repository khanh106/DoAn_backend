using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default);
    Task AddAsync(Product entity, CancellationToken ct = default);
    void Update(Product entity);
}