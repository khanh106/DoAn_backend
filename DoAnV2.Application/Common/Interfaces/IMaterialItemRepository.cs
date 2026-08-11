using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IMaterialItemRepository
{
    Task<MaterialItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MaterialItem>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default);
    Task AddAsync(MaterialItem entity, CancellationToken ct = default);
    void Update(MaterialItem entity);
    void Delete(MaterialItem entity);
}
