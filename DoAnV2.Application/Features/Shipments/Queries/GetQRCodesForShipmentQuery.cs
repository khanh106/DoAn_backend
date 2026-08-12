using DoAnV2.Application.Features.QRCodes.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Shipments.Queries;

/// <summary>
/// Query để Retailer lấy danh sách QR code đã được HTX tạo sẵn cho lô hàng của 1 vận đơn.
/// </summary>
public record GetQRCodesForShipmentQuery(Guid ShipmentId)
    : IRequest<IReadOnlyList<QRCodeInfoDto>>;
