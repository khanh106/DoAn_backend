using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using DoAnV2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class BlockchainTransactionRepository : IBlockchainTransactionRepository
{
    private readonly ApplicationDbContext _db;

    public BlockchainTransactionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(BlockchainTransaction tx, CancellationToken ct = default)
        => await _db.BlockchainTransactions.AddAsync(tx, ct);

    public async Task<BlockchainTransaction?> GetByTxHashAsync(string txHash, CancellationToken ct = default)
        => await _db.BlockchainTransactions
            .FirstOrDefaultAsync(x => x.TransactionHash == txHash, ct);

    public async Task<BlockchainTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.BlockchainTransactions
            .Include(x => x.Batch)
            .Include(x => x.SubBatch)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<BlockchainTransaction>> GetByWalletAddressAsync(string walletAddress, CancellationToken ct = default)
        => await _db.BlockchainTransactions
            .Where(x => x.WalletAddress == walletAddress)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BlockchainTransaction>> GetHistoryForBatchAsync(
        Guid batchId, CancellationToken ct = default)
    {
        // Lấy tất cả giao dịch trực tiếp của Parent + các SubBatch con của nó.
        var directBatchTx = _db.BlockchainTransactions
            .Where(x => x.BatchId == batchId);

        var subBatchIds = _db.SubBatches
            .Where(s => s.ParentBatchId == batchId)
            .Select(s => s.Id);

        var subBatchTx = _db.BlockchainTransactions
            .Where(x => x.SubBatchId != null && subBatchIds.Contains(x.SubBatchId.Value));

        return await directBatchTx
            .Union(subBatchTx)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BlockchainTransaction>> GetHistoryForSubBatchAsync(
        Guid subBatchId, CancellationToken ct = default)
    {
        return await _db.BlockchainTransactions
            .Where(x => x.SubBatchId == subBatchId || x.Batch!.SubBatches.Any(sb => sb.Id == subBatchId))
            .OrderBy(x => x.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BlockchainTransaction>> SearchAsync(
        TransactionStatus? status,
        string? functionName,
        Guid? batchId,
        CancellationToken ct = default)
    {
        IQueryable<BlockchainTransaction> query = _db.BlockchainTransactions
            .AsNoTracking()
            .Include(x => x.Batch)
            .Include(x => x.SubBatch);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(functionName))
            query = query.Where(x => x.FunctionName == functionName);

        if (batchId.HasValue)
            query = query.Where(x => x.BatchId == batchId.Value);

        return await query
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<(int Total, int Success, int Failed)> CountByStatusAsync(CancellationToken ct = default)
    {
        var statusGroups = await _db.BlockchainTransactions
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = statusGroups.Sum(x => x.Count);
        var success = statusGroups.FirstOrDefault(x => x.Status == TransactionStatus.SUCCESS)?.Count ?? 0;
        var failed = statusGroups.FirstOrDefault(x => x.Status == TransactionStatus.FAILED)?.Count ?? 0;

        return (total, success, failed);
    }
}
