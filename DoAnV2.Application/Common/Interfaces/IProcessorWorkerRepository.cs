using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Application.Common.Interfaces;

public interface IProcessorWorkerRepository
{
    Task<ProcessorWorker?> GetAsync(Guid processorId, Guid workerId, CancellationToken ct = default);
    Task<ProcessorWorker?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProcessorWorker>> GetByProcessorIdAsync(Guid processorId, CoopWorkerLinkStatus? status = null, CancellationToken ct = default);
    Task<IReadOnlyList<ProcessorWorker>> GetByWorkerIdAsync(Guid workerId, CoopWorkerLinkStatus? status = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid processorId, Guid workerId, CancellationToken ct = default);
    Task AddAsync(ProcessorWorker entity, CancellationToken ct = default);
    void Update(ProcessorWorker entity);
    void Remove(ProcessorWorker entity);
}
