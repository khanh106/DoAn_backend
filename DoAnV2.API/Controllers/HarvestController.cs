using DoAnV2.Application.Features.Harvests.Commands;
using DoAnV2.Application.Features.Harvests.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 06 - Mục 6.2: API xác nhận thu hoạch cho Farmer / Representative Worker.
/// Gọi Smart Contract harvestBatch ➔ Batch.CurrentStage = STAGE_HARVESTED.
/// BR-09, BR-10: Lô nhiều Worker ➔ chỉ Representative được ký.
/// </summary>
[ApiController]
[Route("api/v1/farmer/batches/{batchId:guid}/harvest")]
[Authorize(Policy = "RequireFarmer")]
public class HarvestController : ControllerBase
{
    private readonly IMediator _mediator;

    public HarvestController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/v1/farmer/batches/{batchId}/harvest
    /// Body JSON: harvestDate, quantity, unit, initialQuality, notes.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HarvestBatchResponseDto>> Harvest(
        [FromRoute] Guid batchId,
        [FromBody] HarvestBatchRequest body,
        CancellationToken ct)
    {
        var cmd = new HarvestBatchCommand(
            BatchId: batchId,
            HarvestDate: body.HarvestDate,
            Quantity: body.Quantity,
            Unit: body.Unit,
            InitialQuality: body.InitialQuality,
            Notes: body.Notes);

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }
}

/// <summary>Body cho POST /harvest.</summary>
public record HarvestBatchRequest(
    DateTime HarvestDate,
    double Quantity,
    string Unit,
    string InitialQuality,
    string? Notes);