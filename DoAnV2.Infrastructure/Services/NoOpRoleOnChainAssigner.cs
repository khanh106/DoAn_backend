using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using DoAnV2.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// Bản NoOp của IRoleOnChainAssigner.
/// Sẽ được thay thế bằng implementation thật (gọi Smart Contract qua Nethereum)
/// khi TASK 03 (BlockchainService) được triển khai.
/// Tạm thời ghi log + lưu BlockchainTransaction(FunctionName="grantRole") để truy vết.
/// </summary>
public class NoOpRoleOnChainAssigner : IRoleOnChainAssigner
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<NoOpRoleOnChainAssigner> _logger;

    public NoOpRoleOnChainAssigner(IUnitOfWork uow, ILogger<NoOpRoleOnChainAssigner> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<string?> GrantRoleAsync(string roleName, string walletAddress, CancellationToken ct = default)
    {
        var txHash = "0xGRANT" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        if (txHash.Length > 66) txHash = txHash[..66];

        var record = new BlockchainTransaction
        {
            WalletAddress = walletAddress,
            TransactionHash = txHash,
            ContractAddress = "0x0000000000000000000000000000000000000000",
            FunctionName = "grantRole",
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.PENDING,
            ErrorMessage = $"[NoOp] Stub grantRole({roleName}). Replace NoOpRoleOnChainAssigner in TASK 03."
        };

        await _uow.BlockchainTransactions.AddAsync(record, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogWarning(
            "[NoOp] GrantRole called: role={Role} address={Address} ➔ txHash={Tx}. " +
            "Replace NoOpRoleOnChainAssigner with real implementation in TASK 03.",
            roleName, walletAddress, txHash);

        return txHash;
    }
}
