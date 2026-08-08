namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Cấu hình cần thiết để sinh JWT, được truyền từ JwtOptions trong Infrastructure.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Sinh Access Token cho người dùng.
    /// Claims: UserId, Email, Role, WalletAddress.
    /// </summary>
    string GenerateAccessToken(
        Guid userId,
        string email,
        string role,
        string? walletAddress,
        IEnumerable<string>? extraClaims = null);

    /// <summary>
    /// Sinh Refresh Token ngẫu nhiên (random URL-safe base64).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>Lấy thời điểm hết hạn của token sinh ra (UTC).</summary>
    DateTime GetAccessTokenExpiry();
    DateTime GetRefreshTokenExpiry();
}