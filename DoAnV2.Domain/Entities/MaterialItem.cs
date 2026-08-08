using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Vật tư nông nghiệp : Nông dược, Phân bón, Nguyên vật liệu, Thiết bị.</summary>
public class MaterialItem : BaseEntity
{
    public Guid ProcessorId { get; set; }
    public User Processor { get; set; } = null!;

    public ItemType ItemType { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public decimal Price { get; set; }
    public double QuantityInStock { get; set; }

    /// <summary>Lượng dùng / ha (Nông dược, Phân bón).</summary>
    public double? DosagePerHa { get; set; }
    public double? Concentration { get; set; }
    public string? Supplier { get; set; }

    /// <summary>Tỉ lệ NPK (VD: "16-16-8").</summary>
    public string? NPKRatio { get; set; }
    public string? Note { get; set; }

    public ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
}