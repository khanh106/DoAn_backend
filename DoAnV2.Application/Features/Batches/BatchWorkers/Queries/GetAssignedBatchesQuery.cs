using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Batches.BatchWorkers.Queries;

public record GetAssignedBatchesQuery : IRequest<IReadOnlyList<AssignedBatchDto>>;

public record AssignedBatchDto(
    Guid BatchId,
    string BatchCode,
    string FruitTypeName,
    string ProductName,
    string FarmAreaName,
    string CurrentStage,
    DateTime PlantingDate,
    double ExpectedQuantity,
    bool IsRepresentative,
    DateTime AssignedDate,
    string WorkerStatus);
