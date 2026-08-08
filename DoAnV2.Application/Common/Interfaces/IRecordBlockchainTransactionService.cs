using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Helper service quản lý vòng đời của một BlockchainTransaction
/// (TASK 03 - Mục 3.3: PENDING ➔ SUCCESS/FAILED).
/// Tách riêng để BlockchainService chỉ lo gọi SC, còn việc ghi log DB do helper này đảm nhiệm.
/// BR-42: Không rollback SQL Data khi thất bại, đánh dấu lỗi + lưu ErrorMessage để Admin retry.
/// </summary>
public interface IRecordBlockchainTransactionService
{
    /// <summary>
    /// Tạo bản ghi BlockchainTransaction trạng thái PENDING với
    /// FunctionName / WalletAddress / BatchId / SubBatchId / ContractAddress.
    /// </summary>
    /// <returns>Bản ghi vừa tạo (chưa có transactionHash thật, dùng placeholder).</returns>
    Task<BlockchainTransaction> RecordPendingAsync(
        string functionName,
        string walletAddress,
        string contractAddress,
        Guid? batchId = null,
        Guid? subBatchId = null,
        string? pendingHashPlaceholder = null,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật bản ghi PENDING thành SUCCESS với transactionHash thật + blockNumber.
    /// </summary>
    Task RecordSuccessAsync(
        BlockchainTransaction pending,
        string transactionHash,
        long? blockNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật bản ghi PENDING thành FAILED kèm ErrorMessage.
    /// Nếu đã có transactionHash thì vẫn lưu lại (tx đã broadcast nhưng revert).
    /// </summary>
    Task RecordFailedAsync(
        BlockchainTransaction pending,
        string errorMessage,
        string? transactionHash = null,
        CancellationToken ct = default);
}
