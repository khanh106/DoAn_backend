using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Batches.Batches.Commands;

public record CreateBatchCommand(
    string BatchCode,
    Guid FruitTypeId,
    Guid ProductId,
    Guid FarmAreaId,
    DateTime PlantingDate,
    double ExpectedQuantity,
    IReadOnlyList<Guid> AssignedWorkerIds,
    Guid RepresentativeWorkerId) : IRequest<BatchDto>;
