using DoAnV2.Application.Features.Harvests.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Harvests.Commands;

/// <summary>
/// TASK 06 - Mục 6.3: Processor tiếp nhận lô sau thu hoạch (gọi SC receiveBatch).
/// BR-11: Chỉ tiếp nhận lô đang ở STAGE_HARVESTED.
/// </summary>
public record ReceiveBatchCommand(
    Guid BatchId,
    DateTime ReceivedDate,
    double Quantity,
    string Unit,
    string DeliveryPerson,
    string ConditionNote) : IRequest<ReceiveBatchResponseDto>;