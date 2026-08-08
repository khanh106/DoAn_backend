using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Infrastructure.Services.Blockchain;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// Triển khai thật IRoleOnChainAssigner bằng cách gọi
/// IBlockchainService.GrantRoleAsync (TASK 03 thay thế cho NoOp).
///
/// Tách riêng interface để các handler (ApproveUserCommandHandler) không cần
/// phụ thuộc trực tiếp vào IBlockchainService.
/// </summary>
public class BlockchainRoleAssigner : IRoleOnChainAssigner
{
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<BlockchainRoleAssigner> _logger;

    public BlockchainRoleAssigner(
        IBlockchainService blockchain,
        ILogger<BlockchainRoleAssigner> logger)
    {
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<string?> GrantRoleAsync(string roleName, string walletAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            _logger.LogWarning("GrantRole skipped: walletAddress rỗng.");
            return null;
        }

        // Map tên role app (FARMER/PROCESSOR/RETAILER) ➔ hằng số SC.
        var scRole = BlockchainRoleNames.FromAppRole(roleName);

        try
        {
            return await _blockchain.GrantRoleAsync(scRole, walletAddress, signerPrivateKey: null, ct: ct);
        }
        catch (Exception ex)
        {
            // Không throw: nếu grant role lỗi, user vẫn có thể được APPROVED trong DB
            // và Admin sẽ retry thủ công (BR-42 - không rollback).
            _logger.LogError(ex, "GrantRole failed for {Role} {Wallet}", scRole, walletAddress);
            return null;
        }
    }
}
