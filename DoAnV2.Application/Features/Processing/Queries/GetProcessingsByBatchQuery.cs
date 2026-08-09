using DoAnV2.Application.Features.Processing.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Processing.Queries;

/// <summary>Lấy lịch sử sơ chế (ProcessingHistory) của 1 Batch (TASK 07 - Mục 7.1).</summary>
public record GetProcessingsByBatchQuery(Guid BatchId)
    : IRequest<IReadOnlyList<ProcessingHistoryDto>>;
