using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
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

    public async Task<IReadOnlyList<BlockchainTransaction>> GetByWalletAddressAsync(string walletAddress, CancellationToken ct = default)
        => await _db.BlockchainTransactions
            .Where(x => x.WalletAddress == walletAddress)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(ct);
}
