using System.Security.Cryptography;
using System.Text;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using Microsoft.Extensions.Options;
using NBitcoin;
using Nethereum.Signer;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// Triển khai IWalletService:
/// - Ethereum: dùng Nethereum.Signer (EthECKey) sinh cặp key.
/// - Bitcoin : dùng NBitcoin.Key sinh cặp key.
/// - Private Key được mã hoá AES-256-CBC trước khi trả về lưu DB (BR-46.1).
/// Định dạng ciphertext: base64( IV(16) || ciphertext ).
/// </summary>
public class WalletService : IWalletService
{
    private readonly WalletOptions _walletOptions;

    public WalletService(IOptions<WalletOptions> walletOptions)
    {
        _walletOptions = walletOptions.Value;
    }

    public (string WalletAddress, string EncryptedPrivateKey) GenerateEthereumWallet(string encryptionKey)
    {
        // Tạo private key hex ngẫu nhiên 32 bytes (64 hex chars) rồi truyền cho EthECKey.
        var privateKeyBytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(privateKeyBytes);
        var privateKeyHex = "0x" + Convert.ToHexString(privateKeyBytes).ToLowerInvariant();

        var key = new EthECKey(privateKeyHex);
        var address = key.GetPublicAddress();      // "0x..." lowercase checksummed

        return (address, EncryptPrivateKeyInternal(privateKeyHex, encryptionKey));
    }

    public (string WalletAddress, string EncryptedPrivateKey) GenerateBitcoinWallet(string encryptionKey)
    {
        var key = new Key(); // NBitcoin random key
        var wif = key.GetWif(_walletOptions.CustodialMode ? Network.Main : Network.TestNet).ToWif();
        var address = key.GetAddress(ScriptPubKeyType.Legacy, _walletOptions.CustodialMode ? Network.Main : Network.TestNet).ToString();

        return (address, EncryptPrivateKeyInternal(wif, encryptionKey));
    }

    public string EncryptPrivateKey(string plainPrivateKey, string encryptionKey)
        => EncryptPrivateKeyInternal(plainPrivateKey, encryptionKey);

    public string DecryptPrivateKey(string encryptedPrivateKey, string encryptionKey)
        => DecryptPrivateKeyInternal(encryptedPrivateKey, encryptionKey);

    // ===================== AES-256-CBC =====================
    private static string EncryptPrivateKeyInternal(string plaintext, string passphrase)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(passphrase, 32);
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = aes.IV;
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // concat IV + ciphertext
        var result = new byte[iv.Length + cipherBytes.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    private static string DecryptPrivateKeyInternal(string base64Cipher, string passphrase)
    {
        var full = Convert.FromBase64String(base64Cipher);
        var iv = new byte[16];
        var cipher = new byte[full.Length - 16];
        Buffer.BlockCopy(full, 0, iv, 0, 16);
        Buffer.BlockCopy(full, 16, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = DeriveKey(passphrase, 32);
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Sinh key AES-256 (32 bytes) từ passphrase bằng SHA-256.
    /// Đủ an toàn cho môi trường dev. Khi lên production nên dùng
    /// HKDF / PBKDF2 với salt được lưu riêng.
    /// </summary>
    private static byte[] DeriveKey(string passphrase, int bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(passphrase ?? string.Empty));
        return hash.Take(bytes).ToArray();
    }
}
