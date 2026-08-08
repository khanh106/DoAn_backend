using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Batches.Batches.Queries;

/// <summary>
/// Lấy chi tiết 1 Batch (kèm danh sách worker).
///   - PROCESSOR: chỉ xem được Batch do chính mình tạo.
///   - FARMER: chỉ xem được Batch mà mình được gán (BR-03).
///   - ADMIN: xem tất cả.
/// </summary>
public class GetBatchByIdQueryHandler : IRequestHandler<GetBatchByIdQuery, BatchDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetBatchByIdQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<BatchDto?> Handle(GetBatchByIdQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var batch = await _uow.Batches.GetByIdWithWorkersAsync(req.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.Id}.");

        var role = _currentUser.Role?.ToUpperInvariant();
        var userId = _currentUser.UserId.Value;

        if (role == "PROCESSOR")
        {
            if (batch.ProcessorId != userId)
                throw new ForbiddenException("Bạn không có quyền xem Batch của Processor khác.");
        }
        else if (role == "FARMER")
        {
            if (!batch.BatchWorkers.Any(w => w.UserId == userId))
                throw new ForbiddenException("Bạn không được phân công vào Batch này.");
        }
        else if (role != "ADMIN")
        {
            throw new ForbiddenException("Không có quyền truy cập.");
        }

        var workers = batch.BatchWorkers.Select(w => w.User).Where(u => u != null).ToList();
        var repName = batch.RepresentativeWorker?.FullName
            ?? workers.FirstOrDefault(w => w.Id == batch.RepresentativeWorkerId)?.FullName;

        var workerDtos = batch.BatchWorkers
            .OrderBy(w => w.AssignedDate)
            .Select(bw => new BatchWorkerDto(
                UserId: bw.UserId,
                FullName: bw.User.FullName,
                WalletAddress: bw.User.WalletAddress,
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
    }
}
