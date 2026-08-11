using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Inspections.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.Inspections.Queries;

/// <summary>
/// TASK 08 - Mục 8.1: Lấy lịch sử kiểm định của 1 Batch (Parent hoặc Sub).
///   - PROCESSOR: chỉ xem được lô của mình.
///   - FARMER: chỉ xem được Batch mà mình được phân công.
///   - ADMIN: xem tất cả.
/// </summary>
public class InspectionQueryHandler
    : IRequestHandler<GetInspectionsByBatchQuery, IReadOnlyList<InspectionHistoryDto>>,
      IRequestHandler<GetInspectionsBySubBatchQuery, IReadOnlyList<InspectionHistoryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public InspectionQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InspectionHistoryDto>> Handle(
        GetInspectionsByBatchQuery req, CancellationToken ct)
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
            case "RETAILER":
            case "ADMIN":
                break;
            default:
                throw new ForbiddenException("Không có quyền truy cập.");
        }

        var list = await _uow.Inspections.GetByBatchIdAsync(batch.Id, ct);

        return list.Select(i => new InspectionHistoryDto(
            Id: i.Id,
            AssetType: AssetType.PARENT.ToString(),
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            SubBatchId: null,
            SubBatchCode: null,
            DocumentName: i.DocumentName,
            DocumentNumber: i.DocumentNumber,
            InspectionUnit: i.InspectionUnit,
            InspectionDate: i.InspectionDate,
            Result: i.Result.ToString(),
            FileURI: i.FileURI,
            Note: i.Note,
            CreatedAt: i.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<InspectionHistoryDto>> Handle(
        GetInspectionsBySubBatchQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var subBatch = await _uow.SubBatches.GetByIdAsync(req.SubBatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy SubBatch {req.SubBatchId}.");

        var parentBatch = await _uow.Batches.GetByIdAsync(subBatch.ParentBatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Parent Batch.");

        var role = _currentUser.Role?.ToUpperInvariant();
        var userId = _currentUser.UserId.Value;

        switch (role)
        {
            case "FARMER":
                if (!await _uow.BatchWorkers.ExistsAsync(parentBatch.Id, userId, ct))
                    throw new ForbiddenException("Bạn không được phân công vào Batch này.");
                break;
            case "PROCESSOR":
                if (parentBatch.ProcessorId != userId)
                    throw new ForbiddenException("Bạn không có quyền xem SubBatch của Processor khác.");
                break;
            case "RETAILER":
            case "ADMIN":
                break;
            default:
                throw new ForbiddenException("Không có quyền truy cập.");
        }

        var list = await _uow.Inspections.GetBySubBatchIdAsync(subBatch.Id, ct);

        return list.Select(i => new InspectionHistoryDto(
            Id: i.Id,
            AssetType: AssetType.SUB.ToString(),
            BatchId: null,
            BatchCode: parentBatch.BatchCode,
            SubBatchId: subBatch.Id,
            SubBatchCode: subBatch.SubBatchCode,
            DocumentName: i.DocumentName,
            DocumentNumber: i.DocumentNumber,
            InspectionUnit: i.InspectionUnit,
            InspectionDate: i.InspectionDate,
            Result: i.Result.ToString(),
            FileURI: i.FileURI,
            Note: i.Note,
            CreatedAt: i.CreatedAt)).ToList();
    }
}
