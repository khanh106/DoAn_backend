using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Shipments.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.Shipments.Queries;

/// <summary>
/// TASK 09 - Mục 9.1 &amp; 9.2: Lấy danh sách vận đơn.
///   - RETAILER: chỉ xem shipment của mình (theo RetailerId).
///   - PROCESSOR: xem lịch sử vận chuyển của Batch/SubBatch thuộc Processor sở hữu.
///   - ADMIN: xem tất cả.
/// </summary>
public class ShipmentQueryHandler
    : IRequestHandler<GetMyRetailerShipmentsQuery, IReadOnlyList<ShipmentHistoryDto>>,
      IRequestHandler<GetShipmentsByBatchQuery, IReadOnlyList<ShipmentHistoryDto>>,
      IRequestHandler<GetShipmentsBySubBatchQuery, IReadOnlyList<ShipmentHistoryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public ShipmentQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ShipmentHistoryDto>> Handle(
        GetMyRetailerShipmentsQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var role = _currentUser.Role?.ToUpperInvariant();
        if (!string.Equals(role, "RETAILER", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Chỉ RETAILER mới xem được danh sách này.");

        var list = await _uow.Shipments.GetByRetailerIdAsync(_currentUser.UserId.Value, ct);

        return list.Select(MapToHistory).ToList();
    }

    public async Task<IReadOnlyList<ShipmentHistoryDto>> Handle(
        GetShipmentsByBatchQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        await AuthorizeProcessorAsync(batch.ProcessorId, ct);

        var list = await _uow.Shipments.GetByBatchIdAsync(batch.Id, ct);

        return list.Select(MapToHistory).ToList();
    }

    public async Task<IReadOnlyList<ShipmentHistoryDto>> Handle(
        GetShipmentsBySubBatchQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var subBatch = await _uow.SubBatches.GetByIdAsync(req.SubBatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy SubBatch {req.SubBatchId}.");

        var parentBatch = await _uow.Batches.GetByIdAsync(subBatch.ParentBatchId, ct)
            ?? throw new NotFoundException("Không tìm thấy Parent Batch.");

        await AuthorizeProcessorAsync(parentBatch.ProcessorId, ct);

        var list = await _uow.Shipments.GetBySubBatchIdAsync(subBatch.Id, ct);

        return list.Select(MapToHistory).ToList();
    }

    private Task AuthorizeProcessorAsync(Guid processorId, CancellationToken ct)
    {
        var role = _currentUser.Role?.ToUpperInvariant();
        var userId = _currentUser.UserId!.Value;

        switch (role)
        {
            case "PROCESSOR":
                if (processorId != userId)
                    throw new ForbiddenException("Bạn không có quyền xem vận đơn của Processor khác.");
                break;
            case "ADMIN":
                break;
            default:
                throw new ForbiddenException("Không có quyền truy cập.");
        }

        return Task.CompletedTask;
    }

    private static ShipmentHistoryDto MapToHistory(Domain.Entities.Shipment s) => new(
        Id: s.Id,
        AssetType: s.AssetType.ToString(),
        BatchId: s.BatchId,
        BatchCode: s.Batch?.BatchCode ?? s.SubBatch?.ParentBatch?.BatchCode,
        SubBatchId: s.SubBatchId,
        SubBatchCode: s.SubBatch?.SubBatchCode,
        RetailerId: s.RetailerId,
        RetailerName: s.Retailer?.FullName ?? string.Empty,
        PickupLocation: s.PickupLocation,
        Destination: s.Destination,
        CarrierInfo: s.CarrierInfo,
        ShippingCode: s.ShippingCode,
        ShippingDate: s.ShippingDate,
        ExpectedDate: s.ExpectedDate,
        ReceivedDate: s.ReceivedDate,
        ReadyForSaleDate: s.ReadyForSaleDate,
        Weight: s.Weight,
        ShipTransactionHash: s.ShipTransactionHash,
        ReceiveTransactionHash: s.ReceiveTransactionHash,
        ReadyTransactionHash: s.ReadyTransactionHash,
        CreatedAt: s.CreatedAt);
}