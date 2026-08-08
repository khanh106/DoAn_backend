using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Harvests.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Harvests.Queries;

/// <summary>
/// TASK 06: Lấy lịch sử thu hoạch / tiếp nhận của 1 Batch.
///   - FARMER: chỉ xem được lô mình được phân công (BR-03).
///   - PROCESSOR: xem được các lô của mình.
///   - ADMIN: xem tất cả.
/// </summary>
public class GetHarvestsByBatchQueryHandler
    : IRequestHandler<GetHarvestsByBatchQuery, IReadOnlyList<HarvestHistoryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetHarvestsByBatchQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<HarvestHistoryDto>> Handle(
        GetHarvestsByBatchQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        var role = _currentUser.Role?.ToUpperInvariant();
        var userId = _currentUser.UserId.Value;

        switch (role)
        {
            case "FARMER":
                if (!await _uow.BatchWorkers.ExistsAsync(batch.Id, userId, ct))
                    throw new ForbiddenException("Bạn không được phân công vào Batch này.");
                break;
            case "PROCESSOR":
                if (batch.ProcessorId != userId)
                    throw new ForbiddenException("Bạn không có quyền xem Batch của Processor khác.");
                break;
            case "ADMIN":
                break;
            default:
                throw new ForbiddenException("Không có quyền truy cập.");
        }

        var list = await _uow.Harvests.GetByBatchIdAsync(batch.Id, ct);
        return list.Select(h => new HarvestHistoryDto(
            Id: h.Id,
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            RepresentativeUserId: h.RepresentativeUserId,
            RepresentativeUserName: h.RepresentativeUser?.FullName ?? string.Empty,
            HarvestDate: h.HarvestDate,
            Quantity: h.Quantity,
            Unit: h.Unit,
            InitialQuality: h.InitialQuality,
            MetadataURI: h.MetadataURI,
            DataHash: h.DataHash,
            CreatedAt: h.CreatedAt)).ToList();
    }
}