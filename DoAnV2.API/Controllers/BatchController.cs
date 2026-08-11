using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Batches.Batches.Queries;
using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/batches")]
[Authorize(Policy = "RequireProcessor")]
public class BatchController : ControllerBase
{
    private readonly IMediator _mediator;

    public BatchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/v1/processor/batches - Lấy danh sách Lô sản xuất / Kế hoạch của HTX.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BatchDto>>> GetBatches(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBatchesQuery(), ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/processor/batches - Tạo lô sản xuất + upload IPFS + gọi SC.</summary>
    [HttpPost]
    public async Task<ActionResult<BatchDto>> Create(
        [FromBody] CreateBatchRequest body, CancellationToken ct)
    {
        var cmd = new CreateBatchCommand(
            body.BatchCode, body.FruitTypeId, body.ProductId, body.FarmAreaId,
            body.PlantingDate, body.ExpectedQuantity,
            body.AssignedWorkerIds, body.RepresentativeWorkerId);
        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>GET /api/v1/processor/batches/{id} - Chi tiết batch.</summary>
    [HttpGet("{id:guid}", Name = "GetBatchById")]
    public async Task<ActionResult<BatchDto>> GetById(
        [FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBatchByIdQuery(id), ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
}
