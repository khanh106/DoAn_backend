using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IInventoryLogRepository
{
    Task AddAsync(InventoryLog entity, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryLog>> GetLogsAsync(Guid processorId, Guid? materialItemId = null, CancellationToken ct = default);
}