using DoAnV2.Application.Features.MasterData.Distributors;
using DoAnV2.Application.Features.MasterData.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/distributors")]
[Authorize(Policy = "RequireProcessor")]
public class DistributorController : ControllerBase
{
    private readonly IMediator _mediator;

    public DistributorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/processor/distributors - Thêm Nhà phân phối đối tác mới.</summary>
    [HttpPost]
    public async Task<ActionResult<DistributorDto>> Create([FromBody] CreateDistributorRequest body, CancellationToken ct)
    {
        var cmd = new CreateDistributorCommand(
            body.Code, body.Name, body.Phone, body.Email, body.Address, body.TaxCode);
        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    /// <summary>GET /api/v1/processor/distributors - Danh sách Nhà phân phối của HTX.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DistributorDto>>> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDistributorsQuery(), ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/processor/distributors/search-retailers - Tìm kiếm Siêu thị/Retailer hệ thống.</summary>
    [HttpGet("search-retailers")]
    public async Task<ActionResult<IReadOnlyList<SearchRetailerResultDto>>> SearchRetailers([FromQuery] string? keyword, CancellationToken ct)
    {
        var result = await _mediator.Send(new SearchRetailersQuery(keyword), ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/processor/distributors/link/{retailerId} - Liên kết Siêu thị vào danh sách nhà phân phối.</summary>
    [HttpPost("link/{retailerId:guid}")]
    public async Task<ActionResult<DistributorDto>> LinkRetailer([FromRoute] Guid retailerId, CancellationToken ct)
    {
        var result = await _mediator.Send(new LinkRetailerCommand(retailerId), ct);
        return Ok(result);
    }

    /// <summary>DELETE /api/v1/processor/distributors/{id} - Xóa nhà phân phối.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDistributorCommand(id), ct);
        return NoContent();
    }
}
