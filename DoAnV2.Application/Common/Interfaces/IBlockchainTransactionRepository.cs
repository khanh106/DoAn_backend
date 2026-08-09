using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IBlockchainTransactionRepository
{
    Task AddAsync(BlockchainTransaction tx, CancellationToken ct = default);
    Task<BlockchainTransaction?> GetByTxHashAsync(string txHash, CancellationToken ct = default);
    Task<IReadOnlyList<BlockchainTransaction>> GetByWalletAddressAsync(string walletAddress, CancellationToken ct = default);

    /// <summary>
    /// TASK 10 - Mục 10.1: Lấy toàn bộ giao dịch On-chain liên quan tới 1 Parent Batch
    /// (bao gồm cả các SubBatch con của nó). Sắp xếp theo Timestamp tăng dần để dựng timeline.
    /// </summary>
    Task<IReadOnlyList<BlockchainTransaction>> GetHistoryForBatchAsync(
        Guid batchId, CancellationToken ct = default);

    /// <summary>
    /// TASK 10 - Mục 10.1: Lấy toàn bộ giao dịch On-chain liên quan tới 1 SubBatch.
    /// </summary>
    Task<IReadOnlyList<BlockchainTransaction>> GetHistoryForSubBatchAsync(
        Guid subBatchId, CancellationToken ct = default);
}
