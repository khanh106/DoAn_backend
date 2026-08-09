using DoAnV2.Application.Features.Processing.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Processing.Commands;

/// <summary>
/// TASK 07 - Mục 7.2: Processor phân loại KHÔNG tách lô (gọi SC classifyOnlyBatch).
/// BR-12: Chỉ phân loại lô đang ở STAGE_PROCESSED.
/// </summary>
public record ClassifyOnlyBatchCommand(
    Guid BatchId,
    string ClassificationNote,
    IReadOnlyList<GradeDetailDto> GradeDetails) : IRequest<ClassifyOnlyResponseDto>;
