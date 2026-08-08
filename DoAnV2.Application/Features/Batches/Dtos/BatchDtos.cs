namespace DoAnV2.Application.Features.Batches.Dtos;

public record BatchDto(
    Guid Id,
    string BatchCode,
    Guid FruitTypeId,
    string FruitTypeName,
    Guid ProductId,
    string ProductName,
    Guid FarmAreaId,
    string FarmAreaName,
    DateTime PlantingDate,
    double ExpectedQuantity,
    Guid? RepresentativeWorkerId,
    string? RepresentativeWorkerName,
    string CurrentStage,
    string? MetadataURI,
    string? DataHash,
    string? BlockchainBatchId,
    Guid ProcessorId,
    string ProcessorName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<BatchWorkerDto> Workers);

public record BatchWorkerDto(
    Guid UserId,
    string FullName,
    string? WalletAddress,
    bool IsRepresentative,
    DateTime AssignedDate,
    string Status);

// ====================== REQUESTS ======================

public record CreateBatchRequest(
    string BatchCode,
    Guid FruitTypeId,
    Guid ProductId,
    Guid FarmAreaId,
    DateTime PlantingDate,
    double ExpectedQuantity,
    IReadOnlyList<Guid> AssignedWorkerIds,
    Guid RepresentativeWorkerId);

public record AddWorkerToBatchRequest(Guid UserId);

public record ChangeRepresentativeRequest(Guid NewRepresentativeWorkerId);
