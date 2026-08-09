using DoAnV2.Application.Features.Packagings.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Packagings.Queries;

/// <summary>Lấy lịch sử đóng gói của 1 Parent Batch (TASK 08 - Mục 8.2).</summary>
public record GetPackagingsByBatchQuery(Guid BatchId)
    : IRequest<IReadOnlyList<PackagingHistoryDto>>;

/// <summary>Lấy lịch sử đóng gói của 1 SubBatch (TASK 08 - Mục 8.2).</summary>
public record GetPackagingsBySubBatchQuery(Guid SubBatchId)
    : IRequest<IReadOnlyList<PackagingHistoryDto>>;
