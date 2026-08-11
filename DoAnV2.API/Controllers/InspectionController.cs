using DoAnV2.Application.Features.Inspections.Commands;
using DoAnV2.Application.Features.Inspections.Dtos;
using DoAnV2.Application.Features.Inspections.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoAnV2.API.Controllers;

/// <summary>
/// TASK 08 - Mục 8.1: API Kiểm định chất lượng (Processor).
///   - POST /api/v1/processor/inspections/parent/{batchId}  (inspectParent)
///   - POST /api/v1/processor/inspections/sub/{subBatchId}  (inspectSub)
///   - GET  /api/v1/processor/inspections/parent/{batchId}/inspections
///   - GET  /api/v1/processor/inspections/sub/{subBatchId}/inspections
///
/// BR-14: Lô phải qua kiểm định SAU KHI phân loại (STAGE_SORTED).
/// BR-15: PASSED ➔ INSPECTION_PASSED; FAILED ➔ dừng quy trình, không đóng gói, không QR thương mại.
/// </summary>
[ApiController]
[Authorize(Policy = "RequireProcessor")]
public class InspectionController : ControllerBase
{
    private readonly IMediator _mediator;

    public InspectionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/v1/processor/inspections/parent/{batchId}
    /// Kiểm định Parent Batch. multipart/form-data:
    ///   - DocumentName, DocumentNumber, InspectionUnit, InspectionDate (form fields)
    ///   - Result ("PASSED" / "FAILED")
    ///   - Note (optional)
    ///   - CertificateFile (PDF/PNG)
    /// </summary>
    [HttpPost("api/v1/processor/inspections/parent/{batchId:guid}")]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<InspectionResponseDto>> InspectParent(
        [FromRoute] Guid batchId,
        [FromForm] string DocumentName,
        [FromForm] string DocumentNumber,
        [FromForm] string InspectionUnit,
        [FromForm] DateTime InspectionDate,
        [FromForm] string Result,
        [FromForm] string? Note,
        [FromForm] IFormFile CertificateFile,
        CancellationToken ct)
    {
        var cmd = new InspectParentCommand(
            BatchId: batchId,
            DocumentName: DocumentName,
            DocumentNumber: DocumentNumber,
            InspectionUnit: InspectionUnit,
            InspectionDate: InspectionDate,
            Result: Result,
            Note: Note,
            CertificateFile: CertificateFile);

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/processor/inspections/sub/{subBatchId}
    /// Kiểm định SubBatch.
    /// </summary>
    [HttpPost("api/v1/processor/inspections/sub/{subBatchId:guid}")]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<InspectionResponseDto>> InspectSub(
        [FromRoute] Guid subBatchId,
        [FromForm] string DocumentName,
        [FromForm] string DocumentNumber,
        [FromForm] string InspectionUnit,
        [FromForm] DateTime InspectionDate,
        [FromForm] string Result,
        [FromForm] string? Note,
        [FromForm] IFormFile CertificateFile,
        CancellationToken ct)
    {
        var cmd = new InspectSubCommand(
            SubBatchId: subBatchId,
            DocumentName: DocumentName,
            DocumentNumber: DocumentNumber,
            InspectionUnit: InspectionUnit,
            InspectionDate: InspectionDate,
            Result: Result,
            Note: Note,
            CertificateFile: CertificateFile);

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/inspections/parent/{batchId}/inspections
    /// Lịch sử kiểm định của 1 Parent Batch.
    /// </summary>
    [HttpGet("api/v1/processor/inspections/parent/{batchId:guid}/inspections")]
    public async Task<ActionResult<IReadOnlyList<InspectionHistoryDto>>> GetInspectionsByBatch(
        [FromRoute] Guid batchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInspectionsByBatchQuery(batchId), ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/processor/inspections/sub/{subBatchId}/inspections
    /// Lịch sử kiểm định của 1 SubBatch.
    /// </summary>
    [HttpGet("api/v1/processor/inspections/sub/{subBatchId:guid}/inspections")]
    public async Task<ActionResult<IReadOnlyList<InspectionHistoryDto>>> GetInspectionsBySubBatch(
        [FromRoute] Guid subBatchId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInspectionsBySubBatchQuery(subBatchId), ct);
        return Ok(result);
    }
}
