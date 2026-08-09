using DoAnV2.Domain.Common;

namespace DoAnV2.Domain.Entities;

/// <summary>Sơ chế sau thu hoạch  Rửa, Làm sạch, Làm khô...</summary>
public class Processing : BaseEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    public string ProcessType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string? MetadataURI { get; set; }
    public string? DataHash { get; set; }
    public string ImageUrlsJson { get; set; } = "[]";
}