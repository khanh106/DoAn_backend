namespace DoAnV2.Application.Features.Harvests.Dtos;

/// <summary>
/// DTO trả về sau khi xác nhận thu hoạch (TASK 06 - Mục 6.2) - có kèm TransactionHash.
/// </summary>
public record HarvestBatchResponseDto(
    Guid HarvestId,
    Guid BatchId,
    string BatchCode,
    Guid RepresentativeUserId,
    string RepresentativeUserName,
    DateTime HarvestDate,
    double Quantity,
    string Unit,
    string InitialQuality,
    string? Notes,
    string? MetadataURI,
    string? DataHash,
    string CurrentStage,
    string? TransactionHash,
    DateTime CreatedAt);

/// <summary>
/// DTO trả về sau khi Processor tiếp nhận lô sau thu hoạch (TASK 06 - Mục 6.3) - có TransactionHash.
/// </summary>
public record ReceiveBatchResponseDto(
    Guid ReceiveId,
    Guid BatchId,
    string BatchCode,
    Guid ReceivedByUserId,
    string ReceivedByUserName,
    DateTime ReceivedDate,
    double Quantity,
    string Unit,
    string DeliveryPerson,
    string ConditionNote,
    string? MetadataURI,
    string? DataHash,
    string CurrentStage,
    string? TransactionHash,
    DateTime CreatedAt);

/// <summary>
/// Lịch sử các lần thu hoạch / tiếp nhận của 1 lô.
/// </summary>
public record HarvestHistoryDto(
    Guid Id,
    Guid BatchId,
    string BatchCode,
    Guid RepresentativeUserId,
    string RepresentativeUserName,
    DateTime HarvestDate,
    double Quantity,
    string Unit,
    string InitialQuality,
    string? MetadataURI,
    string? DataHash,
    DateTime CreatedAt);