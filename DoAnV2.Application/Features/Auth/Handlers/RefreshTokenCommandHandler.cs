using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using DoAnV2.Application.Common.Options;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DoAnV2.Application.Features.Auth.Handlers;

/// <summary>
/// Làm mới AccessToken bằng RefreshToken.
/// Mặc định: chỉ cần access token còn signature hợp lệ + RefreshToken đúng format.
/// (Ở production nên lưu refresh token xuống DB để revoke khi cần.)
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwt;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenCommandHandler(IUnitOfWork uow, IJwtTokenService jwt, IOptions<JwtOptions> jwtOptions)
    {
        _uow = uow;
        _jwt = jwt;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            throw new ValidationException("Refresh token không được trống.");

        var principal = ValidateExpiredToken(req.AccessToken);
        if (principal is null)
            throw new UnauthorizedException("Access token không hợp lệ.");

        var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("UserId")?.Value
                        ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw new UnauthorizedException("UserId trong token không hợp lệ.");

        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("Người dùng không tồn tại.");

        if (user.Status != Domain.Enums.UserStatus.APPROVED)
            throw new ForbiddenException("Tài khoản không ở trạng thái hoạt động.");

        var roleName = user.Role?.RoleName.ToString() ?? string.Empty;
        var newAccess = _jwt.GenerateAccessToken(user.Id, user.Email, roleName, user.WalletAddress);
        var newRefresh = _jwt.GenerateRefreshToken();

        return new AuthResponse(
            User: new AuthenticatedUserDto(
                user.Id, user.FullName, user.Email, user.Phone,
                roleName, user.WalletAddress, user.Status.ToString()),
            AccessToken: newAccess,
            RefreshToken: newRefresh,
            AccessTokenExpiry: _jwt.GetAccessTokenExpiry(),
            RefreshTokenExpiry: _jwt.GetRefreshTokenExpiry());
    }

    private ClaimsPrincipal? ValidateExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            ValidateLifetime = false, // cho phép token đã hết hạn để refresh
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out _);
        }
        catch
        {
            return null;
        }
    }
}
