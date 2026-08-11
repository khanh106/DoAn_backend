using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Batches.Batches.Queries;

public class GetBatchesQueryHandler : IRequestHandler<GetBatchesQuery, IReadOnlyList<BatchDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetBatchesQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<BatchDto>> Handle(GetBatchesQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var userId = _currentUser.UserId.Value;
        var batches = await _uow.Batches.GetByProcessorIdAsync(userId, ct);

        return batches.Select(batch =>
        {
            var workers = batch.BatchWorkers.Select(w => w.User).Where(u => u != null).ToList();
            var repName = batch.RepresentativeWorker?.FullName
                ?? workers.FirstOrDefault(w => w.Id == batch.RepresentativeWorkerId)?.FullName;

            var workerDtos = batch.BatchWorkers
                .OrderBy(w => w.AssignedDate)
                .Select(bw => new BatchWorkerDto(
                    UserId: bw.UserId,
                    FullName: bw.User?.FullName ?? string.Empty,
                    WalletAddress: bw.User?.WalletAddress,
                    IsRepresentative: bw.IsRepresentative,
                    AssignedDate: bw.AssignedDate,
                    Status: bw.Status.ToString()))
                .ToList();

            return new BatchDto(
                Id: batch.Id,
                BatchCode: batch.BatchCode,
                FruitTypeId: batch.FruitTypeId,
                FruitTypeName: batch.FruitType?.Name ?? string.Empty,
                ProductId: batch.ProductId,
                ProductName: batch.Product?.Name ?? string.Empty,
                FarmAreaId: batch.FarmAreaId,
                FarmAreaName: batch.FarmArea?.Name ?? string.Empty,
                PlantingDate: batch.PlantingDate,
                ExpectedQuantity: batch.ExpectedQuantity,
                RepresentativeWorkerId: batch.RepresentativeWorkerId,
                RepresentativeWorkerName: repName,
                CurrentStage: batch.CurrentStage.ToString(),
                MetadataURI: batch.MetadataURI,
                DataHash: batch.DataHash,
                BlockchainBatchId: batch.BlockchainBatchId,
                ProcessorId: batch.ProcessorId,
                ProcessorName: batch.Processor?.FullName ?? string.Empty,
                CreatedAt: batch.CreatedAt,
                UpdatedAt: batch.UpdatedAt,
                Workers: workerDtos);
        }).ToList();
    }
}
