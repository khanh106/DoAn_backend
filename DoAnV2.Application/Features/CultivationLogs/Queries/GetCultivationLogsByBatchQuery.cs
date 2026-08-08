using DoAnV2.Application.Features.CultivationLogs.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.CultivationLogs.Queries;

/// <summary>
/// TASK 06 - Mục 6.1: Lấy danh sách nhật ký canh tác của 1 lô.
/// - FARMER: chỉ xem được lô mình được phân công (BR-03).
/// - PROCESSOR: xem được các lô của mình.
/// - ADMIN: xem tất cả.
/// </summary>
public record GetCultivationLogsByBatchQuery(Guid BatchId)
    : IRequest<IReadOnlyList<CultivationLogDto>>;