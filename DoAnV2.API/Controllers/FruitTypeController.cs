using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.FruitTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/fruit-types")]
[Authorize(Policy = "RequireProcessor")]
public class FruitTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public FruitTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/processor/fruit-types - Thêm loại hoa quả.</summary>
    [HttpPost]
    public async Task<ActionResult<FruitTypeDto>> Create([FromBody] CreateFruitTypeRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateFruitTypeCommand(body.Name, body.Code, body.Description), ct);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    /// <summary>GET /api/v1/processor/fruit-types - Danh sách loại hoa quả theo Processor.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FruitTypeDto>>> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFruitTypesQuery(), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/processor/fruit-types/{id} - Cập nhật / đổi trạng thái.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FruitTypeDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateFruitTypeRequest body,
        CancellationToken ct)
    {
        var cmd = new UpdateFruitTypeCommand(id, body.Name, body.Code, body.Description, body.Status);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }
}