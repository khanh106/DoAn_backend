using DoAnV2.Application.Features.Shipments.Commands;
using DoAnV2.Application.Features.Shipments.Dtos;
using DoAnV2.Application.Features.Shipments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 09 - Mục 9.2: API Cửa hàng / Siêu thị (Retailer).
///   - GET  /api/v1/retailer/shipments                                   (danh sách vận đơn)
///   - POST /api/v1/retailer/shipments/{id}/receive                     (receiveParent / receiveSub)
///   - POST /api/v1/retailer/shipments/{id}/ready-for-sale              (readyParent / readySub)
///
/// BR-18: Retailer chỉ được nhận khi lô ở STAGE_SHIPPING.
/// </summary>
[ApiController]
[Authorize(Policy = "RequireRetailer")]
public class RetailerController : ControllerBase
{
    private readonly IMediator _mediator;

    public RetailerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// GET /api/v1/retailer/shipments
    /// Danh sách các vận đơn được giao tới Cửa hàng/Siêu thị của Retailer đang đăng nhập.
    /// </summary>
    [HttpGet("api/v1/retailer/shipments")]
    public async Task<ActionResult<IReadOnlyList<ShipmentHistoryDto>>> GetMyShipments(
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyRetailerShipmentsQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/retailer/shipments/{id}/receive
    /// Retailer xác nhận tiếp nhận lô hàng (gọi SC receiveParent / receiveSub).
    /// Yêu cầu: lô phải ở STAGE_SHIPPING (BR-18).
    /// </summary>
    [HttpPost("api/v1/retailer/shipments/{id:guid}/receive")]
    public async Task<ActionResult<RetailerActionResponseDto>> Receive(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ReceiveShipmentCommand(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/retailer/shipments/{id}/ready-for-sale
    /// Retailer đưa sản phẩm lên kệ bán (gọi SC readyParent / readySub).
    /// Yêu cầu: lô phải ở RECEIVED_AT_RETAILER.
    /// </summary>
    [HttpPost("api/v1/retailer/shipments/{id:guid}/ready-for-sale")]
    public async Task<ActionResult<RetailerActionResponseDto>> ReadyForSale(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ReadyForSaleCommand(id), ct);
        return Ok(result);
    }
}