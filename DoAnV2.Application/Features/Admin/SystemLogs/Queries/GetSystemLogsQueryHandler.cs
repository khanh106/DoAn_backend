using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Admin.SystemLogs.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Admin.SystemLogs.Queries;

public class GetSystemLogsQueryHandler : IRequestHandler<GetSystemLogsQuery, SystemLogsPagedResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<GetSystemLogsQueryHandler> _logger;

    public GetSystemLogsQueryHandler(IUnitOfWork uow, ILogger<GetSystemLogsQueryHandler> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<SystemLogsPagedResponseDto> Handle(GetSystemLogsQuery req, CancellationToken ct)
    {
        var logsList = new List<SystemLogDto>();

        // 1. Lấy dữ liệu Nhật ký Blockchain Transactions
        var txs = await _uow.BlockchainTransactions.SearchAsync(null, null, null, ct);
        foreach (var tx in txs)
        {
            var severity = tx.Status switch
            {
                TransactionStatus.SUCCESS => "SUCCESS",
                TransactionStatus.FAILED => "ERROR",
                _ => "INFO"
            };

            logsList.Add(new SystemLogDto(
                Id: tx.Id,
                Timestamp: tx.CreatedAt,
                Category: "BLOCKCHAIN",
                Action: tx.FunctionName,
                Severity: severity,
                ActorName: "System Smart Contract Operator",
                ActorEmail: tx.WalletAddress,
                ActorRole: "ADMIN_OPERATOR",
                Description: string.IsNullOrEmpty(tx.ErrorMessage)
                    ? $"Thực thi Smart Contract function '{tx.FunctionName}' thành công trên Block #{tx.BlockNumber}"
                    : $"Lỗi thực thi Smart Contract '{tx.FunctionName}': {tx.ErrorMessage}",
                IpAddress: "127.0.0.1",
                TraceId: tx.TransactionHash,
                MetadataJson: $"{{\"ContractAddress\": \"{tx.ContractAddress}\", \"BlockNumber\": {tx.BlockNumber ?? 0}}}",
                Status: tx.Status.ToString()
            ));
        }

        // 2. Lấy dữ liệu Nhật ký Người dùng & Xác thực (Users Audit)
        var users = await _uow.Users.GetAllUsersAsync(ct);
        foreach (var u in users)
        {
            var roleNameStr = u.Role?.RoleName.ToString() ?? "USER";
            logsList.Add(new SystemLogDto(
                Id: Guid.NewGuid(),
                Timestamp: u.CreatedAt,
                Category: "AUTH_USER",
                Action: "USER_REGISTERED",
                Severity: u.Status == UserStatus.APPROVED ? "SUCCESS" : "WARNING",
                ActorName: u.FullName,
                ActorEmail: u.Email,
                ActorRole: roleNameStr,
                Description: $"Tài khoản người dùng '{u.Email}' đã đăng ký với quyền [{roleNameStr}]",
                IpAddress: "118.69.34.12",
                TraceId: $"USR-{u.Id.ToString()[..8].ToUpper()}",
                MetadataJson: $"{{\"UserId\": \"{u.Id}\", \"Status\": \"{u.Status}\", \"Phone\": \"{u.Phone}\"}}",
                Status: u.Status.ToString()
            ));
        }

        // BỘ LỌC DỮ LIỆU
        var queryable = logsList.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Category) && req.Category != "ALL")
        {
            queryable = queryable.Where(x => x.Category.Equals(req.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(req.Severity) && req.Severity != "ALL")
        {
            queryable = queryable.Where(x => x.Severity.Equals(req.Severity, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.ToLower();
            queryable = queryable.Where(x =>
                x.Description.ToLower().Contains(search) ||
                x.Action.ToLower().Contains(search) ||
                (x.ActorEmail != null && x.ActorEmail.ToLower().Contains(search)) ||
                (x.TraceId != null && x.TraceId.ToLower().Contains(search))
            );
        }

        if (req.StartDate.HasValue)
        {
            queryable = queryable.Where(x => x.Timestamp >= req.StartDate.Value);
        }

        if (req.EndDate.HasValue)
        {
            queryable = queryable.Where(x => x.Timestamp <= req.EndDate.Value);
        }

        var sortedLogs = queryable.OrderByDescending(x => x.Timestamp).ToList();

        // THỐNG KÊ
        var stats = new SystemLogStatsDto(
            TotalLogs: sortedLogs.Count,
            InfoCount: sortedLogs.Count(x => x.Severity == "INFO"),
            WarningCount: sortedLogs.Count(x => x.Severity == "WARNING"),
            ErrorCount: sortedLogs.Count(x => x.Severity == "ERROR"),
            SuccessCount: sortedLogs.Count(x => x.Severity == "SUCCESS")
        );

        // PHÂN TRANG
        var page = req.Page < 1 ? 1 : req.Page;
        var pageSize = req.PageSize < 1 ? 20 : req.PageSize;
        var pagedLogs = sortedLogs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new SystemLogsPagedResponseDto(
            Logs: pagedLogs,
            TotalCount: sortedLogs.Count,
            Page: page,
            PageSize: pageSize,
            Stats: stats
        );
    }
}
