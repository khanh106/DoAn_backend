namespace DoAnV2.Application.Features.CultivationLogs.Dtos;

/// <summary>
/// DTO trả về cho nhật ký canh tác (TASK 06 - Mục 6.1).
/// Lưu OFF-CHAIN, không có transactionHash.
/// </summary>
public record CultivationLogDto(
    Guid Id,
    Guid BatchId,
    string BatchCode,
    Guid UserId,
    string UserFullName,
    string ActivityType,
    string Description,
    DateTime LogDate,
    string? MetadataURI,
    IReadOnlyList<string> ImageUrls,
    DateTime CreatedAt);