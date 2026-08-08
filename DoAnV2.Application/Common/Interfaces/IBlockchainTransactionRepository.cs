using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

public interface IBlockchainTransactionRepository
{
    Task AddAsync(BlockchainTransaction tx, CancellationToken ct = default);
    Task<BlockchainTransaction?> GetByTxHashAsync(string txHash, CancellationToken ct = default);
    Task<IReadOnlyList<BlockchainTransaction>> GetByWalletAddressAsync(string walletAddress, CancellationToken ct = default);
}
