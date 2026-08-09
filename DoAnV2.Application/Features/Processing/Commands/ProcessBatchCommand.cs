using DoAnV2.Application.Features.Processing.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace DoAnV2.Application.Features.Processing.Commands;

/// <summary>
/// TASK 07 - Mục 7.1: Processor ghi nhận công đoạn Sơ chế (gọi SC processBatch).
/// BR-12: Chỉ sơ chế lô đang ở STAGE_RECEIVED.
/// </summary>
public record ProcessBatchCommand(
    Guid BatchId,
    string ProcessType,
    string Description,
    DateTime StartDate,
    DateTime? EndDate,
    IReadOnlyList<IFormFile> Images) : IRequest<ProcessBatchResponseDto>;
