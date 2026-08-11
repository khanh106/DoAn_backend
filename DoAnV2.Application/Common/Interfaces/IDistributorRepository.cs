using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IDistributorRepository
{
    Task<Distributor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Distributor>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default);
    Task AddAsync(Distributor entity, CancellationToken ct = default);
    void Update(Distributor entity);
    void Delete(Distributor entity);
}
