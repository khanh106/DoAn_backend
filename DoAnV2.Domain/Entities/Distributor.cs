using DoAnV2.Domain.Common;

namespace DoAnV2.Domain.Entities;

/// <summary>Đối tác / Nhà phân phối liên kết thu mua nông sản của Hợp tác xã.</summary>
public class Distributor : BaseEntity
{
    public Guid ProcessorId { get; set; }
    public User Processor { get; set; } = null!;

    public Guid? RetailerId { get; set; }
    public User? Retailer { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string Address { get; set; } = null!;
    public string? TaxCode { get; set; }
    public string Status { get; set; } = "ACTIVE";
}
