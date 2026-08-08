using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Repository cho CultivationLog (TASK 06 - Mục 6.1).
/// Nhật ký canh tác lưu OFF-CHAIN (SQL + IPFS ảnh), KHÔNG gọi Smart Contract (BR-07, BR-08).
/// </summary>
public interface ICultivationLogRepository
{
    Task<CultivationLog?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lấy danh sách log của 1 Batch (sắp xếp mới nhất trước).</summary>
    Task<IReadOnlyList<CultivationLog>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default);

    Task AddAsync(CultivationLog entity, CancellationToken ct = default);
}