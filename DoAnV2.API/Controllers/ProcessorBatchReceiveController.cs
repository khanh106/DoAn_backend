using DoAnV2.Application.Features.Harvests.Commands;
using DoAnV2.Application.Features.Harvests.Dtos;
using DoAnV2.Application.Features.Harvests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 06 - Mục 6.3: API Processor tiếp nhận lô sau thu hoạch.
/// Gọi Smart Contract receiveBatch ➔ Batch.CurrentStage = STAGE_RECEIVED.
/// BR-11: Chỉ tiếp nhận lô đang ở STAGE_HARVESTED.
/// </summary>
[ApiController]
[Route("api/v1/processor/batches/{batchId:guid}")]
[Authorize(Policy = "RequireProcessor")]
public class ProcessorBatchReceiveController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProcessorBatchReceiveController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/v1/processor/batches/{batchId}/receive
    /// Processor tiếp nhận lô đã thu hoạch xong.
    /// </summary>
    [HttpPost("receive")]
    public async Task<ActionResult<ReceiveBatchResponseDto>> Receive(
        [FromRoute] Guid batchId,
        [FromBody] ReceiveBatchRequest body,
        CancellationToken ct)
    {
        var cmd = new ReceiveBatchCommand(
            BatchId: batchId,
            ReceivedDate: body.ReceivedDate,
            Quantity: body.Quantity,
            Unit: body.Unit,
            DeliveryPerson: body.DeliveryPerson,
            ConditionNote: body.ConditionNote);

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/batches/{batchId}/harvests
    /// Xem lịch sử thu hoạch / tiếp nhận của lô.
    /// </summary>
    [HttpGet("harvests")]
    public async Task<ActionResult<IReadOnlyList<HarvestHistoryDto>>> GetHarvests(
        [FromRoute] Guid batchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHarvestsByBatchQuery(batchId), ct);
        return Ok(result);
    }
}

/// <summary>Body cho POST /receive.</summary>
public record ReceiveBatchRequest(
    DateTime ReceivedDate,
    double Quantity,
    string Unit,
    string DeliveryPerson,
    string ConditionNote);