using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Admin.Dashboard.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Admin.Dashboard.Queries;

/// <summary>
/// TASK 11 - Mục 11.1: Handler lấy thống kê Dashboard Admin.
///   - UserStats: tổng số user, phân bổ theo Role + Status.
///   - BatchStats: tổng số Parent Batch theo từng stage quan trọng.
///   - BlockchainStats: tổng số tx on-chain + số SUCCESS / FAILED.
/// </summary>
public class GetDashboardStatsQueryHandler
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

    public GetDashboardStatsQueryHandler(
        IUnitOfWork uow,
        ILogger<GetDashboardStatsQueryHandler> logger)
    {
        _uow = uow;
        _logger = logger;
    }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery req, CancellationToken ct)
    {
        // Thực thi tuần tự 3 truy vấn để tránh xung đột DbContext trong EF Core
        var userStats = await _uow.Users.GetStatsAsync(ct);
        var batchStats = await _uow.Batches.GetStatsAsync(ct);
        var txStats = await _uow.BlockchainTransactions.CountByStatusAsync(ct);

        _logger.LogInformation(
            "Dashboard stats: users={U} batches={B} tx={T}",
            userStats.Total, batchStats.Total, txStats.Total);

        return new DashboardStatsDto(
            UserStats: new UserStatsSection(
                TotalUsers: userStats.Total,
                FarmersCount: userStats.Farmers,
                ProcessorsCount: userStats.Processors,
                RetailersCount: userStats.Retailers,
                ActiveCount: userStats.Active,
                PendingCount: userStats.Pending,
                LockedCount: userStats.Locked),
            BatchStats: new BatchStatsSection(
                TotalBatches: batchStats.Total,
                InProductionCount: batchStats.InProduction,
                HarvestedCount: batchStats.Harvested,
                PackagedCount: batchStats.Packaged,
                ReadyForSaleCount: batchStats.ReadyForSale),
            BlockchainStats: new BlockchainStatsSection(
                TotalTransactions: txStats.Total,
                SuccessfulTransactions: txStats.Success,
                FailedTransactions: txStats.Failed));
    }

}
