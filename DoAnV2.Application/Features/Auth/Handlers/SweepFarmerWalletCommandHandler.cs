using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.Auth.Handlers;

/// <summary>
/// Admin thu hồi ETH từ ví Custodial Wallet của Farmer (BR-46.2).
/// Điều kiện:
///   - User là FARMER.
///   - User đã được APPROVE.
///   - User có WalletAddress.
/// Hành vi: gọi IBlockchainService.SweepFarmerWalletAsync,
///   ghi BlockchainTransaction(FunctionName="sweepFarmerWallet") để truy vết.
/// </summary>
public class SweepFarmerWalletCommandHandler : IRequestHandler<SweepFarmerWalletCommand, SweepFarmerWalletResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IBlockchainService _blockchain;

    public SweepFarmerWalletCommandHandler(IUnitOfWork uow, IBlockchainService blockchain)
    {
        _uow = uow;
        _blockchain = blockchain;
    }

    public async Task<SweepFarmerWalletResponse> Handle(SweepFarmerWalletCommand req, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(req.UserId, ct)
            ?? throw new NotFoundException($"Không tìm thấy user {req.UserId}.");

        if (user.Role?.RoleName != RoleType.FARMER)
            throw new ValidationException("Chỉ thu hồi ETH từ ví Farmer.");

        if (string.IsNullOrWhiteSpace(user.WalletAddress))
            throw new ValidationException("User chưa có WalletAddress.");

        if (user.Status != UserStatus.APPROVED)
            throw new ValidationException(
                $"Chỉ thu hồi ETH từ user có Status = APPROVED. Hiện tại: {user.Status}.");

        var txHash = await _blockchain.SweepFarmerWalletAsync(
            user.WalletAddress!,
            user.EncryptedPrivateKey,
            ct);

        return new SweepFarmerWalletResponse(
            UserId: user.Id,
            WalletAddress: user.WalletAddress!,
            TransactionHash: txHash,
            Message: txHash is null
                ? "Sweep đã bị skip (số dư thấp hơn MinFarmerBalanceToKeep hoặc WalletFunding.Enabled=false). Không có tx hash."
                : $"Đã gửi sweep tx. TxHash: {txHash}.");
    }
}
