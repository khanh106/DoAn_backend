using DoAnV2.Application.Features.ProcessorWorkers.Commands;
using DoAnV2.Application.Features.ProcessorWorkers.Dtos;
using DoAnV2.Application.Features.ProcessorWorkers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/workers")]
[Authorize(Policy = "RequireProcessor")]
public class ProcessorWorkerController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProcessorWorkerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/v1/processor/workers/search?keyword=... - Tìm kiếm công nhân & xem trạng thái liên kết.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<SearchWorkerResultDto>>> Search([FromQuery] string? keyword, CancellationToken ct)
    {
        var result = await _mediator.Send(new SearchWorkersQuery(keyword), ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/processor/workers/invite - Gửi lời mời liên kết tới công nhân.</summary>
    [HttpPost("invite")]
    public async Task<ActionResult<ProcessorWorkerLinkDto>> Invite([FromBody] SendInvitationRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new SendWorkerInvitationCommand(body.WorkerId), ct);
        return Ok(result);
    }
}
