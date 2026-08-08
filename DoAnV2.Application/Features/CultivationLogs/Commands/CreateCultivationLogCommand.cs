using DoAnV2.Application.Features.CultivationLogs.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace DoAnV2.Application.Features.CultivationLogs.Commands;

/// <summary>
/// TASK 06 - Mục 6.1: Worker/ Farmer ghi nhật ký canh tác cho lô mình được phân công.
/// - Validate Worker có trong BatchWorker (BR-03).
/// - Upload danh sách ảnh lên IPFS.
/// - Lưu bản ghi CultivationLog vào SQL Server (OFF-CHAIN - KHÔNG gọi SC, BR-07/BR-08).
/// </summary>
public record CreateCultivationLogCommand(
    Guid BatchId,
    string ActivityType,
    string Description,
    DateTime LogDate,
    IReadOnlyList<IFormFile> Images) : IRequest<CultivationLogDto>;