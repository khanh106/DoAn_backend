using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>LÔ CON - sinh ra sau phân loại/tách lô (Chương 21, BR-16).
/// Sau đó có thể áp dụng Kiểm định/Đóng gói/Vận chuyển riêng (gọi hàm *Sub).
/// BR-17: Truy ngược SubBatch → Parent Batch.</summary>
public class SubBatch : BaseEntity
{
    /// <summary>Mã lô con Unique - VD: "SUB-2026-001-1".</summary>
    public string SubBatchCode { get; set; } = null!;

    public Guid ParentBatchId { get; set; }
    public Batch ParentBatch { get; set; } = null!;

    /// <summary>"Loại 1" / "Loại 2" / "Loại 3".</summary>
    public string Classification { get; set; } = null!;
    public double Quantity { get; set; }
    public string? PackageCode { get; set; }
    public string? QRCode { get; set; }

    public BatchStage CurrentStage { get; set; } = BatchStage.STAGE_SORTED;

    public string? MetadataURI { get; set; }
    public string? DataHash { get; set; }

    // Navigation
    public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    public ICollection<Packaging> Packagings { get; set; } = new List<Packaging>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<BlockchainTransaction> BlockchainTransactions { get; set; } = new List<BlockchainTransaction>();
}