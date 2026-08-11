using DoAnV2.Application.Features.Admin.Blockchain.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Admin.Blockchain.Commands;

/// <summary>
/// TASK 11 - Mục 11.2: Admin gán role on-chain cho 1 địa chỉ ví (grantRole).
/// BR-42: Admin có thể bù đắp thủ công khi việc grant role tự động trong
/// quy trình duyệt user bị lỗi.
/// </summary>
public record GrantRoleCommand(
    string RoleName,        // "FARMER_ROLE" | "PROCESSOR_ROLE" | "RETAILER_ROLE"
    string AccountAddress)  // 0x...
    : IRequest<WhitelistRoleResultDto>;

/// <summary>
/// TASK 11 - Mục 11.2: Admin thu hồi role on-chain cho 1 địa chỉ ví (revokeRole).
/// </summary>
public record RevokeRoleCommand(
    string RoleName,
    string AccountAddress)
    : IRequest<WhitelistRoleResultDto>;
