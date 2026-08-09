namespace DoAnV2.Application.Features.Inspections.Dtos;

/// <summary>
/// DTO trả về sau khi Processor ghi nhận Kiểm định chất lượng (TASK 08 - Mục 8.1).
/// Áp dụng cho Parent Batch hoặc SubBatch. Đạt → PACKAGED; Không đạt → dừng (BR-14, BR-15).
/// </summary>
public record InspectionResponseDto(
    Guid InspectionId,
    string AssetType,
    Guid? BatchId,
    string? BatchCode,
    Guid? SubBatchId,
    string? SubBatchCode,
    string DocumentName,
    string DocumentNumber,
    string InspectionUnit,
    DateTime InspectionDate,
    string Result,
    string FileURI,
    string? DataHash,
    string? MetadataURI,
    string? Note,
    string CurrentStage,
    string? TransactionHash,
    DateTime CreatedAt);

/// <summary>
/// Lịch sử các lần kiểm định của 1 lô (Parent hoặc Sub).
/// </summary>
public record InspectionHistoryDto(
    Guid Id,
    string AssetType,
    Guid? BatchId,
    string? BatchCode,
    Guid? SubBatchId,
    string? SubBatchCode,
    string DocumentName,
    string DocumentNumber,
    string InspectionUnit,
    DateTime InspectionDate,
    string Result,
    string FileURI,
    string? Note,
    DateTime CreatedAt);
