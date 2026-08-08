using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Nhật ký nhập/xuất kho (Chương 13).</summary>
public class InventoryLog : BaseEntity
{
    public Guid MaterialItemId { get; set; }
    public MaterialItem MaterialItem { get; set; } = null!;

    public InventoryTransactionType TransactionType { get; set; }
    public double Quantity { get; set; }
    public DateTime TransactionDate { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? Note { get; set; }
}