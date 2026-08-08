using DoAnV2.Application.Features.Batches.BatchWorkers.Commands;
using DoAnV2.Application.Features.Batches.BatchWorkers.Queries;
using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 05 - Mục 5.2: API Quản lý Nhân sự Lô (Processor).
/// </summary>
[ApiController]
[Route("api/v1/processor/batches/{id:guid}")]
[Authorize(Policy = "RequireProcessor")]
public class BatchWorkerController : ControllerBase
{
    private readonly IMediator _mediator;

    public BatchWorkerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/processor/batches/{id}/workers - Thêm công nhân vào lô.</summary>
    [HttpPost("workers")]
    public async Task<ActionResult<BatchDto>> AddWorker(
        [FromRoute] Guid id,
        [FromBody] AddWorkerToBatchRequest body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new AddWorkerToBatchCommand(id, body.UserId), ct);
        return Ok(result);
    }

    /// <summary>DELETE /api/v1/processor/batches/{id}/workers/{workerId} - Xóa công nhân khỏi lô.</summary>
    [HttpDelete("workers/{workerId:guid}")]
    public async Task<ActionResult<BatchDto>> RemoveWorker(
        [FromRoute] Guid id,
        [FromRoute] Guid workerId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RemoveWorkerFromBatchCommand(id, workerId), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/processor/batches/{id}/representative - Đổi người đại diện.</summary>
    [HttpPut("representative")]
    public async Task<ActionResult<BatchDto>> ChangeRepresentative(
        [FromRoute] Guid id,
        [FromBody] ChangeRepresentativeRequest body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ChangeRepresentativeCommand(id, body.NewRepresentativeWorkerId), ct);
        return Ok(result);
    }
}

/// <summary>
/// TASK 05 - Mục 5.2 + 5.3: API cho Farmer (xem lô được phân công, xác nhận nhận lô).
/// </summary>
[ApiController]
[Route("api/v1/farmer/batches")]
[Authorize(Policy = "RequireFarmer")]
public class FarmerBatchController : ControllerBase
{
    private readonly IMediator _mediator;

    public FarmerBatchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/v1/farmer/batches/assigned - Danh sách lô được phân công.</summary>
    [HttpGet("assigned")]
    public async Task<ActionResult<IReadOnlyList<AssignedBatchDto>>> GetAssigned(
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAssignedBatchesQuery(), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/farmer/batches/{id}/accept - Công nhân xác nhận nhận lô (gọi SC acceptBatch).</summary>
    [HttpPut("{id:guid}/accept")]
    public async Task<ActionResult<BatchWorkerAcceptedDto>> Accept(
        [FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new AcceptBatchCommand(id), ct);
        return Ok(result);
    }
}
