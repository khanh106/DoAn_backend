using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IFarmAreaRepository
{
    Task<FarmArea?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FarmArea>> GetByProcessorIdAsync(
        Guid processorId,
        string? province = null,
        string? district = null,
        string? ward = null,
        string? plantingCode = null,
        CancellationToken ct = default);
    Task AddAsync(FarmArea entity, CancellationToken ct = default);
    void Update(FarmArea entity);
}