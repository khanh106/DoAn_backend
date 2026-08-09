using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Repository cho bảng Processing - ghi nhận công đoạn Sơ chế (TASK 07 - Mục 7.1).
/// </summary>
public interface IProcessingRepository
{
    Task<Processing?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Processing>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default);

    Task AddAsync(Processing entity, CancellationToken ct = default);
}
