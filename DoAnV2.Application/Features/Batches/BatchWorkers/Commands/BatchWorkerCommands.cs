using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Batches.BatchWorkers.Commands;

public record AddWorkerToBatchCommand(Guid BatchId, Guid UserId) : IRequest<BatchDto>;

public record RemoveWorkerFromBatchCommand(Guid BatchId, Guid UserId) : IRequest<BatchDto>;

public record ChangeRepresentativeCommand(Guid BatchId, Guid NewRepresentativeWorkerId) : IRequest<BatchDto>;

public record AcceptBatchCommand(Guid BatchId) : IRequest<BatchWorkerAcceptedDto>;

public record BatchWorkerAcceptedDto(
    Guid BatchId,
    string BatchCode,
    string Status,
    string? TransactionHash);
