using DoAnV2.Domain.Common;

namespace DoAnV2.Domain.Entities;

/// <summary>Nhật ký canh tác  Lưu Off-chain, KHÔNG gọi Smart Contract mỗi log (BR-08).</summary>
public class CultivationLog : BaseEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>"Tưới nước" / "Bón phân" / "Phun thuốc" / "Chăm sóc".</summary>
    public string ActivityType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime LogDate { get; set; }

    public string? MetadataURI { get; set; }

    /// <summary>Mảng URL hình ảnh dạng JSON.</summary>
    public string ImageUrlsJson { get; set; } = "[]";
}