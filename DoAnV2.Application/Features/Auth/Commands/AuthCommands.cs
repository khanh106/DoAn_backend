using DoAnV2.Application.Features.Auth.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Auth.Commands;

// ===== Auth =====
public record RegisterCommand(
    string FullName,
    string Email,
    string Phone,
    string Password,
    DoAnV2.Domain.Enums.RoleType RoleRequested,
    string? WalletAddress = null) : IRequest<AuthResponse>;


public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;


public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponse>;


// ===== Admin: Approve / Reject =====
public record ApproveUserCommand(Guid UserId, string Action) : IRequest<PendingUserDto>;


// ===== Admin: Lock / Unlock =====
public record LockUserCommand(Guid UserId, bool Lock) : IRequest<PendingUserDto>;


// ===== Admin: Sweep Farmer Wallet (thu hồi ETH) =====
public record SweepFarmerWalletCommand(Guid UserId) : IRequest<SweepFarmerWalletResponse>;


// ===== Authenticated: Get my profile =====
public record GetMyProfileQuery : IRequest<ProfileResponse>;


// ===== Admin: Get pending list =====
public record GetPendingUsersQuery : IRequest<IReadOnlyList<PendingUserDto>>;

// ===== Admin: Get all users =====
public record GetAllUsersQuery : IRequest<IReadOnlyList<UserAccountDto>>;
// ===== Admin: Get user detail =====
public record GetUserDetailQuery(Guid UserId) : IRequest<UserDetailDto>;
// ===== Admin: Change User Role =====
public record ChangeUserRoleCommand(Guid UserId, DoAnV2.Domain.Enums.RoleType NewRole) : IRequest<UserAccountDto>;
// ===== Authenticated: HTX Profile =====
public record GetCooperativeProfileQuery() : IRequest<CooperativeProfileDto?>;
public record UpdateCooperativeProfileCommand(CooperativeProfileDto Profile) : IRequest<bool>;
public record UpdateWalletAddressCommand(string WalletAddress, string? PrivateKey = null) : IRequest<bool>;
