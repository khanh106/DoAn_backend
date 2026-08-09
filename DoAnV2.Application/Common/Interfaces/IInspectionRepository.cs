using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Repository cho bảng Inspection - Giấy kiểm định chất lượng (TASK 08 - Mục 8.1).
/// Áp dụng cho Parent Batch hoặc SubBatch. Đạt → PACKAGED; Không đạt → dừng (BR-14, BR-15).
/// </summary>
public interface IInspectionRepository
{
    Task<Inspection?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Inspection>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default);

    Task<IReadOnlyList<Inspection>> GetBySubBatchIdAsync(
        Guid subBatchId, CancellationToken ct = default);

    Task AddAsync(Inspection entity, CancellationToken ct = default);
}
