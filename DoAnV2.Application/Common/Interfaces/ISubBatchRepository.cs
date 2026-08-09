using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Repository cho bảng SubBatch - Lô con sinh ra từ công đoạn phân loại/tách lô (TASK 07 - Mục 7.3, BR-16).
/// </summary>
public interface ISubBatchRepository
{
    Task<SubBatch?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<SubBatch?> GetBySubBatchCodeAsync(string subBatchCode, CancellationToken ct = default);

    Task<bool> SubBatchCodeExistsAsync(string subBatchCode, CancellationToken ct = default);

    Task<IReadOnlyList<SubBatch>> GetByParentBatchIdAsync(
        Guid parentBatchId, CancellationToken ct = default);

    Task AddAsync(SubBatch entity, CancellationToken ct = default);

    /// <summary>
    /// TASK 10 - Mục 10.1: Lấy SubBatch kèm các Navigation cần thiết cho truy xuất công khai.
    /// Bao gồm: ParentBatch, Inspections, Packagings, Shipments (kèm Retailer).
    /// </summary>
    Task<SubBatch?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// TASK 10 - Mục 10.1: Đánh dấu entity đã thay đổi - dùng cho EF Core change tracking.</summary>
    void Update(SubBatch entity);
}
