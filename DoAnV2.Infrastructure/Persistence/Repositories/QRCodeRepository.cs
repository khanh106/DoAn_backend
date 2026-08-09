using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai IQRCodeRepository (TASK 08 - Mục 8.3).
/// Lưu trữ thông tin QR code truy xuất nguồn gốc (BR-17).
/// </summary>
public class QRCodeRepository : IQRCodeRepository
{
    private readonly ApplicationDbContext _db;

    public QRCodeRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<QRCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.QRCodes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<QRCode>> GetByTargetAsync(
        QRTargetType targetType, Guid targetId, CancellationToken ct = default)
    {
        return await _db.QRCodes
            .Where(x => x.TargetType == targetType && x.TargetId == targetId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsByTargetAsync(
        QRTargetType targetType, Guid targetId, CancellationToken ct = default)
        => _db.QRCodes.AnyAsync(
            x => x.TargetType == targetType && x.TargetId == targetId, ct);

    public async Task AddAsync(QRCode entity, CancellationToken ct = default)
        => await _db.QRCodes.AddAsync(entity, ct);
}
