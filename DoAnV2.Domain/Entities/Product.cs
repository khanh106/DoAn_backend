using DoAnV2.Domain.Common;

namespace DoAnV2.Domain.Entities;

/// <summary>Sản phẩm cụ thể trong FruitType (Chương 10.2).</summary>
public class Product : BaseEntity
{
    public Guid FruitTypeId { get; set; }
    public FruitType FruitType { get; set; } = null!;

    public string GroupName { get; set; } = null!;
    public string ProductType { get; set; } = null!;
    public string Variety { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
}