using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/products")]
[Authorize(Policy = "RequireProcessor")]
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/processor/products - Thêm sản phẩm.</summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest body, CancellationToken ct)
    {
        var cmd = new CreateProductCommand(
            body.FruitTypeId, body.GroupName, body.ProductType,
            body.Variety, body.Name, body.ShortName, body.Description);
        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    /// <summary>GET /api/v1/processor/products - Danh sách sản phẩm.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductsQuery(), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/v1/processor/products/{id} - Cập nhật sản phẩm.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest body,
        CancellationToken ct)
    {
        var cmd = new UpdateProductCommand(id, body.GroupName, body.ProductType, body.Variety,
            body.Name, body.ShortName, body.Description, body.Status);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }
}