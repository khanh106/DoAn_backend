using System.Text.Json;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Processing.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Processing.Queries;

/// <summary>
/// TASK 07: Lấy lịch sử sơ chế của 1 Batch.
///   - PROCESSOR: chỉ xem được Batch của mình.
///   - FARMER: chỉ xem được Batch mà mình được phân công.
///   - ADMIN: xem tất cả.
/// </summary>
public class GetProcessingsByBatchQueryHandler
    : IRequestHandler<GetProcessingsByBatchQuery, IReadOnlyList<ProcessingHistoryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetProcessingsByBatchQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProcessingHistoryDto>> Handle(
        GetProcessingsByBatchQuery req, CancellationToken ct)
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

        var list = await _uow.Processings.GetByBatchIdAsync(batch.Id, ct);

        return list.Select(p =>
        {
            var urls = SafeParseUrls(p.ImageUrlsJson);
            return new ProcessingHistoryDto(
                Id: p.Id,
                BatchId: batch.Id,
                BatchCode: batch.BatchCode,
                ProcessType: p.ProcessType,
                Description: p.Description,
                StartDate: p.StartDate,
                EndDate: p.EndDate,
                ImageUrls: urls,
                MetadataURI: p.MetadataURI,
                DataHash: p.DataHash,
                CreatedAt: p.CreatedAt);
        }).ToList();
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
