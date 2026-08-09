using DoAnV2.Application.Features.Packagings.Commands;
using DoAnV2.Application.Features.Packagings.Dtos;
using DoAnV2.Application.Features.Packagings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 08 - Mục 8.2: API Đóng gói thương mại (Processor).
///   - POST /api/v1/processor/packagings/parent/{batchId}   (packageParent)
///   - POST /api/v1/processor/packagings/sub/{subBatchId}   (packageSub)
///   - GET  /api/v1/processor/packagings/parent/{batchId}/packagings
///   - GET  /api/v1/processor/packagings/sub/{subBatchId}/packagings
///
/// BR-14: Chỉ đóng gói khi đã kiểm định ĐẠT (INSPECTION_PASSED).
/// </summary>
[ApiController]
[Authorize(Policy = "RequireProcessor")]
public class PackagingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PackagingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/v1/processor/packagings/parent/{batchId}
    /// Đóng gói Parent Batch. multipart/form-data:
    ///   - Input (JSON string matching PackageInputDto): PackDate, Weight, Specification, UsageGuide, StorageGuide, Color, Smell, Standard, Note
    ///   - Images (file[]) optional
    /// </summary>
    [HttpPost("api/v1/processor/packagings/parent/{batchId:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PackagingResponseDto>> PackageParent(
        [FromRoute] Guid batchId,
        [FromForm] string Input,
        [FromForm] List<IFormFile>? Images,
        CancellationToken ct)
    {
        var inputDto = System.Text.Json.JsonSerializer.Deserialize<PackageInputDto>(
            Input,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new DoAnV2.Application.Common.Exceptions.ValidationException("Input JSON không hợp lệ.");

        var cmd = new PackageParentCommand(
            BatchId: batchId,
            Input: inputDto,
            Images: (IReadOnlyList<IFormFile>?)Images ?? Array.Empty<IFormFile>());

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/processor/packagings/sub/{subBatchId}
    /// Đóng gói SubBatch.
    /// </summary>
    [HttpPost("api/v1/processor/packagings/sub/{subBatchId:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PackagingResponseDto>> PackageSub(
        [FromRoute] Guid subBatchId,
        [FromForm] string Input,
        [FromForm] List<IFormFile>? Images,
        CancellationToken ct)
    {
        var inputDto = System.Text.Json.JsonSerializer.Deserialize<PackageInputDto>(
            Input,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new DoAnV2.Application.Common.Exceptions.ValidationException("Input JSON không hợp lệ.");

        var cmd = new PackageSubCommand(
            SubBatchId: subBatchId,
            Input: inputDto,
            Images: (IReadOnlyList<IFormFile>?)Images ?? Array.Empty<IFormFile>());

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/packagings/parent/{batchId}/packagings
    /// Lịch sử đóng gói của 1 Parent Batch.
    /// </summary>
    [HttpGet("api/v1/processor/packagings/parent/{batchId:guid}/packagings")]
    public async Task<ActionResult<IReadOnlyList<PackagingHistoryDto>>> GetPackagingsByBatch(
        [FromRoute] Guid batchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPackagingsByBatchQuery(batchId), ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/packagings/sub/{subBatchId}/packagings
    /// Lịch sử đóng gói của 1 SubBatch.
    /// </summary>
    [HttpGet("api/v1/processor/packagings/sub/{subBatchId:guid}/packagings")]
    public async Task<ActionResult<IReadOnlyList<PackagingHistoryDto>>> GetPackagingsBySubBatch(
        [FromRoute] Guid subBatchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPackagingsBySubBatchQuery(subBatchId), ct);
        return Ok(result);
    }
}
