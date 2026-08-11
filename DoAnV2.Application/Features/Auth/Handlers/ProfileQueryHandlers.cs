using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DoAnV2.Application.Features.Auth.Handlers;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, ProfileResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetMyProfileQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ProfileResponse> Handle(GetMyProfileQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var user = await _uow.Users.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");

        return new ProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role?.RoleName.ToString() ?? string.Empty,
            user.WalletAddress,
            user.Status.ToString(),
            user.CreatedAt,
            user.UpdatedAt);
    }
}

public class GetPendingUsersQueryHandler : IRequestHandler<GetPendingUsersQuery, IReadOnlyList<PendingUserDto>>
{
    private readonly IUnitOfWork _uow;

    public GetPendingUsersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<PendingUserDto>> Handle(GetPendingUsersQuery req, CancellationToken ct)
    {
        var users = await _uow.Users.GetPendingUsersAsync(ct);
        return users.Select(u => new PendingUserDto(
            u.Id, u.FullName, u.Email, u.Phone,
            u.Role?.RoleName.ToString() ?? string.Empty,
            u.Status.ToString(), u.CreatedAt)).ToList();
    }
}

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserAccountDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAllUsersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<UserAccountDto>> Handle(GetAllUsersQuery req, CancellationToken ct)
    {
        var users = await _uow.Users.GetAllUsersAsync(ct);
        return users.Select(u => new UserAccountDto(
            u.Id,
            u.FullName,
            u.Email,
            u.Phone,
            u.Role?.RoleName.ToString() ?? string.Empty,
            u.WalletAddress,
            u.Status.ToString(),
            u.CreatedAt)).ToList();
    }
}

public class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, UserDetailDto>
{
    private readonly IUnitOfWork _uow;

    public GetUserDetailQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UserDetailDto> Handle(GetUserDetailQuery req, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(req.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");

        CooperativeProfileDto? coopProfile = null;
        if (!string.IsNullOrWhiteSpace(user.CooperativeProfileInfo))
        {
            try
            {
                coopProfile = JsonSerializer.Deserialize<CooperativeProfileDto>(user.CooperativeProfileInfo);
            }
            catch { }
        }

        return new UserDetailDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role?.RoleName.ToString() ?? string.Empty,
            user.WalletAddress,
            !string.IsNullOrWhiteSpace(user.EncryptedPrivateKey),
            user.Status.ToString(),
            user.CreatedAt,
            user.UpdatedAt,
            coopProfile);
    }
}

// =========================================================================
// ===== Handlers đọc & cập nhật Hồ sơ Hợp tác xã / Doanh nghiệp =====
// =========================================================================

public class GetCooperativeProfileQueryHandler : IRequestHandler<GetCooperativeProfileQuery, CooperativeProfileDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetCooperativeProfileQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<CooperativeProfileDto?> Handle(GetCooperativeProfileQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var user = await _uow.Users.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");

        if (string.IsNullOrWhiteSpace(user.CooperativeProfileInfo))
            return null;

        try
        {
            return JsonSerializer.Deserialize<CooperativeProfileDto>(user.CooperativeProfileInfo);
        }
        catch
        {
            return null;
        }
    }
}

public class UpdateCooperativeProfileCommandHandler : IRequestHandler<UpdateCooperativeProfileCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public UpdateCooperativeProfileCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(UpdateCooperativeProfileCommand req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var user = await _uow.Users.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");

        // Serialize DTO thành định dạng JSON lưu vào cột CooperativeProfileInfo của User
        user.CooperativeProfileInfo = JsonSerializer.Serialize(req.Profile);

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public class UpdateWalletAddressCommandHandler : IRequestHandler<UpdateWalletAddressCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;

    public UpdateWalletAddressCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions)
    {
        _uow = uow;
        _currentUser = currentUser;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
    }

    public async Task<bool> Handle(UpdateWalletAddressCommand req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var user = await _uow.Users.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");

        user.WalletAddress = req.WalletAddress;
     if (!string.IsNullOrWhiteSpace(req.PrivateKey))
{
    var cleanKey = req.PrivateKey.Trim();
    if (cleanKey.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    {
        cleanKey = cleanKey[2..];
    }

    if (cleanKey.Length != 64 || !cleanKey.All(c => "0123456789abcdefABCDEF".Contains(c)))
    {
        throw new ValidationException("Khóa Private Key không hợp lệ! Private Key Ethereum phải có độ dài đúng 64 ký tự Hex (32 bytes).");
    }

    user.EncryptedPrivateKey = _walletService.EncryptPrivateKey("0x" + cleanKey, _walletOptions.EncryptionKey);
}
user.UpdatedAt = DateTime.UtcNow;


        await _uow.SaveChangesAsync(ct);
        return true;
    }
}


