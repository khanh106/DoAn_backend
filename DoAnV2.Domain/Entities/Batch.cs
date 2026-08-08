using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>LÔ SẢN XUẤT - thực thể trung tâm. Đi qua 10 trạng thái (Chương 30).</summary>
public class Batch : BaseEntity
{
    /// <summary>Mã lô Unique - VD: "BATCH-2026-001".</summary>
    public string BatchCode { get; set; } = null!;

    public Guid FruitTypeId { get; set; }
    public FruitType FruitType { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid FarmAreaId { get; set; }
    public FarmArea FarmArea { get; set; } = null!;

    public DateTime PlantingDate { get; set; }
    public double ExpectedQuantity { get; set; }

    /// <summary>Người đại diện của lô .</summary>
    public Guid? RepresentativeWorkerId { get; set; }
    public User? RepresentativeWorker { get; set; }

    public BatchStage CurrentStage { get; set; } = BatchStage.STAGE_PLANTING;

    public string? MetadataURI { get; set; }
    public string? DataHash { get; set; }
    public string? BlockchainBatchId { get; set; }

    public Guid ProcessorId { get; set; }
    public User Processor { get; set; } = null!;

    // Navigation
    public ICollection<BatchWorker> BatchWorkers { get; set; } = new List<BatchWorker>();
    public ICollection<CultivationLog> CultivationLogs { get; set; } = new List<CultivationLog>();
    public ICollection<Harvest> Harvests { get; set; } = new List<Harvest>();
    public ICollection<Processing> Processings { get; set; } = new List<Processing>();
    public ICollection<SubBatch> SubBatches { get; set; } = new List<SubBatch>();
    public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    public ICollection<Packaging> Packagings { get; set; } = new List<Packaging>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<BlockchainTransaction> BlockchainTransactions { get; set; } = new List<BlockchainTransaction>();
}