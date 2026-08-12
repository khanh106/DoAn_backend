using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.QRCodes.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;
using System.Linq;


namespace DoAnV2.Application.Features.Shipments.Queries;

/// <summary>
/// Handler: Retailer lấy danh sách QR code đã được HTX tạo sẵn cho lô hàng của 1 vận đơn.
/// Trả về tất cả QR codes (BATCH/SUBBATCH/BOX/COMMERCIAL) liên kết với batch/subbatch của shipment.
/// </summary>
public class GetQRCodesForShipmentQueryHandler
    : IRequestHandler<GetQRCodesForShipmentQuery, IReadOnlyList<QRCodeInfoDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetQRCodesForShipmentQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

        public async Task<IReadOnlyList<QRCodeInfoDto>> Handle(
        GetQRCodesForShipmentQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var shipment = await _uow.Shipments.GetByIdAsync(req.ShipmentId, ct)
            ?? throw new NotFoundException($"Không tìm thấy vận đơn {req.ShipmentId}.");

        // Chỉ retailer sở hữu shipment mới được xem
        if (shipment.RetailerId != _currentUser.UserId.Value)
            throw new ForbiddenException("Bạn không có quyền xem QR code của vận đơn này.");

        var result = new List<QRCodeInfoDto>();

        // Tất cả các loại QRTargetType cần kiểm tra gắn với Batch/SubBatch
        var targetTypes = new[] { QRTargetType.BATCH, QRTargetType.SUBBATCH, QRTargetType.BOX, QRTargetType.COMMERCIAL };

        // Lấy QR codes theo SubBatch (nếu có)
        if (shipment.SubBatchId.HasValue)
        {
            foreach (var type in targetTypes)
            {
                var qrs = await _uow.QRCodes.GetByTargetAsync(type, shipment.SubBatchId.Value, ct);
                result.AddRange(qrs.Select(MapToDto));
            }
        }

        // Lấy QR codes theo Batch (nếu có)
        if (shipment.BatchId.HasValue)
        {
            foreach (var type in targetTypes)
            {
                var qrs = await _uow.QRCodes.GetByTargetAsync(type, shipment.BatchId.Value, ct);
                result.AddRange(qrs.Select(MapToDto));
            }
        }

        // Loại bỏ các mã trùng lặp (nếu có) và trả về kết quả
        return result.DistinctBy(x => x.Id).ToList();
    }


    private static QRCodeInfoDto MapToDto(Domain.Entities.QRCode q) => new(
        Id: q.Id,
        TargetType: q.TargetType.ToString(),
        TargetId: q.TargetId,
        QRValue: q.QRValue,
        Status: q.Status.ToString(),
        CreatedAt: q.CreatedAt);
}
