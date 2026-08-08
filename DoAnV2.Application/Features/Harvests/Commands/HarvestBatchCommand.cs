using DoAnV2.Application.Features.Harvests.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Harvests.Commands;

/// <summary>
/// TASK 06 - Mục 6.2: Farmer/Representative xác nhận thu hoạch lô.
/// </summary>
public record HarvestBatchCommand(
    Guid BatchId,
    DateTime HarvestDate,
    double Quantity,
    string Unit,
    string InitialQuality,
    string? Notes) : IRequest<HarvestBatchResponseDto>;