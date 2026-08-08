using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// Bản NoOp của IBlockchainService.
/// Tương tự NoOpRoleOnChainAssigner: chỉ ghi log + ghi BlockchainTransaction
/// (Status = PENDING) để truy vết. Khi TASK 03 hoàn thành Nethereum,
/// thay bằng impl thật sẽ gửi tx + đợi receipt + cập nhật Status = SUCCESS.
/// </summary>
public class NoOpBlockchainService : IBlockchainService
{
    private readonly IUnitOfWork _uow;
    private readonly WalletFundingOptions _options;
    private readonly ILogger<NoOpBlockchainService> _logger;

    public NoOpBlockchainService(
        IUnitOfWork uow,
        IOptions<WalletFundingOptions> options,
        ILogger<NoOpBlockchainService> logger)
    {
        _uow = uow;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> FundFarmerWalletAsync(string farmerWalletAddress, decimal amountEth, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "[NoOp] WalletFunding.Enabled=false ➔ bỏ qua fund ví {Addr}.",
                farmerWalletAddress);
            return null;
        }

        var txHash = GenerateFakeTxHash(farmerWalletAddress);

        var record = new BlockchainTransaction
        {
            WalletAddress = farmerWalletAddress,
            TransactionHash = txHash,
            ContractAddress = "0x0000000000000000000000000000000000000000", // ETH transfer (không phải SC)
            FunctionName = "fundFarmerWallet",
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.PENDING,
            ErrorMessage = "[NoOp] Stub - chờ TASK 03 thay bằng Nethereum Web3 transaction."
        };

        await _uow.BlockchainTransactions.AddAsync(record, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogWarning(
            "[NoOp] FundFarmerWallet: addr={Addr} amount={Amount} ETH ➔ txHash={Tx}. " +
            "Replace NoOpBlockchainService with real Nethereum impl in TASK 03.",
            farmerWalletAddress, amountEth, txHash);

        return txHash;
    }

    public async Task<string?> SweepFarmerWalletAsync(string farmerWalletAddress, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "[NoOp] WalletFunding.Enabled=false ➔ bỏ qua sweep ví {Addr}.",
                farmerWalletAddress);
            return null;
        }

        var txHash = GenerateFakeTxHash(farmerWalletAddress, sweep: true);

        var record = new BlockchainTransaction
        {
            WalletAddress = farmerWalletAddress,
            TransactionHash = txHash,
            ContractAddress = "0x0000000000000000000000000000000000000000",
            FunctionName = "sweepFarmerWallet",
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.PENDING,
            ErrorMessage = "[NoOp] Stub - chờ TASK 03. Cần đọc balance on-chain trước khi sweep."
        };

        await _uow.BlockchainTransactions.AddAsync(record, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogWarning(
            "[NoOp] SweepFarmerWallet: addr={Addr} → Admin ➔ txHash={Tx}. " +
            "Replace NoOpBlockchainService with real Nethereum impl in TASK 03.",
            farmerWalletAddress, txHash);

        return txHash;
    }

    private static string GenerateFakeTxHash(string address, bool sweep = false)
    {
        // Sinh chuỗi 0x + 64 hex chars để trông giống txHash thật.
        // Không lưu vào DB trong chain thật.
        var prefix = sweep ? "0xSWEEPFARM" : "0xFUNDFARM";
        var rand = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var hex = prefix + rand;
        return hex.Length >= 66 ? hex[..66] : hex.PadRight(66, '0');
    }
}
