using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Phiếu đóng gói  Chỉ sau khi kiểm định đạt (BR-14).</summary>
public class Packaging : BaseEntity
{
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }

    public Guid? SubBatchId { get; set; }
    public SubBatch? SubBatch { get; set; }

    /// <summary>PARENT (packageParent) / SUB (packageSub).</summary>
    public AssetType AssetType { get; set; }

    public DateTime PackDate { get; set; }
    public double Weight { get; set; }
    public string Specification { get; set; } = null!;
    public string? UsageGuide { get; set; }
    public string? StorageGuide { get; set; }
    public string? Color { get; set; }
    public string? Smell { get; set; }
    public string? Standard { get; set; }
    public string ImageUrlsJson { get; set; } = "[]";
    public string? Note { get; set; }
}