using System.Text.Json;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.CultivationLogs.Dtos;
using DoAnV2.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.CultivationLogs.Commands;

/// <summary>
/// TASK 06 - Mục 6.1: Handler ghi nhật ký canh tác.
///   1. Validate Worker được phân công vào batch (BR-03).
///   2. Upload danh sách ảnh lên IPFS ➔ danh sách FileURIs.
///   3. Lưu CultivationLog vào SQL (OFF-CHAIN, BR-07/BR-08 - KHÔNG gọi SC).
/// </summary>
public class CreateCultivationLogCommandHandler
    : IRequestHandler<CreateCultivationLogCommand, CultivationLogDto>
{
    // BR-07 / BR-08: cho phép nhưng nên giới hạn 1 dòng ActivityType.
    private static readonly HashSet<string> AllowedActivityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tưới nước",
        "Bón phân",
        "Phun thuốc",
        "Chăm sóc",
    };

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly ILogger<CreateCultivationLogCommandHandler> _logger;

    public CreateCultivationLogCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        ILogger<CreateCultivationLogCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _logger = logger;
    }

    public async Task<CultivationLogDto> Handle(
        CreateCultivationLogCommand req, CancellationToken ct)
    {
        // ========== 1. Auth & validate input ==========
        var userId = Guard.RequireFarmer(_currentUser);

        if (string.IsNullOrWhiteSpace(req.ActivityType))
            throw new ValidationException("ActivityType không được trống.");
        if (!AllowedActivityTypes.Contains(req.ActivityType.Trim()))
            throw new ValidationException(
                $"ActivityType '{req.ActivityType}' không hợp lệ. Cho phép: {string.Join(", ", AllowedActivityTypes)}.");
        if (string.IsNullOrWhiteSpace(req.Description))
            throw new ValidationException("Description không được trống.");
        if (req.LogDate == default)
            throw new ValidationException("LogDate không hợp lệ.");

        // ========== 2. Validate Batch + Worker assignment (BR-03) ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        var bw = await _uow.BatchWorkers.GetAsync(req.BatchId, userId, ct)
            ?? throw new ForbiddenException("Bạn không được phân công vào Batch này.");

        // ========== 3. Upload danh sách ảnh lên IPFS ==========
        var imageUris = new List<string>();
        var images = req.Images ?? new List<IFormFile>();
        foreach (var img in images)
        {
            if (img is null || img.Length == 0) continue;
            var (fileUri, _) = await _ipfs.UploadFileAsync(img, ct);
            imageUris.Add(fileUri);
        }

        // ========== 4. Upload metadata JSON (cho traceability, optional) ==========
        var metadata = new
        {
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            workerId = userId,
            activityType = req.ActivityType.Trim(),
            description = req.Description,
            logDate = req.LogDate,
            imageCount = imageUris.Count,
            uploadedAt = DateTime.UtcNow,
        };

        string? metadataUri = null;
        try
        {
            var (uri, _) = await _ipfs.UploadJsonAsync(
                metadata,
                fileName: $"cultivation-{batch.BatchCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
                ct: ct);
            metadataUri = uri;
        }
        catch (Exception ex)
        {
            // Không block nghiệp vụ nếu upload metadata JSON lỗi.
            _logger.LogWarning(ex,
                "Upload metadata JSON cho cultivation log của Batch {BatchId} thất bại - tiếp tục lưu ảnh.",
                batch.Id);
        }

        // ========== 5. Lưu CultivationLog OFF-CHAIN (BR-07/BR-08) ==========
        var log = new CultivationLog
        {
            BatchId = batch.Id,
            UserId = userId,
            ActivityType = req.ActivityType.Trim(),
            Description = req.Description.Trim(),
            LogDate = req.LogDate,
            MetadataURI = metadataUri,
            ImageUrlsJson = JsonSerializer.Serialize(imageUris),
        };
        await _uow.CultivationLogs.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);

        return new CultivationLogDto(
            Id: log.Id,
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            UserId: userId,
            UserFullName: bw.User?.FullName ?? string.Empty,
            ActivityType: log.ActivityType,
            Description: log.Description,
            LogDate: log.LogDate,
            MetadataURI: log.MetadataURI,
            ImageUrls: imageUris,
            CreatedAt: log.CreatedAt);
    }
}