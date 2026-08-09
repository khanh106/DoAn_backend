using DoAnV2.Application.Features.Processing.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Processing.Commands;

/// <summary>
/// Một lô con trong yêu cầu tách lô (TASK 07 - Mục 7.3).
/// </summary>
public record SubBatchInput(
    string SubBatchCode,
    string Classification,
    double Quantity);

/// <summary>
/// TASK 07 - Mục 7.3: Processor phân loại CÓ tách lô (gọi SC splitBatch).
/// BR-12: Chỉ tách lô ở STAGE_PROCESSED.
/// BR-13: Tổng quantity SubBatch không vượt quá ExpectedQuantity của Parent.
/// BR-16: Mỗi SubBatch phải liên kết với ParentBatchId.
/// </summary>
public record SplitBatchCommand(
    Guid BatchId,
    IReadOnlyList<SubBatchInput> SubBatches) : IRequest<SplitBatchResponseDto>;
