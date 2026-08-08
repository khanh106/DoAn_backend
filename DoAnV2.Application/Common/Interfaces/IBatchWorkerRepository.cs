using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Application.Common.Interfaces;

public interface IBatchWorkerRepository
{
    /// <summary>Lookup 1 BatchWorker theo (BatchId, UserId).</summary>
    Task<BatchWorker?> GetAsync(Guid batchId, Guid userId, CancellationToken ct = default);

    /// <summary>Danh sách worker của 1 batch.</summary>
    Task<IReadOnlyList<BatchWorker>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default);

    /// <summary>Các lô mà 1 User (Farmer) được phân công.</summary>
    Task<IReadOnlyList<BatchWorker>> GetByUserIdAsync(
        Guid userId,
        WorkerAssignmentStatus? status = null,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid batchId, Guid userId, CancellationToken ct = default);

    Task<bool> HasRepresentativeAsync(Guid batchId, CancellationToken ct = default);

    Task AddAsync(BatchWorker entity, CancellationToken ct = default);

    void Update(BatchWorker entity);

    void Remove(BatchWorker entity);
}
