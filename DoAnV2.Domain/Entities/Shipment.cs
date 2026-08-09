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
    public DateTime? ReadyForSaleDate { get; set; }
    public double Weight { get; set; }

    // ===== IPFS + Blockchain metadata cho từng giai đoạn =====

    /// <summary>MetadataURI upload khi Processor gọi shipParent / shipSub (TASK 09 - Mục 9.1).</summary>
    public string? MetadataURI { get; set; }
    public string? DataHash { get; set; }
    public string? ShipTransactionHash { get; set; }

    /// <summary>MetadataURI upload khi Retailer gọi receiveParent / receiveSub (TASK 09 - Mục 9.2).</summary>
    public string? ReceiveMetadataURI { get; set; }
    public string? ReceiveDataHash { get; set; }
    public string? ReceiveTransactionHash { get; set; }

    /// <summary>MetadataURI upload khi Retailer gọi readyParent / readySub (TASK 09 - Mục 9.2).</summary>
    public string? ReadyMetadataURI { get; set; }
    public string? ReadyDataHash { get; set; }
    public string? ReadyTransactionHash { get; set; }
}