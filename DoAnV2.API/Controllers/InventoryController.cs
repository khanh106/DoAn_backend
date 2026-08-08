using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

[ApiController]
[Route("api/v1/processor/inventory")]
[Authorize(Policy = "RequireProcessor")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>POST /api/v1/processor/inventory/transactions - Nhập/Xuất kho.</summary>
    [HttpPost("transactions")]
    public async Task<ActionResult<InventoryLogDto>> CreateTransaction(
        [FromBody] CreateInventoryTransactionRequest body,
        CancellationToken ct)
    {
        var cmd = new CreateInventoryTransactionCommand(
            body.MaterialItemId, body.TransactionType, body.Quantity, body.Note);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/processor/inventory/logs - Lịch sử biến động kho.</summary>
    [HttpGet("logs")]
    public async Task<ActionResult<IReadOnlyList<InventoryLogDto>>> GetLogs(
        [FromQuery] Guid? materialItemId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInventoryLogsQuery(materialItemId), ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/processor/inventory/stock - Báo cáo tồn kho hiện tại.</summary>
    [HttpGet("stock")]
    public async Task<ActionResult<IReadOnlyList<StockItemDto>>> GetStock(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInventoryStockQuery(), ct);
        return Ok(result);
    }
}