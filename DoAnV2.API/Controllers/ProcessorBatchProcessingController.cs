using DoAnV2.Application.Features.Processing.Commands;
using DoAnV2.Application.Features.Processing.Dtos;
using DoAnV2.Application.Features.Processing.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 07: API Sơ chế + Phân loại + Tách lô cho Processor.
///   - POST /api/v1/processor/batches/{batchId}/process        (Mục 7.1)
///   - POST /api/v1/processor/batches/{batchId}/classify-only  (Mục 7.2)
///   - POST /api/v1/processor/batches/{batchId}/split          (Mục 7.3)
///   - GET  /api/v1/processor/batches/{batchId}/processings    (Lịch sử sơ chế)
///
/// BR-12: Phải qua Sơ chế trước khi Phân loại/Tách lô.
/// BR-13: Tổng sản lượng SubBatch không vượt quá sản lượng lô gốc.
/// BR-16: SubBatch liên kết ParentBatchId.
/// </summary>
[ApiController]
[Route("api/v1/processor/batches/{batchId:guid}")]
[Authorize(Policy = "RequireProcessor")]
public class ProcessorBatchProcessingController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProcessorBatchProcessingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/v1/processor/batches/{batchId}/process
    /// Mục 7.1: Processor ghi nhận công đoạn Sơ chế (Rửa, Làm sạch, Làm khô...).
    /// Body multipart/form-data:
    ///   - ProcessType: string (form field)
    ///   - Description: string (form field)
    ///   - StartDate:   DateTime (form field)
    ///   - EndDate:     DateTime? (form field, optional)
    ///   - Images:      IFormFile[] (form files, optional)
    /// </summary>
    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProcessBatchResponseDto>> Process(
        [FromRoute] Guid batchId,
        [FromForm] string ProcessType,
        [FromForm] string Description,
        [FromForm] DateTime StartDate,
        [FromForm] DateTime? EndDate,
        [FromForm] List<IFormFile>? Images,
        CancellationToken ct)
    {
        var cmd = new ProcessBatchCommand(
            BatchId: batchId,
            ProcessType: ProcessType,
            Description: Description,
            StartDate: StartDate,
            EndDate: EndDate,
            Images: (IReadOnlyList<IFormFile>?)Images ?? Array.Empty<IFormFile>());

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/processor/batches/{batchId}/classify-only
    /// Mục 7.2: Processor phân loại KHÔNG tách lô.
    /// </summary>
    [HttpPost("classify-only")]
    public async Task<ActionResult<ClassifyOnlyResponseDto>> ClassifyOnly(
        [FromRoute] Guid batchId,
        [FromBody] ClassifyOnlyRequest body,
        CancellationToken ct)
    {
        var gradeDetails = body.GradeDetails
            .Select(g => new GradeDetailDto(g.Grade, g.Quantity, g.Note))
            .ToList();

        var cmd = new ClassifyOnlyBatchCommand(
            BatchId: batchId,
            ClassificationNote: body.ClassificationNote,
            GradeDetails: gradeDetails);

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/processor/batches/{batchId}/split
    /// Mục 7.3: Processor phân loại CÓ tách lô con.
    /// </summary>
    [HttpPost("split")]
    public async Task<ActionResult<SplitBatchResponseDto>> Split(
        [FromRoute] Guid batchId,
        [FromBody] SplitBatchRequest body,
        CancellationToken ct)
    {
        var subBatches = body.SubBatches
            .Select(s => new SubBatchInput(s.SubBatchCode, s.Classification, s.Quantity))
            .ToList();

        var cmd = new SplitBatchCommand(
            BatchId: batchId,
            SubBatches: subBatches);

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/batches/{batchId}/processings
    /// Lịch sử các lần sơ chế của lô.
    /// </summary>
    [HttpGet("processings")]
    public async Task<ActionResult<IReadOnlyList<ProcessingHistoryDto>>> GetProcessings(
        [FromRoute] Guid batchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProcessingsByBatchQuery(batchId), ct);
        return Ok(result);
    }
}

// ====================== REQUEST BODIES ======================

/// <summary>Body cho POST /classify-only.</summary>
public record ClassifyOnlyRequest(
    string ClassificationNote,
    IReadOnlyList<GradeDetailRequest> GradeDetails);

/// <summary>1 grade trong body classify-only.</summary>
public record GradeDetailRequest(
    string Grade,
    double Quantity,
    string? Note);

/// <summary>Body cho POST /split.</summary>
public record SplitBatchRequest(
    IReadOnlyList<SubBatchRequest> SubBatches);

/// <summary>1 SubBatch trong body split.</summary>
public record SubBatchRequest(
    string SubBatchCode,
    string Classification,
    double Quantity);
