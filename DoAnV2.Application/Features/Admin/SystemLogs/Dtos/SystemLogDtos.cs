namespace DoAnV2.Application.Features.Admin.SystemLogs.Dtos;

/// <summary>
/// DTO thông tin bản ghi Nhật ký hệ thống
/// </summary>
public record SystemLogDto(
    Guid Id,
    DateTime Timestamp,
    string Category,      // "BLOCKCHAIN" | "AUTH_USER" | "INVENTORY" | "CULTIVATION" | "SYSTEM"
    string Action,        // "USER_LOGIN" | "GRANT_ROLE" | "MINT_BATCH" | "RECORD_LOG" | "SYSTEM_ERROR"
    string Severity,      // "INFO" | "WARNING" | "ERROR" | "SUCCESS"
    string? ActorName,
    string? ActorEmail,
    string? ActorRole,
    string Description,
    string? IpAddress,
    string? TraceId,
    string? MetadataJson,
    string Status
);

/// <summary>
/// Thống kê phân loại log hệ thống
/// </summary>
public record SystemLogStatsDto(
    int TotalLogs,
    int InfoCount,
    int WarningCount,
    int ErrorCount,
    int SuccessCount
);

/// <summary>
/// Phản hồi danh sách log có phân trang & thống kê
/// </summary>
public record SystemLogsPagedResponseDto(
    IReadOnlyList<SystemLogDto> Logs,
    int TotalCount,
    int Page,
    int PageSize,
    SystemLogStatsDto Stats
);
