using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using DoAnV2.Application.Common.Options;
using MediatR;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Auth.Handlers;

/// <summary>
/// Xử lý luồng đăng ký:
/// 1. Validate email không trùng.
/// 2. Hash mật khẩu bằng BCrypt.
/// 3. Nếu Role = FARMER ➔ tự sinh Custodial Wallet (AES-256).
/// 4. Tạo User với Status = PENDING.
/// 5. Trả AuthResponse (chưa trả token vì Status = PENDING, sẽ trả response rỗng).
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IWalletService _wallet;
    private readonly IJwtTokenService _jwt;
    private readonly WalletOptions _walletOptions;

    public RegisterCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IWalletService wallet,
        IJwtTokenService jwt,
        IOptions<WalletOptions> walletOptions)
    {
        _uow = uow;
        _hasher = hasher;
        _wallet = wallet;
        _jwt = jwt;
        _walletOptions = walletOptions.Value;
    }

    public async Task<AuthResponse> Handle(RegisterCommand req, CancellationToken ct)
    {
        if (req.RoleRequested == RoleType.ADMIN)
            throw new ForbiddenException("Không thể tự đăng ký tài khoản Admin.");

        if (await _uow.Users.EmailExistsAsync(req.Email, ct))
            throw new ConflictException($"Email '{req.Email}' đã được sử dụng.");

        var role = new Role
        {
            Id = (int)req.RoleRequested,
            RoleName = req.RoleRequested,
        };

        var user = new User
        {
            FullName = req.FullName,
            Email = req.Email,
            Phone = req.Phone,
            PasswordHash = _hasher.Hash(req.Password),
            RoleId = (int)req.RoleRequested,
            Role = role,
            Status = UserStatus.PENDING,
        };

        // Nếu là FARMER ➔ sinh Custodial Wallet (Ethereum).
        if (req.RoleRequested == RoleType.FARMER && _walletOptions.CustodialMode)
        {
            var (address, encryptedKey) = _wallet.GenerateEthereumWallet(_walletOptions.EncryptionKey);
            user.WalletAddress = address;
            user.EncryptedPrivateKey = encryptedKey;
        }

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        // Sau khi đăng ký Status = PENDING ➔ KHÔNG trả token,
        // FE sẽ hiển thị "Chờ Admin duyệt".
        return new AuthResponse(
            User: new AuthenticatedUserDto(
                user.Id, user.FullName, user.Email, user.Phone,
                req.RoleRequested.ToString(), user.WalletAddress, user.Status.ToString()),
            AccessToken: string.Empty,
            RefreshToken: string.Empty,
            AccessTokenExpiry: DateTime.MinValue,
            RefreshTokenExpiry: DateTime.MinValue);
    }
}
