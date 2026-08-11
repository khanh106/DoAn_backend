namespace DoAnV2.Application.Features.Admin.Dashboard.Dtos;

/// <summary>
/// TASK 11 - Mục 11.1: Response cho GET /api/v1/admin/dashboard/stats.
/// Thống kê tổng quan: User, Batch, Blockchain Transaction.
/// </summary>
public record DashboardStatsDto(
    UserStatsSection UserStats,
    BatchStatsSection BatchStats,
    BlockchainStatsSection BlockchainStats);

public record UserStatsSection(
    int TotalUsers,
    int FarmersCount,
    int ProcessorsCount,
    int RetailersCount,
    int ActiveCount,
    int PendingCount,
    int LockedCount);

public record BatchStatsSection(
    int TotalBatches,
    int InProductionCount,
    int HarvestedCount,
    int PackagedCount,
    int ReadyForSaleCount);

public record BlockchainStatsSection(
    int TotalTransactions,
    int SuccessfulTransactions,
    int FailedTransactions);
