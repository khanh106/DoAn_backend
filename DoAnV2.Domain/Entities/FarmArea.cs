using DoAnV2.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

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

    // [NotMapped] để EF KHÔNG tự tạo shadow FK "farm_area_id2"
    // Quan hệ Batch↔FarmArea đã được khai báo rõ trong Batch.cs + OnModelCreating.
    // Nếu để collection nav, EF sẽ tự tạo FK thứ 2 (farm_area_id2) gây lỗi khi INSERT.
    [NotMapped]
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}