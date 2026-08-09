using System.Text.Json;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Packagings.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.Packagings.Queries;

/// <summary>
/// TASK 08 - Mục 8.2: Lấy lịch sử đóng gói của 1 Batch (Parent hoặc Sub).
///   - PROCESSOR: chỉ xem được lô của mình.
///   - FARMER: chỉ xem được Batch mà mình được phân công.
///   - ADMIN: xem tất cả.
/// </summary>
public class PackagingQueryHandler
    : IRequestHandler<GetPackagingsByBatchQuery, IReadOnlyList<PackagingHistoryDto>>,
      IRequestHandler<GetPackagingsBySubBatchQuery, IReadOnlyList<PackagingHistoryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public PackagingQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PackagingHistoryDto>> Handle(
        GetPackagingsByBatchQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        await AuthorizeBatchAccessAsync(batch.Id, batch.ProcessorId, ct);

        var list = await _uow.Packagings.GetByBatchIdAsync(batch.Id, ct);

        return list.Select(p => new PackagingHistoryDto(
            Id: p.Id,
            AssetType: AssetType.PARENT.ToString(),
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            SubBatchId: null,
            SubBatchCode: null,
            PackDate: p.PackDate,
            Weight: p.Weight,
            Specification: p.Specification,
            UsageGuide: p.UsageGuide,
            StorageGuide: p.StorageGuide,
            Color: p.Color,
            Smell: p.Smell,
            Standard: p.Standard,
            ImageUrls: SafeParseUrls(p.ImageUrlsJson),
            Note: p.Note,
            CreatedAt: p.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<PackagingHistoryDto>> Handle(
        GetPackagingsBySubBatchQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var subBatch = await _uow.SubBatches.GetByIdAsync(req.SubBatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy SubBatch {req.SubBatchId}.");

        var parentBatch = await _uow.Batches.GetByIdAsync(subBatch.ParentBatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Parent Batch.");

        await AuthorizeBatchAccessAsync(parentBatch.Id, parentBatch.ProcessorId, ct);

        var list = await _uow.Packagings.GetBySubBatchIdAsync(subBatch.Id, ct);

        return list.Select(p => new PackagingHistoryDto(
            Id: p.Id,
            AssetType: AssetType.SUB.ToString(),
            BatchId: null,
            BatchCode: parentBatch.BatchCode,
            SubBatchId: subBatch.Id,
            SubBatchCode: subBatch.SubBatchCode,
            PackDate: p.PackDate,
            Weight: p.Weight,
            Specification: p.Specification,
            UsageGuide: p.UsageGuide,
            StorageGuide: p.StorageGuide,
            Color: p.Color,
            Smell: p.Smell,
            Standard: p.Standard,
            ImageUrls: SafeParseUrls(p.ImageUrlsJson),
            Note: p.Note,
            CreatedAt: p.CreatedAt)).ToList();
    }

    private async Task AuthorizeBatchAccessAsync(
        Guid batchId, Guid processorId, CancellationToken ct)
    {
        var role = _currentUser.Role?.ToUpperInvariant();
        var userId = _currentUser.UserId!.Value;

        switch (role)
        {
            case "FARMER":
                if (!await _uow.BatchWorkers.ExistsAsync(batchId, userId, ct))
                    throw new ForbiddenException("Bạn không được phân công vào Batch này.");
                break;
            case "PROCESSOR":
                if (processorId != userId)
                    throw new ForbiddenException("Bạn không có quyền xem Batch của Processor khác.");
                break;
            case "ADMIN":
                break;
            default:
                throw new ForbiddenException("Không có quyền truy cập.");
        }
    }

    private static IReadOnlyList<string> SafeParseUrls(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
