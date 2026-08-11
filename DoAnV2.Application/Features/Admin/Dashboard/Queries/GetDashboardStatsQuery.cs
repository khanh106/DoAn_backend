using DoAnV2.Application.Features.Admin.Dashboard.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Admin.Dashboard.Queries;

/// <summary>
/// TASK 11 - Mục 11.1: Query lấy thống kê tổng quan cho Dashboard Admin.
/// </summary>
public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
