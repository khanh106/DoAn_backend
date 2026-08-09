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

    /// <summary>
    /// TASK 10 - Mục 10.1: Tìm QRCode theo chuỗi QRValue (URL truy xuất công khai).
    /// Dùng để resolve code từ QR scan về target (BATCH/SUBBATCH/BOX/COMMERCIAL).
    /// </summary>
    Task<QRCode?> GetByQRValueAsync(string qrValue, CancellationToken ct = default);
}
