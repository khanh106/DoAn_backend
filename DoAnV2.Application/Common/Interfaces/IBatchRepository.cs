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
    /// Update scalar columns (MetadataURI, DataHash, UpdatedAt) của Batch bằng raw SQL
    /// để tránh EF tracking navigation properties gây lỗi "association has been severed".
    /// </summary>
    Task UpdateMetadataAsync(Guid batchId, string metadataURI, string dataHash, DateTime updatedAt, CancellationToken ct = default);

    /// <summary>
    /// Xóa Batch + tất cả BatchWorkers liên quan bằng raw SQL DELETE,
    /// bypass hoàn toàn EF change tracker để tránh lỗi "association has been severed".
    /// </summary>
    Task DeleteBatchWithWorkersAsync(Guid batchId, CancellationToken ct = default);

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
