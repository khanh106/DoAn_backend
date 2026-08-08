namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Tổng hợp các thao tác on-chain liên quan tới user / custodial wallet.
/// Tách riêng IBlockchainService (gọi SC, quản lý gas, sweep) với
/// IRoleOnChainAssigner (chỉ phần grant role) để dễ tái sử dụng ở TASK 03.
///
/// TASK 02 cung cấp NoOp implementation (chỉ log + ghi BlockchainTransaction).
/// TASK 03 sẽ thay bằng implementation Nethereum thật.
/// </summary>
public interface IBlockchainService
{
    /// <summary>
    /// Cấp ETH cho ví Custodial Wallet của Farmer (Admin ví gửi ➔ Farmer ví nhận).
    /// BR-46.2: Ví Farmer được Admin tài trợ gas fee.
    /// Trả về transaction hash (null nếu NoOp).
    /// </summary>
    Task<string?> FundFarmerWalletAsync(string farmerWalletAddress, decimal amountEth, CancellationToken ct = default);

    /// <summary>
    /// Thu hồi toàn bộ ETH (trừ MinFarmerBalanceToKeep) từ ví Farmer về ví Admin.
    /// BR-46.2: Cơ chế thu hồi khi ví không sử dụng / user bị REJECT / admin bấm Sweep.
    /// Trả về transaction hash (null nếu NoOp hoặc không có gì để sweep).
    /// </summary>
    Task<string?> SweepFarmerWalletAsync(string farmerWalletAddress, CancellationToken ct = default);
}
