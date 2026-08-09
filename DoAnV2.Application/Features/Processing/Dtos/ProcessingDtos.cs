namespace DoAnV2.Application.Features.Processing.Dtos;

/// <summary>
/// DTO trả về sau khi Processor ghi nhận công đoạn Sơ chế (TASK 07 - Mục 7.1) - có TransactionHash.
/// </summary>
public record ProcessBatchResponseDto(
    Guid ProcessingId,
    Guid BatchId,
    string BatchCode,
    Guid ProcessedByUserId,
    string ProcessedByUserName,
    string ProcessType,
    string Description,
    DateTime StartDate,
    DateTime? EndDate,
    IReadOnlyList<string> ImageUrls,
    string? MetadataURI,
    string? DataHash,
    string CurrentStage,
    string? TransactionHash,
    DateTime CreatedAt);

/// <summary>
/// DTO trả về sau khi Processor phân loại KHÔNG tách lô (TASK 07 - Mục 7.2).
/// </summary>
public record ClassifyOnlyResponseDto(
    Guid BatchId,
    string BatchCode,
    Guid ClassifiedByUserId,
    string ClassifiedByUserName,
    string ClassificationNote,
    IReadOnlyList<GradeDetailDto> GradeDetails,
    string? MetadataURI,
    string? DataHash,
    string CurrentStage,
    string? TransactionHash,
    DateTime CreatedAt);

/// <summary>
/// Chi tiết 1 grade trong phân loại (cho classifyOnly).
/// </summary>
public record GradeDetailDto(
    string Grade,
    double Quantity,
    string? Note);

/// <summary>
/// DTO cho 1 SubBatch trả về sau khi tách lô (TASK 07 - Mục 7.3).
/// </summary>
public record SubBatchResponseDto(
    Guid Id,
    string SubBatchCode,
    Guid ParentBatchId,
    string ParentBatchCode,
    string Classification,
    double Quantity,
    BatchStageInfo CurrentStage,
    string? MetadataURI,
    string? DataHash,
    DateTime CreatedAt);

/// <summary>
/// Wrapper cho BatchStage (tránh xung đột namespace).
/// </summary>
public record BatchStageInfo(string Stage);

/// <summary>
/// DTO tổng hợp sau khi tách lô - trả về danh sách SubBatches.
/// </summary>
public record SplitBatchResponseDto(
    Guid ParentBatchId,
    string ParentBatchCode,
    Guid SplitByUserId,
    string SplitByUserName,
    double TotalSubBatchQuantity,
    IReadOnlyList<SubBatchResponseDto> SubBatches,
    string CurrentStage,
    string? TransactionHash,
    DateTime CreatedAt);

/// <summary>
/// Lịch sử các lần sơ chế của 1 lô.
/// </summary>
public record ProcessingHistoryDto(
    Guid Id,
    Guid BatchId,
    string BatchCode,
    string ProcessType,
    string Description,
    DateTime StartDate,
    DateTime? EndDate,
    IReadOnlyList<string> ImageUrls,
    string? MetadataURI,
    string? DataHash,
    DateTime CreatedAt);
