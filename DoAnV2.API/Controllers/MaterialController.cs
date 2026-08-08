using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.Materials;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/materials")]
[Authorize(Policy = "RequireProcessor")]
public class MaterialController : ControllerBase
{
    private readonly IMediator _mediator;

    public MaterialController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/processor/materials - Khai báo vật tư mới.</summary>
    [HttpPost]
    public async Task<ActionResult<MaterialItemDto>> Create([FromBody] CreateMaterialRequest body, CancellationToken ct)
    {
        var cmd = new CreateMaterialCommand(
            body.ItemType, body.Code, body.Name, body.Unit, body.Price,
            body.DosagePerHa, body.Concentration, body.Supplier, body.NPKRatio, body.Note);
        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    /// <summary>GET /api/v1/processor/materials - Danh sách vật tư.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialItemDto>>> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMaterialsQuery(), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/processor/materials/{id} - Cập nhật thông tin/đơn giá/nồng độ NPK.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MaterialItemDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateMaterialRequest body,
        CancellationToken ct)
    {
        var cmd = new UpdateMaterialCommand(id, body.Name, body.Unit, body.Price,
            body.DosagePerHa, body.Concentration, body.Supplier, body.NPKRatio, body.Note);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }
}