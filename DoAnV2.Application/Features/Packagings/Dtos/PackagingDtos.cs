namespace DoAnV2.Application.Features.Packagings.Dtos;

/// <summary>
/// DTO trả về sau khi Processor đóng gói thương mại (TASK 08 - Mục 8.2).
/// Chỉ sau khi kiểm định đạt (INSPECTION_PASSED) mới được đóng gói (BR-14).
/// </summary>
public record PackagingResponseDto(
    Guid PackagingId,
    string AssetType,
    Guid? BatchId,
    string? BatchCode,
    Guid? SubBatchId,
    string? SubBatchCode,
    DateTime PackDate,
    double Weight,
    string Specification,
    string? UsageGuide,
    string? StorageGuide,
    string? Color,
    string? Smell,
    string? Standard,
    IReadOnlyList<string> ImageUrls,
    string? Note,
    string CurrentStage,
    string? TransactionHash,
    DateTime CreatedAt);

/// <summary>
/// Lịch sử các phiếu đóng gói của 1 lô (Parent hoặc Sub).
/// </summary>
public record PackagingHistoryDto(
    Guid Id,
    string AssetType,
    Guid? BatchId,
    string? BatchCode,
    Guid? SubBatchId,
    string? SubBatchCode,
    DateTime PackDate,
    double Weight,
    string Specification,
    string? UsageGuide,
    string? StorageGuide,
    string? Color,
    string? Smell,
    string? Standard,
    IReadOnlyList<string> ImageUrls,
    string? Note,
    DateTime CreatedAt);
