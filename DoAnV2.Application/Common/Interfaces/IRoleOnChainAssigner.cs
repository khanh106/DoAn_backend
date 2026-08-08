namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Trừu tượng hoá việc cấp role on-chain cho user (sẽ được triển khai
/// đầy đủ ở TASK 03 khi có IBlockchainService + Nethereum).
/// Ở TASK 02, mặc định triển khai là NoOp - chỉ log và trả về null txHash.
/// </summary>
public interface IRoleOnChainAssigner
{
    /// <summary>
    /// Cấp role on-chain cho địa chỉ ví.
    /// Trả về transaction hash (null nếu không thực hiện).
    /// </summary>
    Task<string?> GrantRoleAsync(string roleName, string walletAddress, CancellationToken ct = default);
}
