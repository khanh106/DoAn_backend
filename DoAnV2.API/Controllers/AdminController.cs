using DoAnV2.Application.Features.Admin.SystemLogs.Dtos;
using DoAnV2.Application.Features.Admin.SystemLogs.Queries;
using DoAnV2.Application.Features.Admin.Blockchain.Commands;
using DoAnV2.Application.Features.Admin.Blockchain.Dtos;
using DoAnV2.Application.Features.Admin.Blockchain.Queries;
using DoAnV2.Application.Features.Admin.Dashboard.Dtos;
using DoAnV2.Application.Features.Admin.Dashboard.Queries;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 11 - Mục 11.1 + 11.2:
///   - GET    /api/v1/admin/dashboard/stats                            - Dashboard tổng quan.
///   - GET    /api/v1/admin/blockchain/transactions                   - Danh sách giao dịch (filter).
///   - POST   /api/v1/admin/blockchain/transactions/{id}/retry       - Retry giao dịch FAILED (BR-42).
///   - POST   /api/v1/admin/blockchain/whitelist/grant-role           - Cấp role on-chain.
///   - POST   /api/v1/admin/blockchain/whitelist/revoke-role          - Thu hồi role on-chain.
///
/// Tất cả endpoints yêu cầu Role = ADMIN.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "RequireAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ============================================================
    // 11.1 - Dashboard
    // ============================================================

    /// <summary>GET /api/v1/admin/dashboard/stats - Thống kê tổng quan cho Admin.</summary>
    [HttpGet("dashboard/stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery(), ct);
        return Ok(result);
    }

    // ============================================================
    // 11.2 - Blockchain Monitoring + Retry
    // ============================================================

    /// <summary>
    /// GET /api/v1/admin/blockchain/transactions?status=FAILED&amp;functionName=shipParent&amp;batchId=...
    /// Lấy danh sách giao dịch Blockchain có filter (tất cả optional).
    /// </summary>
    [HttpGet("blockchain/transactions")]
    public async Task<ActionResult<IReadOnlyList<BlockchainTransactionDto>>> GetTransactions(
        [FromQuery] TransactionStatus? status,
        [FromQuery] string? functionName,
        [FromQuery] Guid? batchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetBlockchainTransactionsQuery(status, functionName, batchId), ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/admin/blockchain/transactions/{id}/retry
    /// Phát lệnh Retry cho 1 giao dịch FAILED (BR-42).
    /// </summary>
    [HttpPost("blockchain/transactions/{id:guid}/retry")]
    public async Task<ActionResult<RetryTransactionResultDto>> RetryTransaction(
        [FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new RetryBlockchainTransactionCommand(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/admin/blockchain/whitelist/grant-role
    /// Body: { "roleName": "FARMER_ROLE", "accountAddress": "0x..." }
    /// </summary>
    [HttpPost("blockchain/whitelist/grant-role")]
    public async Task<ActionResult<WhitelistRoleResultDto>> GrantRole(
        [FromBody] WhitelistRoleRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GrantRoleCommand(body.RoleName, body.AccountAddress), ct);
        return Ok(result);
    }
    [HttpGet("system-logs")]
    public async Task<ActionResult<SystemLogsPagedResponseDto>> GetSystemLogs(
        [FromQuery] string? category,
        [FromQuery] string? severity,
        [FromQuery] string? search,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetSystemLogsQuery(category, severity, search, startDate, endDate, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/admin/blockchain/whitelist/revoke-role
    /// Body: { "roleName": "FARMER_ROLE", "accountAddress": "0x..." }
    /// </summary>
    [HttpPost("blockchain/whitelist/revoke-role")]
    public async Task<ActionResult<WhitelistRoleResultDto>> RevokeRole(
        [FromBody] WhitelistRoleRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RevokeRoleCommand(body.RoleName, body.AccountAddress), ct);
        return Ok(result);
    }
}

/// <summary>Request body cho grant/revoke role.</summary>
public record WhitelistRoleRequest(
    string RoleName,
    string AccountAddress);
