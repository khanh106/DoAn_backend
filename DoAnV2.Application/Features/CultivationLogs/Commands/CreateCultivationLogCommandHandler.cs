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
        "Cắt tỉa",
        "Kiểm tra sâu bệnh",
        "Làm cỏ",
        "Khác",
        "Phun thuốc bảo vệ thực vật",
        "Làm cỏ & Cắt tỉa",
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
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var userId = _currentUser.UserId.Value;
        var role = _currentUser.Role?.ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(req.ActivityType))
            throw new ValidationException("ActivityType không được trống.");
        if (!AllowedActivityTypes.Contains(req.ActivityType.Trim()))
            throw new ValidationException(
                $"ActivityType '{req.ActivityType}' không hợp lệ. Cho phép: {string.Join(", ", AllowedActivityTypes)}.");
        if (string.IsNullOrWhiteSpace(req.Description))
            throw new ValidationException("Description không được trống.");
        if (req.LogDate == default)
            throw new ValidationException("LogDate không hợp lệ.");

        // ========== 2. Validate Batch + Permission ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.CurrentStage != DoAnV2.Domain.Enums.BatchStage.STAGE_PLANTING)
        {
            throw new ValidationException(
                "Lô sản xuất đã hoàn thành thu hoạch hoặc đang ở giai đoạn sau thu hoạch. Không được phép ghi nhật ký canh tác.");
        }

        string userFullName = string.Empty;
        if (role == "FARMER")
        {
            var bw = await _uow.BatchWorkers.GetAsync(req.BatchId, userId, ct)
                ?? throw new ForbiddenException("Bạn không được phân công vào Batch này.");
            userFullName = bw.User?.FullName ?? string.Empty;
        }
        else if (role == "PROCESSOR" || role == "COOPERATIVE" || role == "ADMIN")
        {
            if (role == "PROCESSOR" && batch.ProcessorId != userId)
                throw new ForbiddenException("Bạn không có quyền quản lý Batch này.");
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            userFullName = user?.FullName ?? "Hợp tác xã";
        }
        else
        {
            throw new ForbiddenException("Không có quyền thực hiện thao tác này.");
        }

        
// ========== 3. Upload song song: ảnh + metadata JSON ==========
var images = req.Images ?? new List<IFormFile>();
var validImages = images.Where(img => img is not null && img.Length > 0).ToList();

// Task upload ảnh (chạy nền)
var imagesTask = Task.WhenAll(
    validImages.Select(img => _ipfs.UploadFileAsync(img, ct))
);

// Task upload metadata JSON (chạy nền)
var metadataTask = UploadMetadataSafeAsync(batch.BatchCode, userId, req, ct);

// Đợi cả hai — chạy song song, tổng thời gian = max(ảnh, metadata)
var imageResults = await imagesTask;
var metadataUri = await metadataTask;
var imageUris = imageResults.Select(r => r.FileURI).ToList();

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
            UserFullName: userFullName,
            ActivityType: log.ActivityType,
            Description: log.Description,
            LogDate: log.LogDate,
             MetadataURI: log.MetadataURI,
            ImageUrls: imageUris,
            CreatedAt: log.CreatedAt);
    }

    /// <summary>
    /// Helper: Upload metadata JSON lên IPFS - không block nghiệp vụ nếu lỗi.
    /// Được chạy song song với upload ảnh trong Handle() để tiết kiệm ~1.5s.
    /// </summary>
    private async Task<string?> UploadMetadataSafeAsync(
        string batchCode,
        Guid userId,
        CreateCultivationLogCommand req,
        CancellationToken ct)
    {
        try
        {
            var metadata = new
            {
                batchId = req.BatchId,
                batchCode = batchCode,
                workerId = userId,
                activityType = req.ActivityType.Trim(),
                description = req.Description,
                logDate = req.LogDate,
                imageCount = (req.Images ?? new List<IFormFile>())
                    .Count(i => i is not null && i.Length > 0),
                uploadedAt = DateTime.UtcNow,
            };

            var (uri, _) = await _ipfs.UploadJsonAsync(
                metadata,
                fileName: $"cultivation-{batchCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
                ct: ct);
            return uri;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Upload metadata JSON cho cultivation log của Batch {BatchId} thất bại - tiếp tục lưu ảnh.",
                req.BatchId);
            return null;
        }
    }
}