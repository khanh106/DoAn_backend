namespace DoAnV2.Application.Common.Options;

/// <summary>
/// Cấu hình cấp phí gas (ETH) cho Custodial Wallet của Farmer.
/// BR-46.2: Ví Farmer được Admin tài trợ gas phí.
/// </summary>
public class WalletFundingOptions
{
    public const string SectionName = "WalletFunding";

    /// <summary>
    /// Bật/tắt cơ chế tự động cấp ETH khi Admin APPROVE Farmer.
    /// Trong môi trường không có RPC thật (CI, dev offline) có thể set = false.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Số ETH cấp cho mỗi Farmer khi APPROVE. Mặc định 0.003 ETH (~ đủ ~30 tx grantRole).
    /// </summary>
    public decimal FundAmountEth { get; set; } = 0.003m;

    /// <summary>
    /// Địa chỉ ví nhận lại ETH khi Sweep (mặc định = ví Admin deployer).
    /// Nếu để trống thì lấy từ BlockchainOptions.AdminAddress (TODO TASK 03).
    /// </summary>
    public string? SweepRecipientAddress { get; set; }

    /// <summary>
    /// Số ETH tối thiểu còn lại trong ví Farmer để KHÔNG sweep.
    /// Tránh sweep nhầm khi Farmer vừa nhận grant hoặc vừa gửi tx.
    /// </summary>
    public decimal MinFarmerBalanceToKeep { get; set; } = 0.0005m;

    /// <summary>
    /// Timeout (giây) chờ receipt tx khi gọi RPC thật.
    /// </summary>
    public int ReceiptTimeoutSeconds { get; set; } = 60;
}
