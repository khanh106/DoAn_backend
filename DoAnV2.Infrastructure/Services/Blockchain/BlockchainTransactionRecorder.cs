using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Infrastructure.Services.Blockchain;

/// <summary>
/// Triển khai IRecordBlockchainTransactionService (TASK 03 - Mục 3.3):
///   1. PENDING  ➔ tạo bản ghi với placeholder hash.
///   2. SUCCESS  ➔ cập nhật transactionHash + blockNumber.
///   3. FAILED   ➔ cập nhật ErrorMessage, KHÔNG rollback các bảng nghiệp vụ khác (BR-42).
/// </summary>
public class BlockchainTransactionRecorder : IRecordBlockchainTransactionService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<BlockchainTransactionRecorder> _logger;

    public BlockchainTransactionRecorder(IUnitOfWork uow, ILogger<BlockchainTransactionRecorder> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<BlockchainTransaction> RecordPendingAsync(
        string functionName,
        string walletAddress,
        string contractAddress,
        Guid? batchId = null,
        Guid? subBatchId = null,
        string? pendingHashPlaceholder = null,
        CancellationToken ct = default)
    {
        var tx = new BlockchainTransaction
        {
            FunctionName = functionName,
            WalletAddress = walletAddress,
            ContractAddress = contractAddress,
            BatchId = batchId,
            SubBatchId = subBatchId,
            // TransactionHash là NOT NULL ➔ dùng placeholder cho PENDING.
            // Unique index sẽ KHÔNG xung đột vì Guid random.
            TransactionHash = pendingHashPlaceholder
                ?? $"PENDING-{Guid.NewGuid():N}",
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.PENDING,
        };

        await _uow.BlockchainTransactions.AddAsync(tx, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "BlockchainTx PENDING: fn={Function}, batch={BatchId}, sub={SubBatchId}, wallet={Wallet}",
            functionName, batchId, subBatchId, walletAddress);

        return tx;
    }

    public async Task RecordSuccessAsync(
        BlockchainTransaction pending,
        string transactionHash,
        long? blockNumber,
        CancellationToken ct = default)
    {
        pending.TransactionHash = transactionHash;
        pending.BlockNumber = blockNumber;
        pending.Status = TransactionStatus.SUCCESS;
        pending.ErrorMessage = null;
        pending.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "BlockchainTx SUCCESS: fn={Function}, tx={TxHash}, block={Block}",
            pending.FunctionName, transactionHash, blockNumber);
    }

    public async Task RecordFailedAsync(
        BlockchainTransaction pending,
        string errorMessage,
        string? transactionHash = null,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(transactionHash))
            pending.TransactionHash = transactionHash;
        pending.Status = TransactionStatus.FAILED;
        pending.ErrorMessage = errorMessage;
        pending.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        _logger.LogWarning(
            "BlockchainTx FAILED: fn={Function}, tx={TxHash}, error={Error}",
            pending.FunctionName, transactionHash, errorMessage);
    }
}
