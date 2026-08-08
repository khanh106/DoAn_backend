using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IFruitTypeRepository
{
    Task<FruitType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FruitType>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default);
    Task<bool> CodeExistsForProcessorAsync(string code, Guid processorId, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(FruitType entity, CancellationToken ct = default);
    void Update(FruitType entity);
}