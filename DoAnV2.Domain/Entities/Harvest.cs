using DoAnV2.Domain.Common;

namespace DoAnV2.Domain.Entities;

/// <summary>Đợt thu hoạch . Đại diện xác nhận + gọi Smart Contract (BR-09).</summary>
public class Harvest : BaseEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    /// <summary>Người đại diện xác nhận thu hoạch.</summary>
    public Guid RepresentativeUserId { get; set; }
    public User RepresentativeUser { get; set; } = null!;

    public DateTime HarvestDate { get; set; }
    public double Quantity { get; set; }
    public string Unit { get; set; } = null!;
    public string InitialQuality { get; set; } = null!;

    public string? MetadataURI { get; set; }
    public string? DataHash { get; set; }
}