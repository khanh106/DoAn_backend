using DoAnV2.Application.Features.Admin.SystemLogs.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Admin.SystemLogs.Queries;

public record GetSystemLogsQuery(
    string? Category = null,
    string? Severity = null,
    string? Search = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<SystemLogsPagedResponseDto>;
