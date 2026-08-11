using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;

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

    /// <summary>
    /// TASK 11 - Mục 11.1: Thống kê số lượng Parent Batch theo từng trạng thái - dùng cho Dashboard Admin.
    /// </summary>
    Task<BatchStats> GetStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Batch>> GetByProcessorIdAsync(Guid processorId, CancellationToken ct = default);
}



public record BatchStats(
    int Total,
    int InProduction,       // STAGE_PLANTING
    int Harvested,          // STAGE_HARVESTED
    int Packaged,           // PACKAGED
    int ReadyForSale);      // READY_FOR_SALE
