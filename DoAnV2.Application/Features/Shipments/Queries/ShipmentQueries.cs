using DoAnV2.Application.Features.Shipments.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Shipments.Queries;

/// <summary>TASK 09 - Mục 9.2: Retailer xem các lô hàng đang vận chuyển tới mình.</summary>
public record GetMyRetailerShipmentsQuery
    : IRequest<IReadOnlyList<ShipmentHistoryDto>>;

/// <summary>TASK 09 - Mục 9.1: Processor xem lịch sử vận chuyển của 1 Parent Batch.</summary>
public record GetShipmentsByBatchQuery(Guid BatchId)
    : IRequest<IReadOnlyList<ShipmentHistoryDto>>;

/// <summary>TASK 09 - Mục 9.1: Processor xem lịch sử vận chuyển của 1 SubBatch.</summary>
public record GetShipmentsBySubBatchQuery(Guid SubBatchId)
    : IRequest<IReadOnlyList<ShipmentHistoryDto>>;