using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Vận đơn giao hàng  BR-18: Retailer chỉ nhận khi lô ở STAGE_SHIPPING.</summary>
public class Shipment : BaseEntity
{
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }

    public Guid? SubBatchId { get; set; }
    public SubBatch? SubBatch { get; set; }

    /// <summary>PARENT (shipParent) / SUB (shipSub).</summary>
    public AssetType AssetType { get; set; }

    public string PickupLocation { get; set; } = null!;
    public string Destination { get; set; } = null!;

    public Guid RetailerId { get; set; }
    public User Retailer { get; set; } = null!;

    public string CarrierInfo { get; set; } = null!;
    public string ShippingCode { get; set; } = null!;
    public DateTime ShippingDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public double Weight { get; set; }
}