using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/v1/users/profile - Lấy thông tin user đang đăng nhập (Authenticated).</summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ProfileResponse>> GetProfile(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyProfileQuery(), ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/users/pending - Admin lấy danh sách user PENDING.</summary>
    [HttpGet("pending")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<IReadOnlyList<PendingUserDto>>> GetPending(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPendingUsersQuery(), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/users/{id}/approve - Admin duyệt / từ chối user.</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<PendingUserDto>> Approve([FromRoute] Guid id, [FromBody] ApproveUserRequest body, CancellationToken ct)
    {
        var cmd = new ApproveUserCommand(id, body.Action);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/users/{id}/lock - Admin khoá / mở khoá user.</summary>
    [HttpPut("{id:guid}/lock")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<PendingUserDto>> Lock([FromRoute] Guid id, [FromBody] LockUserRequest body, CancellationToken ct)
    {
        var cmd = new LockUserCommand(id, body.Lock);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/users/{id}/sweep - Admin thu hồi ETH từ ví Custodial Wallet của Farmer (BR-46.2).</summary>
    [HttpPost("{id:guid}/sweep")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<SweepFarmerWalletResponse>> Sweep([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new SweepFarmerWalletCommand(id), ct);
        return Ok(result);
    }
}
