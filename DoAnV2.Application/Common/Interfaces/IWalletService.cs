namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Service sinh / quản lý Custodial Wallet cho Farmer (BR-46.1).
/// - Ethereum: dùng Nethereum.Signer (đã có sẵn).
/// - Bitcoin : dùng NBitcoin.
/// - Private Key phải được mã hóa AES-256 trước khi lưu DB (EncryptedPrivateKey).
/// </summary>
public interface IWalletService
{
    /// <summary>Sinh cặp key Ethereum mới, trả về (PublicAddress, EncryptedPrivateKey).</summary>
    (string WalletAddress, string EncryptedPrivateKey) GenerateEthereumWallet(string encryptionKey);

    /// <summary>Sinh cặp key Bitcoin mới, trả về (PublicAddress, EncryptedPrivateKey).</summary>
    (string WalletAddress, string EncryptedPrivateKey) GenerateBitcoinWallet(string encryptionKey);

    /// <summary>Mã hóa Private Key bằng AES-256 trước khi lưu DB.</summary>
    string EncryptPrivateKey(string plainPrivateKey, string encryptionKey);

    /// <summary>Giải mã EncryptedPrivateKey (chỉ dùng nội bộ khi ký giao dịch).</summary>
    string DecryptPrivateKey(string encryptedPrivateKey, string encryptionKey);
}