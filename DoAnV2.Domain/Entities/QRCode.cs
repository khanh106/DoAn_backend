using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Mã QR truy xuất  Người dùng quét → truy ngược SubBatch → Batch (BR-17).</summary>
public class QRCode : BaseEntity
{
    /// <summary>BATCH / SUBBATCH / BOX / COMMERCIAL.</summary>
    public QRTargetType TargetType { get; set; }

    public Guid TargetId { get; set; }

    /// <summary>URL truy xuất + checksum.</summary>
    public string QRValue { get; set; } = null!;

    public QRCodeStatus Status { get; set; } = QRCodeStatus.ACTIVE;
}

public enum QRCodeStatus
{
    ACTIVE = 1,
    INACTIVE = 2
}