using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Auth.Handlers;

/// <summary>
/// Duyệt / Từ chối user (chỉ Admin).
/// Nếu APPROVE + có WalletAddress ➔
///   1. Gọi IRoleOnChainAssigner.GrantRoleAsync (granted role on-chain).
///   2. Gọi IBlockchainService.FundFarmerWalletAsync (cấp 0.003 ETH gas fee).
/// </summary>
public class ApproveUserCommandHandler : IRequestHandler<ApproveUserCommand, PendingUserDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRoleOnChainAssigner _onChain;
    private readonly IBlockchainService _blockchain;
    private readonly WalletFundingOptions _walletFundingOptions;
    private readonly ILogger<ApproveUserCommandHandler> _logger;

    public ApproveUserCommandHandler(
        IUnitOfWork uow,
        IRoleOnChainAssigner onChain,
        IBlockchainService blockchain,
        IOptions<WalletFundingOptions> walletFundingOptions,
        ILogger<ApproveUserCommandHandler> logger)
    {
        _uow = uow;
        _onChain = onChain;
        _blockchain = blockchain;
        _walletFundingOptions = walletFundingOptions.Value;
        _logger = logger;
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

            // Nếu User có WalletAddress (HTX đăng ký ví MetaMask hoặc Nông dân) ➔ Thực hiện cấp Role trên Blockchain
            if (!string.IsNullOrWhiteSpace(user.WalletAddress))
            {
                // 1. Cấp role tương ứng trên Smart Contract (FARMER_ROLE, PROCESSOR_ROLE, RETAILER_ROLE)
                var roleNameOnChain = user.Role?.RoleName.ToString() ?? "FARMER";
                await _onChain.GrantRoleAsync(
                    roleName: roleNameOnChain,
                    walletAddress: user.WalletAddress!,
                    ct: ct);

                // 2. Nếu là FARMER hoặc RETAILER ➔ Cấp thêm ETH gas fee ban đầu cho ví Custodial
                if (user.Role?.RoleName == RoleType.FARMER || user.Role?.RoleName == RoleType.RETAILER)
                {
                    try
                    {
                        await _blockchain.FundFarmerWalletAsync(
                            farmerWalletAddress: user.WalletAddress!,
                            amountEth: _walletFundingOptions.FundAmountEth,
                            ct: ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Cấp ETH gas fee thất bại cho ví {Wallet}. Lý do: {Message}", user.WalletAddress, ex.Message);
                    }
                }
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
