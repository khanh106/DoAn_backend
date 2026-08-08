using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Lịch sử giao dịch Blockchain . FAILED → cho retry (Chương 42.1).
/// BR-20: Không xóa lịch sử Blockchain.</summary>
public class BlockchainTransaction : BaseEntity
{
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }

    public Guid? SubBatchId { get; set; }
    public SubBatch? SubBatch { get; set; }

    /// <summary>Ví thực hiện giao dịch (ActorWallet).</summary>
    public string WalletAddress { get; set; } = null!;

    /// <summary>Hash giao dịch - Unique.</summary>
    public string TransactionHash { get; set; } = null!;
    public string ContractAddress { get; set; } = null!;

    /// <summary>Tên hàm Smart Contract: createBatch, harvestBatch, inspectParent...</summary>
    public string FunctionName { get; set; } = null!;

    public long? BlockNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.PENDING;
    public string? ErrorMessage { get; set; }
}