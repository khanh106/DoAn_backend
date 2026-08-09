using DoAnV2.Application.Features.Shipments.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Shipments.Commands;

/// <summary>
/// Input cho Processor khi tạo vận đơn (TASK 09 - Mục 9.1).
/// </summary>
public record ShipmentInputDto(
    string PickupLocation,
    string Destination,
    Guid RetailerId,
    string CarrierInfo,
    string ShippingCode,
    DateTime ShippingDate,
    DateTime? ExpectedDate,
    double Weight);

/// <summary>
/// TASK 09 - Mục 9.1: Processor tạo vận đơn cho Parent Batch (gọi SC shipParent).
/// BR-18: Chỉ vận chuyển khi lô ở PACKAGED.
/// </summary>
public record ShipParentCommand(
    Guid BatchId,
    ShipmentInputDto Input) : IRequest<ShipmentResponseDto>;

/// <summary>
/// TASK 09 - Mục 9.1: Processor tạo vận đơn cho SubBatch (gọi SC shipSub).
/// BR-18: Chỉ vận chuyển khi SubBatch ở PACKAGED.
/// </summary>
public record ShipSubCommand(
    Guid SubBatchId,
    ShipmentInputDto Input) : IRequest<ShipmentResponseDto>;

/// <summary>
/// TASK 09 - Mục 9.2: Retailer xác nhận tiếp nhận lô hàng (gọi SC receiveParent / receiveSub).
/// BR-18: Chỉ nhận khi lô ở STAGE_SHIPPING.
/// </summary>
public record ReceiveShipmentCommand(
    Guid ShipmentId) : IRequest<RetailerActionResponseDto>;

/// <summary>
/// TASK 09 - Mục 9.2: Retailer xác nhận đưa sản phẩm lên kệ bán (gọi SC readyParent / readySub).
/// Yêu cầu: lô phải ở RECEIVED_AT_RETAILER.
/// </summary>
public record ReadyForSaleCommand(
    Guid ShipmentId) : IRequest<RetailerActionResponseDto>;