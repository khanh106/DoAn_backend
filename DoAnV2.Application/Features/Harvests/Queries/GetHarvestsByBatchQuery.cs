using DoAnV2.Application.Features.Harvests.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Harvests.Queries;

/// <summary>Lấy lịch sử thu hoạch của 1 lô (HarvestHistory).</summary>
public record GetHarvestsByBatchQuery(Guid BatchId)
    : IRequest<IReadOnlyList<HarvestHistoryDto>>;