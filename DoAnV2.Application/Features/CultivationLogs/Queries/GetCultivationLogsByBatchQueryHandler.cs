using System.Text.Json;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.CultivationLogs.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.CultivationLogs.Queries;

/// <summary>
/// TASK 06 - Mục 6.1: Handler lấy danh sách nhật ký canh tác của 1 Batch.
///   - FARMER: phải có trong BatchWorker (BR-03).
///   - PROCESSOR: phải là chủ sở hữu Batch.
///   - ADMIN: xem tất cả.
/// </summary>
public class GetCultivationLogsByBatchQueryHandler
    : IRequestHandler<GetCultivationLogsByBatchQuery, IReadOnlyList<CultivationLogDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetCultivationLogsByBatchQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CultivationLogDto>> Handle(
        GetCultivationLogsByBatchQuery req, CancellationToken ct)
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

        var logs = await _uow.CultivationLogs.GetByBatchIdAsync(batch.Id, ct);
        var list = new List<CultivationLogDto>(logs.Count);
        foreach (var log in logs)
        {
            IReadOnlyList<string> images = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(log.ImageUrlsJson))
            {
                try
                {
                    images = JsonSerializer.Deserialize<List<string>>(log.ImageUrlsJson)
                             ?? new List<string>();
                }
                catch
                {
                    images = Array.Empty<string>();
                }
            }

            list.Add(new CultivationLogDto(
                Id: log.Id,
                BatchId: batch.Id,
                BatchCode: batch.BatchCode,
                UserId: log.UserId,
                UserFullName: log.User?.FullName ?? string.Empty,
                ActivityType: log.ActivityType,
                Description: log.Description,
                LogDate: log.LogDate,
                MetadataURI: log.MetadataURI,
                ImageUrls: images,
                CreatedAt: log.CreatedAt));
        }
        return list;
    }
}