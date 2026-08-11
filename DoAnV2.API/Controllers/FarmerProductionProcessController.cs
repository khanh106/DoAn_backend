using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using DoAnV2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.API.Controllers;

/// <summary>
/// API Hướng dẫn Quy trình sản xuất cho tài khoản Nông dân (Farmer).
/// Chỉ hiển thị các quy trình sản xuất của các Hợp tác xã / Cơ sở chế biến mà Farmer đã liên kết.
/// </summary>
[ApiController]
[Route("api/v1/farmer/processes")]
[Authorize(Policy = "RequireFarmer")]
public class FarmerProductionProcessController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public FarmerProductionProcessController(ApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// GET /api/v1/farmer/processes - Danh sách quy trình sản xuất từ các HTX đã liên kết.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FarmerProductionProcessDto>>> GetLinkedProcesses(CancellationToken ct)
    {
        var farmerId = _currentUser.UserId;
        if (farmerId is null) return Unauthorized();

        // 1. Lấy danh sách ProcessorId từ ProcessorWorkers (Status == ACCEPTED)
        var linkedProcessorIdsFromWorkers = await _dbContext.ProcessorWorkers
            .Where(pw => pw.WorkerId == farmerId.Value && pw.Status == CoopWorkerLinkStatus.ACCEPTED)
            .Select(pw => pw.ProcessorId)
            .ToListAsync(ct);

        // 2. Lấy danh sách ProcessorId từ các Lô mà Farmer được phân công
        var linkedProcessorIdsFromBatches = await _dbContext.BatchWorkers
            .Include(bw => bw.Batch)
            .Where(bw => bw.UserId == farmerId.Value && bw.Batch != null)
            .Select(bw => bw.Batch.ProcessorId)
            .ToListAsync(ct);

        // Gộp tất cả ProcessorId liên kết (Loại trùng lặp)
        var allProcessorIds = linkedProcessorIdsFromWorkers
            .Concat(linkedProcessorIdsFromBatches)
            .Distinct()
            .ToList();

        if (!allProcessorIds.Any())
        {
            return Ok(new List<FarmerProductionProcessDto>());
        }

        // 3. Lấy thông tin User đại diện HTX
        var processorUsers = await _dbContext.Users
            .Where(u => allProcessorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        // 4. Lấy tất cả Quy trình sản xuất thuộc về các ProcessorId đó
        var processes = await _dbContext.ProductionProcesses
            .Include(p => p.Steps)
            .Where(p => allProcessorIds.Contains(p.ProcessorId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var result = processes.Select(p =>
        {
            var processorName = processorUsers.TryGetValue(p.ProcessorId, out var procUser)
                ? (procUser.FullName ?? "Hợp tác xã")
                : "Hợp tác xã";

            var processorPhone = procUser?.Phone;
            var processorEmail = procUser?.Email;

            return new FarmerProductionProcessDto(
                p.Id,
                p.ProcessorId,
                processorName,
                processorPhone,
                processorEmail,
                p.Name,
                p.Description,
                p.CreatedAt.ToString("yyyy-MM-dd"),
                p.Steps.OrderBy(s => s.OrderIndex).Select(s => new ProcessStepDto(
                    s.Id,
                    s.Stage.ToString(),
                    s.StepName,
                    s.OrderIndex,
                    s.Description
                )).ToList()
            );
        }).ToList();

        return Ok(result);
    }
}

public record FarmerProductionProcessDto(
    Guid Id,
    Guid ProcessorId,
    string ProcessorName,
    string? ProcessorPhone,
    string? ProcessorEmail,
    string Name,
    string? Description,
    string CreatedAt,
    List<ProcessStepDto> Steps
);
