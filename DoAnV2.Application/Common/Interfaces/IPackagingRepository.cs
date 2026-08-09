using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Repository cho bảng Packaging - Phiếu đóng gói thương mại (TASK 08 - Mục 8.2).
/// Chỉ sau khi kiểm định đạt (INSPECTION_PASSED) mới được đóng gói (BR-14).
/// </summary>
public interface IPackagingRepository
{
    Task<Packaging?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Packaging>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default);

    Task<IReadOnlyList<Packaging>> GetBySubBatchIdAsync(
        Guid subBatchId, CancellationToken ct = default);

    Task AddAsync(Packaging entity, CancellationToken ct = default);
}
