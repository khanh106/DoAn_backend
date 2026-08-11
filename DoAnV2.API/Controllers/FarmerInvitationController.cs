using DoAnV2.Application.Features.ProcessorWorkers.Commands;
using DoAnV2.Application.Features.ProcessorWorkers.Dtos;
using DoAnV2.Application.Features.ProcessorWorkers.Queries;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/farmer/invitations")]
[Authorize(Policy = "RequireFarmer")]
public class FarmerInvitationController : ControllerBase
{
    private readonly IMediator _mediator;

    public FarmerInvitationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/v1/farmer/invitations - Lấy danh sách lời mời liên kết từ HTX gửi cho Farmer.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProcessorWorkerLinkDto>>> GetInvitations([FromQuery] CoopWorkerLinkStatus? status, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFarmerInvitationsQuery(status), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/farmer/invitations/{id}/respond - Chấp nhận / Từ chối lời mời.</summary>
    [HttpPut("{id:guid}/respond")]
    public async Task<ActionResult<bool>> Respond([FromRoute] Guid id, [FromBody] RespondInvitationRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new RespondWorkerInvitationCommand(id, body.Action), ct);
        return Ok(result);
    }
}
