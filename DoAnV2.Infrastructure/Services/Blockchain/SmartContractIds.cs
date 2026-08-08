using Nethereum.Util;

namespace DoAnV2.Infrastructure.Services.Blockchain;

/// <summary>
/// Helper chuyển đổi giữa C# string/Guid/SHA-256 ➔ Solidity bytes32.
///
/// FruitTraceability.sol nhận `bytes32` cho hầu hết ID (batchId, batchCode,
/// fruitType, dataHash, role). Ta dùng Keccak-256 (giống hàm SHA3 dùng
/// trong Ethereum) để ép mọi string độ dài tuỳ ý về đúng 32 bytes.
///
/// Riêng `dataHash` (SHA-256 hex 64 ký tự) ➔ convert trực tiếp từ hex.
/// </summary>
public static class SmartContractIds
{
    private static readonly Sha3Keccack Keccak = Sha3Keccack.Current;

    /// <summary>keccak256(text) ➔ bytes32 (32 bytes).</summary>
    public static byte[] ToBytes32(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new byte[32];
        var hashHex = Keccak.CalculateHash(text); // returns hex string "0x..."
        var hex = hashHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? hashHex[2..]
            : hashHex;
        // Lấy 64 hex chars đầu (= 32 bytes). Keccak256 luôn ra 32 bytes nên an toàn.
        return Convert.FromHexString(hex[..64]);
    }

    /// <summary>Convert hex string (0x... hoặc không) sang 32 bytes. Trả về null nếu không hợp lệ.</summary>
    public static byte[]? HexToBytes32(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        if (s.Length != 64) return null;
        try
        {
            return Convert.FromHexString(s);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Trả về 32 bytes đại diện cho 1 Guid (keccak256 của chuỗi Guid).</summary>
    public static byte[] GuidToBytes32(Guid id) => ToBytes32(id.ToString("D"));

    /// <summary>Trả về 32 bytes đại diện cho 1 string code (vd: "BATCH-2026-001").</summary>
    public static byte[] CodeToBytes32(string code) => ToBytes32(code);
}
