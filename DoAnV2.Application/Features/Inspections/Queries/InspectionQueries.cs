using DoAnV2.Application.Features.Inspections.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Inspections.Queries;

/// <summary>Lấy lịch sử kiểm định của 1 Parent Batch (TASK 08 - Mục 8.1).</summary>
public record GetInspectionsByBatchQuery(Guid BatchId)
    : IRequest<IReadOnlyList<InspectionHistoryDto>>;

/// <summary>Lấy lịch sử kiểm định của 1 SubBatch (TASK 08 - Mục 8.1).</summary>
public record GetInspectionsBySubBatchQuery(Guid SubBatchId)
    : IRequest<IReadOnlyList<InspectionHistoryDto>>;
