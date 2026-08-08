using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Batches.Batches.Queries;

public record GetBatchByIdQuery(Guid Id) : IRequest<BatchDto?>;
