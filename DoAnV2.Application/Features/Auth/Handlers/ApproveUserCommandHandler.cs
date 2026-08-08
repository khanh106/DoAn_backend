using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Auth.Handlers;

/// <summary>
/// Duyệt / Từ chối user (chỉ Admin).
/// Nếu APPROVE + có WalletAddress ➔
///   1. Gọi IRoleOnChainAssigner.GrantRoleAsync (granted role on-chain).
///   2. Gọi IBlockchainService.FundFarmerWalletAsync (cấp 0.003 ETH gas fee).
/// BR-46.2: Ví Farmer được Admin tài trợ gas fee.
/// </summary>
public class ApproveUserCommandHandler : IRequestHandler<ApproveUserCommand, PendingUserDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRoleOnChainAssigner _onChain;
    private readonly IBlockchainService _blockchain;
    private readonly WalletFundingOptions _walletFundingOptions;

    public ApproveUserCommandHandler(
        IUnitOfWork uow,
        IRoleOnChainAssigner onChain,
        IBlockchainService blockchain,
        IOptions<WalletFundingOptions> walletFundingOptions)
    {
        _uow = uow;
        _onChain = onChain;
        _blockchain = blockchain;
        _walletFundingOptions = walletFundingOptions.Value;
    }

    public async Task<PendingUserDto> Handle(ApproveUserCommand req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Action))
            throw new ValidationException("Action không được trống.");

        var action = req.Action.Trim().ToUpperInvariant();
        if (action != "APPROVE" && action != "REJECT")
            throw new ValidationException("Action chỉ chấp nhận APPROVE hoặc REJECT.");

        var user = await _uow.Users.GetByIdAsync(req.UserId, ct)
            ?? throw new NotFoundException($"Không tìm thấy user {req.UserId}.");

        if (action == "APPROVE")
        {
            user.Status = UserStatus.APPROVED;

            // FARMER có WalletAddress ➔ (1) grant role on-chain + (2) cấp gas fee.
            if (!string.IsNullOrWhiteSpace(user.WalletAddress)
                && user.Role?.RoleName == RoleType.FARMER)
            {
                // 1. Grant role on-chain (TASK 03 sẽ thay bằng Nethereum)
                await _onChain.GrantRoleAsync(
                    roleName: "FARMER_ROLE",
                    walletAddress: user.WalletAddress!,
                    ct: ct);

                // 2. Cấp ETH gas fee (BR-46.2 / yêu cầu user)
                await _blockchain.FundFarmerWalletAsync(
                    farmerWalletAddress: user.WalletAddress!,
                    amountEth: _walletFundingOptions.FundAmountEth,
                    ct: ct);
            }
        }
        else
        {
            user.Status = UserStatus.REJECTED;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        return new PendingUserDto(
            user.Id, user.FullName, user.Email, user.Phone,
            user.Role?.RoleName.ToString() ?? string.Empty,
            user.Status.ToString(), user.CreatedAt);
    }
}
