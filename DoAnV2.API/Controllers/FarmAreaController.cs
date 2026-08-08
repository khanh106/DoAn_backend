using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.FarmAreas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/farm-areas")]
[Authorize(Policy = "RequireProcessor")]
public class FarmAreaController : ControllerBase
{
    private readonly IMediator _mediator;

    public FarmAreaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/processor/farm-areas - Tạo vùng trồng mới.</summary>
    [HttpPost]
    public async Task<ActionResult<FarmAreaDto>> Create([FromBody] CreateFarmAreaRequest body, CancellationToken ct)
    {
        var cmd = new CreateFarmAreaCommand(
            body.Name, body.OwnerName, body.Province, body.District, body.Ward,
            body.Area, body.SoilType, body.GPS, body.PlantingCode);
        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>GET /api/v1/processor/farm-areas - Danh sách vùng trồng (filter theo Tỉnh/Huyện/Xã, Mã số vùng).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FarmAreaDto>>> List(
        [FromQuery] string? province,
        [FromQuery] string? district,
        [FromQuery] string? ward,
        [FromQuery] string? plantingCode,
        CancellationToken ct)
    {
        var query = new GetFarmAreasQuery(province, district, ward, plantingCode);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/processor/farm-areas/{id} - Chi tiết vùng trồng.</summary>
    [HttpGet("{id:guid}", Name = "GetFarmAreaById")]
    public async Task<ActionResult<FarmAreaDto>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFarmAreaByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/processor/farm-areas/{id} - Chỉnh sửa vùng trồng.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FarmAreaDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateFarmAreaRequest body,
        CancellationToken ct)
    {
        var cmd = new UpdateFarmAreaCommand(id, body.Name, body.OwnerName,
            body.Province, body.District, body.Ward, body.Area,
            body.SoilType, body.GPS, body.PlantingCode);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }
}