using DoAnV2.Application.Features.Auth.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Auth.Commands;

// ===== Auth =====
public record RegisterCommand(
    string FullName,
    string Email,
    string Phone,
    string Password,
    DoAnV2.Domain.Enums.RoleType RoleRequested) : IRequest<AuthResponse>;


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
