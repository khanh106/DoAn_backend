using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;

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

    /// <summary>
    /// TASK 11 - Mục 11.2: Lấy 1 record giao dịch theo Id (kèm Batch/SubBatch để kiểm tra dữ liệu off-chain).
    /// Dùng cho luồng Retry.
    /// </summary>
    Task<BlockchainTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// TASK 11 - Mục 11.2: Danh sách giao dịch Blockchain phục vụ trang giám sát của Admin.
    /// Hỗ trợ filter theo Status (FAILED/PENDING/SUCCESS), FunctionName, BatchId.
    /// Sắp xếp theo Timestamp giảm dần (mới nhất trước).
    /// </summary>
    Task<IReadOnlyList<BlockchainTransaction>> SearchAsync(
        TransactionStatus? status,
        string? functionName,
        Guid? batchId,
        CancellationToken ct = default);

    /// <summary>
    /// TASK 11 - Mục 11.1: Thống kê số lượng giao dịch theo trạng thái - dùng cho Dashboard.
    /// </summary>
    Task<(int Total, int Success, int Failed)> CountByStatusAsync(CancellationToken ct = default);
}
