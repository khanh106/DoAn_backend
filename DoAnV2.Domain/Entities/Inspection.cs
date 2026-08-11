using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Giấy kiểm định  Áp dụng Parent hoặc Sub. Đạt → PACKAGED; Không đạt → dừng (BR-14, BR-15).</summary>
public class Inspection : BaseEntity
{
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }

    public Guid? SubBatchId { get; set; }
    public SubBatch? SubBatch { get; set; }

    /// <summary>PARENT (inspectParent) / SUB (inspectSub).</summary>
    public AssetType AssetType { get; set; }

    public string DocumentName { get; set; } = null!;
    public string DocumentNumber { get; set; } = null!;
    public string InspectionUnit { get; set; } = null!;
    public DateTime InspectionDate { get; set; }

    public InspectionResult Result { get; set; }

    /// <summary>URI PDF/PNG trên IPFS.</summary>
    public string FileURI { get; set; } = null!;
    public string? Note { get; set; }

    // ===== TASK 11: Lưu metadata IPFS để phục vụ Retry (BR-42) =====
    public string? MetadataURI { get; set; }
    public string? DataHash { get; set; }
}
