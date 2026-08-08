using DoAnV2.Application.Features.CultivationLogs.Commands;
using DoAnV2.Application.Features.CultivationLogs.Dtos;
using DoAnV2.Application.Features.CultivationLogs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 06 - Mục 6.1: API Nhật ký canh tác (Off-chain SQL Server + IPFS ảnh).
/// BR-07, BR-08: Tuyệt đối KHÔNG gọi Smart Contract cho mỗi dòng nhật ký.
/// </summary>
[ApiController]
[Route("api/v1/farmer/batches/{batchId:guid}/logs")]
[Authorize(Policy = "RequireFarmer")]
public class CultivationLogController : ControllerBase
{
    private readonly IMediator _mediator;

    public CultivationLogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/v1/farmer/batches/{batchId}/logs
    /// Form-data: ActivityType, Description, LogDate, Images[] (tệp ảnh).
    /// Worker phải được phân công vào batch (BR-03).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)] // 50 MB tổng
    public async Task<ActionResult<CultivationLogDto>> Create(
        [FromRoute] Guid batchId,
        [FromForm] CreateCultivationLogForm form,
        CancellationToken ct)
    {
        var cmd = new CreateCultivationLogCommand(
            BatchId: batchId,
            ActivityType: form.ActivityType,
            Description: form.Description,
            LogDate: form.LogDate,
            Images: form.Images ?? new List<IFormFile>());

        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetByBatch), new { batchId }, result);
    }

    /// <summary>
    /// GET /api/v1/farmer/batches/{batchId}/logs
    /// Xem danh sách nhật ký canh tác của 1 lô.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CultivationLogDto>>> GetByBatch(
        [FromRoute] Guid batchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCultivationLogsByBatchQuery(batchId), ct);
        return Ok(result);
    }
}

/// <summary>Form-data cho việc tạo CultivationLog.</summary>
public class CreateCultivationLogForm
{
    public string ActivityType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime LogDate { get; set; }
    public List<IFormFile>? Images { get; set; }
}