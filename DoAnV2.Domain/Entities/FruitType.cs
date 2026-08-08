using DoAnV2.Domain.Common;

namespace DoAnV2.Domain.Entities;

/// <summary>Loại hoa quả (Chương 10.1). Thuộc về một Processor - đảm bảo độc lập dữ liệu giữa các HTX (BR-10.3).</summary>
public class FruitType : BaseEntity
{
    public Guid ProcessorId { get; set; }
    public User Processor { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Mã ngắn (CAM, BUOI...) - thường làm tiền tố BatchCode.</summary>
    public string Code { get; set; } = null!;

    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";

    public ICollection<Product> Products { get; set; } = new List<Product>();
}