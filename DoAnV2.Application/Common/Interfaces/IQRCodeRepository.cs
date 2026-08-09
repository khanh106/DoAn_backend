using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Repository cho bảng QRCode - Mã QR truy xuất nguồn gốc (TASK 08 - Mục 8.3).
/// Áp dụng cho BATCH / SUBBATCH / BOX / COMMERCIAL (BR-17).
/// </summary>
public interface IQRCodeRepository
{
    Task<QRCode?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<QRCode>> GetByTargetAsync(
        QRTargetType targetType, Guid targetId, CancellationToken ct = default);

    Task<bool> ExistsByTargetAsync(
        QRTargetType targetType, Guid targetId, CancellationToken ct = default);

    Task AddAsync(QRCode entity, CancellationToken ct = default);
}
