using DoAnV2.Domain.Common;

namespace DoAnV2.Domain.Entities;

/// <summary>Vùng trồng (Chương 11) - thuộc Processor.</summary>
public class FarmArea : BaseEntity
{
    public Guid ProcessorId { get; set; }
    public User Processor { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string OwnerName { get; set; } = null!;
    public string Province { get; set; } = null!;
    public string District { get; set; } = null!;
    public string Ward { get; set; } = null!;
    public double Area { get; set; }
    public string? SoilType { get; set; }

    /// <summary>GPS dạng "lat,lng" hoặc GeoJSON.</summary>
    public string? GPS { get; set; }

    public string? PlantingCode { get; set; }

    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}