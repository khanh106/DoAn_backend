using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.Auth.Handlers;

/// <summary>
/// Xử lý đăng nhập:
/// 1. Tìm user theo email.
/// 2. Nếu Status != APPROVED ➔ 403 (Account is pending approval or locked).
/// 3. Verify password BCrypt.
/// 4. Sinh AccessToken + RefreshToken.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public LoginCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponse> Handle(LoginCommand req, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailAsync(req.Email, ct)
            ?? throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        if (user.Status == UserStatus.PENDING)
            throw new ForbiddenException("Tài khoản đang chờ Admin duyệt.");

        if (user.Status == UserStatus.REJECTED)
            throw new ForbiddenException("Tài khoản đã bị từ chối.");

        if (user.Status == UserStatus.LOCKED)
            throw new ForbiddenException("Tài khoản đang bị khóa.");

        if (user.Status != UserStatus.APPROVED)
            throw new ForbiddenException("Account is pending approval or locked.");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        var roleName = user.Role?.RoleName.ToString() ?? string.Empty;
        var access = _jwt.GenerateAccessToken(
            user.Id, user.Email, roleName, user.WalletAddress);
        var refresh = _jwt.GenerateRefreshToken();

        return new AuthResponse(
            User: new AuthenticatedUserDto(
                user.Id, user.FullName, user.Email, user.Phone,
                roleName, user.WalletAddress, user.Status.ToString()),
            AccessToken: access,
            RefreshToken: refresh,
            AccessTokenExpiry: _jwt.GetAccessTokenExpiry(),
            RefreshTokenExpiry: _jwt.GetRefreshTokenExpiry());
    }
}
