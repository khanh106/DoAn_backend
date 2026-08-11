using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using DoAnV2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.API.Controllers;

/// <summary>
/// API Quản lý Quy trình sản xuất & các công đoạn (ProductionProcess & ProcessStep) cho Hợp tác xã/Cơ sở chế biến.
///   - GET  /api/v1/processor/processes - Lấy danh sách quy trình
///   - POST /api/v1/processor/processes - Tạo mới mẫu quy trình kèm công đoạn
/// </summary>
[ApiController]
[Route("api/v1/processor/processes")]
[Authorize(Policy = "RequireProcessor")]
public class ProductionProcessController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ProductionProcessController(ApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// GET /api/v1/processor/processes - Lấy danh sách quy trình sản xuất của Processor.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductionProcessDto>>> GetProcesses(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        var processes = await _dbContext.ProductionProcesses
            .Include(p => p.Steps)
            .Where(p => p.ProcessorId == userId.Value)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductionProcessDto(
                p.Id,
                p.ProcessorId,
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
            ))
            .ToListAsync(ct);

        return Ok(processes);
    }

    /// <summary>
    /// POST /api/v1/processor/processes - Tạo mới mẫu quy trình sản xuất.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductionProcessDto>> CreateProcess(
        [FromBody] CreateProductionProcessRequest request,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Tên quy trình không được để trống." });

        var process = new ProductionProcess
        {
            ProcessorId = userId.Value,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        if (request.Steps != null && request.Steps.Count > 0)
        {
            foreach (var stepReq in request.Steps)
            {
                if (!Enum.TryParse<BatchStage>(stepReq.Stage, true, out var parsedStage))
                {
                    parsedStage = BatchStage.STAGE_PROCESSED;
                }

                process.Steps.Add(new ProcessStep
                {
                    Stage = parsedStage,
                    StepName = stepReq.StepName.Trim(),
                    OrderIndex = stepReq.OrderIndex,
                    Description = stepReq.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _dbContext.ProductionProcesses.Add(process);
        await _dbContext.SaveChangesAsync(ct);

        var result = new ProductionProcessDto(
            process.Id,
            process.ProcessorId,
            process.Name,
            process.Description,
            process.CreatedAt.ToString("yyyy-MM-dd"),
            process.Steps.OrderBy(s => s.OrderIndex).Select(s => new ProcessStepDto(
                s.Id,
                s.Stage.ToString(),
                s.StepName,
                s.OrderIndex,
                s.Description
            )).ToList()
        );

        return CreatedAtAction(nameof(GetProcesses), new { id = process.Id }, result);
    }
}

// ====================== DTOs & REQUEST BODIES ======================

public record ProcessStepDto(
    Guid Id,
    string Stage,
    string StepName,
    int OrderIndex,
    string? Description
);

public record ProductionProcessDto(
    Guid Id,
    Guid ProcessorId,
    string Name,
    string? Description,
    string CreatedAt,
    List<ProcessStepDto> Steps
);

public record CreateProductionProcessRequest(
    string Name,
    string? Description,
    List<CreateProcessStepRequest>? Steps
);

public record CreateProcessStepRequest(
    string Stage,
    string StepName,
    int OrderIndex,
    string? Description
);
