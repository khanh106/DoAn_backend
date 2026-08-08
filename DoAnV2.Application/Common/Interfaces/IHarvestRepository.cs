using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Repository cho Harvest (TASK 06 - Mục 6.2 & 6.3).
/// Mỗi Harvest đại diện cho 1 lần xác nhận thu hoạch của lô (gọi SC harvestBatch) HOẶC
/// tiếp nhận sau thu hoạch của Processor (gọi SC receiveBatch).
/// </summary>
public interface IHarvestRepository
{
    Task<Harvest?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Danh sách các đợt Harvest của 1 Batch (mới nhất trước).</summary>
    Task<IReadOnlyList<Harvest>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default);

    Task AddAsync(Harvest entity, CancellationToken ct = default);
}