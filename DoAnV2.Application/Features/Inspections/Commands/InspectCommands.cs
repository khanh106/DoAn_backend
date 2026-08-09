using DoAnV2.Application.Features.Inspections.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace DoAnV2.Application.Features.Inspections.Commands;

/// <summary>
/// TASK 08 - Mục 8.1: Processor ghi nhận Kiểm định chất lượng cho Parent Batch.
/// BR-14: Lô phải qua kiểm định SAU KHI phân loại (STAGE_SORTED).
/// BR-15: PASSED → INSPECTION_PASSED, FAILED → dừng quy trình.
/// </summary>
public record InspectParentCommand(
    Guid BatchId,
    string DocumentName,
    string DocumentNumber,
    string InspectionUnit,
    DateTime InspectionDate,
    string Result,                // PASSED / FAILED
    string? Note,
    IFormFile CertificateFile) : IRequest<InspectionResponseDto>;

/// <summary>
/// TASK 08 - Mục 8.1: Processor ghi nhận Kiểm định chất lượng cho SubBatch.
/// BR-14 + BR-16: SubBatch liên kết ParentBatchId, chỉ kiểm định khi ở STAGE_SORTED.
/// </summary>
public record InspectSubCommand(
    Guid SubBatchId,
    string DocumentName,
    string DocumentNumber,
    string InspectionUnit,
    DateTime InspectionDate,
    string Result,
    string? Note,
    IFormFile CertificateFile) : IRequest<InspectionResponseDto>;
