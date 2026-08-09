using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IBatchRepository
{
    Task<Batch?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lookup batch by Guid kèm Include Workers + FruitType + Product + FarmArea + Processor.</summary>
    Task<Batch?> GetByIdWithWorkersAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lookup batch by BatchCode.</summary>
    Task<Batch?> GetByBatchCodeAsync(string batchCode, CancellationToken ct = default);

    Task<bool> BatchCodeExistsAsync(string batchCode, CancellationToken ct = default);

    /// <summary>
    /// TASK 10 - Mục 10.1: Lấy Batch kèm đầy đủ thông tin cho truy xuất công khai:
    /// Workers, FruitType, Product, FarmArea, RepresentativeWorker, Processor.
    /// </summary>
    Task<Batch?> GetByIdWithFullChainAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Batch entity, CancellationToken ct = default);

    void Update(Batch entity);

    void Remove(Batch entity);
}
