using DoAnV2.Application.Features.Shipments.Commands;
using DoAnV2.Application.Features.Shipments.Dtos;
using DoAnV2.Application.Features.Shipments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 09 - Mục 9.1: API Vận chuyển (Processor).
///   - POST /api/v1/processor/shipments/parent/{batchId}      (shipParent)
///   - POST /api/v1/processor/shipments/sub/{subBatchId}      (shipSub)
///   - GET  /api/v1/processor/shipments/parent/{batchId}/shipments
///   - GET  /api/v1/processor/shipments/sub/{subBatchId}/shipments
///
/// BR-18: Chỉ vận chuyển khi lô ở PACKAGED (sau khi đã đóng gói).
/// </summary>
[ApiController]
[Authorize(Policy = "RequireProcessor")]
public class ShipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShipmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/v1/processor/shipments/parent/{batchId}
    /// Tạo vận đơn cho Parent Batch. Body JSON theo ShipmentInputDto:
    ///   PickupLocation, Destination, RetailerId, CarrierInfo, ShippingCode,
    ///   ShippingDate, ExpectedDate, Weight.
    /// </summary>
    [HttpPost("api/v1/processor/shipments/parent/{batchId:guid}")]
    [Consumes("application/json")]
    public async Task<ActionResult<ShipmentResponseDto>> ShipParent(
        [FromRoute] Guid batchId,
        [FromBody] ShipmentInputDto input,
        CancellationToken ct)
    {
        if (input is null)
            throw new DoAnV2.Application.Common.Exceptions.ValidationException("Body không được rỗng.");

        var cmd = new ShipParentCommand(BatchId: batchId, Input: input);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/processor/shipments/sub/{subBatchId}
    /// Tạo vận đơn cho SubBatch.
    /// </summary>
    [HttpPost("api/v1/processor/shipments/sub/{subBatchId:guid}")]
    [Consumes("application/json")]
    public async Task<ActionResult<ShipmentResponseDto>> ShipSub(
        [FromRoute] Guid subBatchId,
        [FromBody] ShipmentInputDto input,
        CancellationToken ct)
    {
        if (input is null)
            throw new DoAnV2.Application.Common.Exceptions.ValidationException("Body không được rỗng.");

        var cmd = new ShipSubCommand(SubBatchId: subBatchId, Input: input);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/shipments/parent/{batchId}/shipments
    /// Lịch sử vận đơn của 1 Parent Batch.
    /// </summary>
    [HttpGet("api/v1/processor/shipments/parent/{batchId:guid}/shipments")]
    public async Task<ActionResult<IReadOnlyList<ShipmentHistoryDto>>> GetShipmentsByBatch(
        [FromRoute] Guid batchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetShipmentsByBatchQuery(batchId), ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/shipments/sub/{subBatchId}/shipments
    /// Lịch sử vận đơn của 1 SubBatch.
    /// </summary>
    [HttpGet("api/v1/processor/shipments/sub/{subBatchId:guid}/shipments")]
    public async Task<ActionResult<IReadOnlyList<ShipmentHistoryDto>>> GetShipmentsBySubBatch(
        [FromRoute] Guid subBatchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetShipmentsBySubBatchQuery(subBatchId), ct);
        return Ok(result);
    }
}