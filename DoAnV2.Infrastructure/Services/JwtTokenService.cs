using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// Triển khai IJwtTokenService. Claims gồm:
/// - UserId (Guid)
/// - Email
/// - Role
/// - WalletAddress (nếu có)
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private DateTime _now => DateTime.UtcNow;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(
        Guid userId,
        string email,
        string role,
        string? walletAddress,
        IEnumerable<string>? extraClaims = null)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.Key);
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("UserId", userId.ToString()),
            new("Role", role),
        };

        if (!string.IsNullOrWhiteSpace(walletAddress))
        {
            claims.Add(new Claim("WalletAddress", walletAddress));
        }

        if (extraClaims != null)
        {
            foreach (var c in extraClaims)
            {
                claims.Add(new Claim("Extra", c));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _now,
            expires: GetAccessTokenExpiry(),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public DateTime GetAccessTokenExpiry()
        => _now.AddMinutes(_options.AccessTokenExpiryMinutes);

    public DateTime GetRefreshTokenExpiry()
        => _now.AddDays(_options.RefreshTokenExpiryDays);
}
