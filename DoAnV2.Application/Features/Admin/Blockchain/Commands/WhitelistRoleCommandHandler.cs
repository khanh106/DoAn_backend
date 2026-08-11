using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Admin.Blockchain.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Admin.Blockchain.Commands;

public class WhitelistRoleCommandHandler
    : IRequestHandler<GrantRoleCommand, WhitelistRoleResultDto>,
      IRequestHandler<RevokeRoleCommand, WhitelistRoleResultDto>
{
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<WhitelistRoleCommandHandler> _logger;

    public WhitelistRoleCommandHandler(
        IBlockchainService blockchain,
        ILogger<WhitelistRoleCommandHandler> logger)
    {
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<WhitelistRoleResultDto> Handle(GrantRoleCommand req, CancellationToken ct)
    {
        Validate(req.RoleName, req.AccountAddress);

        try
        {
            var txHash = await _blockchain.GrantRoleAsync(
                roleName: req.RoleName,
                accountAddress: req.AccountAddress,
                ct: ct);

            _logger.LogInformation(
                "GrantRole on-chain OK: role={Role}, account={Account}, tx={TxHash}",
                req.RoleName, req.AccountAddress, txHash);

            return new WhitelistRoleResultDto(
                RoleName: req.RoleName,
                AccountAddress: req.AccountAddress,
                Action: "GRANT",
                TransactionHash: txHash,
                ExecutedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GrantRole thất bại cho ví {Account} với role {Role}", req.AccountAddress, req.RoleName);
            throw new DomainException($"Không thể cấp role on-chain ({req.RoleName}): {ex.Message}", 400);
        }
    }

    public async Task<WhitelistRoleResultDto> Handle(RevokeRoleCommand req, CancellationToken ct)
    {
        Validate(req.RoleName, req.AccountAddress);

        try
        {
            var txHash = await _blockchain.RevokeRoleAsync(
                roleName: req.RoleName,
                accountAddress: req.AccountAddress,
                ct: ct);

            _logger.LogInformation(
                "RevokeRole on-chain OK: role={Role}, account={Account}, tx={TxHash}",
                req.RoleName, req.AccountAddress, txHash);

            return new WhitelistRoleResultDto(
                RoleName: req.RoleName,
                AccountAddress: req.AccountAddress,
                Action: "REVOKE",
                TransactionHash: txHash,
                ExecutedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RevokeRole thất bại cho ví {Account} với role {Role}", req.AccountAddress, req.RoleName);
            throw new DomainException($"Không thể thu hồi role on-chain ({req.RoleName}): {ex.Message}", 400);
        }
    }

    private static void Validate(string roleName, string accountAddress)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ValidationException("RoleName không được trống.");
        if (string.IsNullOrWhiteSpace(accountAddress))
            throw new ValidationException("AccountAddress không được trống.");
        if (!accountAddress.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            || accountAddress.Length != 42)
            throw new ValidationException("AccountAddress phải là địa chỉ EVM hợp lệ (0x + 40 hex chars).");
    }
}
