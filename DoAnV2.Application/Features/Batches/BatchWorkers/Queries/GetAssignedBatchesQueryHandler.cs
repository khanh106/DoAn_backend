using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Batches.BatchWorkers.Queries;

/// <summary>
/// TASK 05 - Mục 5.2: GET /api/v1/farmer/batches/assigned
/// Danh sách các lô được phân công cho Farmer hiện tại đang đăng nhập.
/// </summary>
public class GetAssignedBatchesQueryHandler
    : IRequestHandler<GetAssignedBatchesQuery, IReadOnlyList<AssignedBatchDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetAssignedBatchesQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AssignedBatchDto>> Handle(
        GetAssignedBatchesQuery req, CancellationToken ct)
    {
        var userId = Guard.RequireFarmer(_currentUser);
        var list = await _uow.BatchWorkers.GetByUserIdAsync(userId, status: null, ct: ct);

        return list.Select(bw => new AssignedBatchDto(
            BatchId: bw.BatchId,
            BatchCode: bw.Batch.BatchCode,
            FruitTypeName: bw.Batch.FruitType?.Name ?? string.Empty,
            ProductName: bw.Batch.Product?.Name ?? string.Empty,
            FarmAreaName: bw.Batch.FarmArea?.Name ?? string.Empty,
            CurrentStage: bw.Batch.CurrentStage.ToString(),
            PlantingDate: bw.Batch.PlantingDate,
            ExpectedQuantity: bw.Batch.ExpectedQuantity,
            IsRepresentative: bw.IsRepresentative,
            AssignedDate: bw.AssignedDate,
            WorkerStatus: bw.Status.ToString()
        )).ToList();
    }
}
