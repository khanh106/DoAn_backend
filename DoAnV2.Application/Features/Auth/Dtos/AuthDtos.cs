using DoAnV2.Domain.Enums;

namespace DoAnV2.Application.Features.Auth.Dtos;

// ============ Requests ============

public record RegisterRequest(
    string FullName,
    string Email,
    string Phone,
    string Password,
    RoleType RoleRequested,
    string? WalletAddress = null);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken);

public record ApproveUserRequest(Guid UserId, string Action /* APPROVE | REJECT */);

public record LockUserRequest(Guid UserId, bool Lock);
public record ChangeUserRoleRequest(RoleType NewRole);
public record UpdateWalletAddressRequest(string WalletAddress);

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
public record UserAccountDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string Role,
    string? WalletAddress,
    string Status,
    DateTime CreatedAt);

public record UserDetailDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string Role,
    string? WalletAddress,
    bool HasCustodialKey,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CooperativeProfileDto? CooperativeProfile);
// ============ Hồ sơ Hợp tác xã / Doanh nghiệp ============

public record CooperativeCertificateFileDto(
    string Id,
    string Name,
    string Url,
    string Type,
    string Size);



public record CooperativeProfileDto(
    string UnitName,
    string EntityType,
    string RepresentativeName,
    string MainProducts,
    string Phone,
    string Email,
    string Address,
    string Website,
    string BusinessRegistrationNo,
    string BusinessSymbol,
    string Certificates,
    string PlantingAreaCode,
    string TotalRevenue,
    string MainMarket,
    string TotalEmployees,
    string EstablishedYear,
    List<CooperativeCertificateFileDto> CertificateFiles);
