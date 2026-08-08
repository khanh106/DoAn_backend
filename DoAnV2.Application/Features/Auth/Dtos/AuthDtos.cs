using DoAnV2.Domain.Enums;

namespace DoAnV2.Application.Features.Auth.Dtos;

// ============ Requests ============

public record RegisterRequest(
    string FullName,
    string Email,
    string Phone,
    string Password,
    RoleType RoleRequested);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken);

public record ApproveUserRequest(Guid UserId, string Action /* APPROVE | REJECT */);

public record LockUserRequest(Guid UserId, bool Lock);

// ============ Responses ============

public record AuthenticatedUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string Role,
    string? WalletAddress,
    string Status);

public record AuthResponse(
    AuthenticatedUserDto User,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry);

public record ProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string Role,
    string? WalletAddress,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record PendingUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string Role,
    string Status,
    DateTime CreatedAt);

public record SweepFarmerWalletResponse(
    Guid UserId,
    string WalletAddress,
    string? TransactionHash,
    string Message);
