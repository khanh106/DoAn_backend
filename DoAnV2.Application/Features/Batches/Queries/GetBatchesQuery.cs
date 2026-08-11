using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Batches.Batches.Queries;

public record GetBatchesQuery : IRequest<IReadOnlyList<BatchDto>>;
