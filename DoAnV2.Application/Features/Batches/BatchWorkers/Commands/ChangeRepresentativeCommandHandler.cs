using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Batches.BatchWorkers.Commands;

/// <summary>
/// TASK 05 - Mục 5.2: Đổi Người đại diện (Representative).
///   - Người đại diện MỚI phải đã có trong BatchWorker (BR-06).
///   - Set IsRepresentative=false cho đại diện cũ, true cho đại diện mới.
///   - Cập nhật Batch.RepresentativeWorkerId.
///   - Gọi SC setRepresentative (nếu user mới có WalletAddress).
/// </summary>
public class ChangeRepresentativeCommandHandler
    : IRequestHandler<ChangeRepresentativeCommand, BatchDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<ChangeRepresentativeCommandHandler> _logger;

    public ChangeRepresentativeCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IBlockchainService blockchain,
        ILogger<ChangeRepresentativeCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<BatchDto> Handle(ChangeRepresentativeCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        var batch = await _uow.Batches.GetByIdWithWorkersAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền sửa Batch của Processor khác.");

        if (batch.RepresentativeWorkerId == req.NewRepresentativeWorkerId)
            throw new ValidationException("Worker mới trùng với đại diện hiện tại.");

        var newRepBw = await _uow.BatchWorkers.GetAsync(req.BatchId, req.NewRepresentativeWorkerId, ct)
            ?? throw new NotFoundException(
                $"Worker {req.NewRepresentativeWorkerId} không có trong Batch. Hãy thêm vào trước.");

        // Cập nhật các IsRepresentative
        var oldRepId = batch.RepresentativeWorkerId;
        if (oldRepId.HasValue)
        {
            var oldBw = await _uow.BatchWorkers.GetAsync(req.BatchId, oldRepId.Value, ct);
            if (oldBw != null)
            {
                oldBw.IsRepresentative = false;
                _uow.BatchWorkers.Update(oldBw);
            }
        }

        newRepBw.IsRepresentative = true;
        _uow.BatchWorkers.Update(newRepBw);

        batch.RepresentativeWorkerId = req.NewRepresentativeWorkerId;
        _uow.Batches.Update(batch);

        await _uow.SaveChangesAsync(ct);

        // Gọi SC setRepresentative (BR-46: skip nếu chưa có ví)
        if (!string.IsNullOrWhiteSpace(newRepBw.User?.WalletAddress))
        {
            try
            {
                await _blockchain.SetRepresentativeAsync(
                    batchId: batch.Id.ToString(),
                    repAddress: newRepBw.User.WalletAddress,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "setRepresentative on-chain thất bại khi đổi đại diện Batch {BatchId}.",
                    batch.Id);
            }
        }
        else
        {
            _logger.LogWarning(
                "New representative {UserId} chưa có WalletAddress - bỏ qua setRepresentative on-chain.",
                req.NewRepresentativeWorkerId);
        }

        var reloaded = await _uow.Batches.GetByIdWithWorkersAsync(req.BatchId, ct)!;
        return await AddWorkerToBatchCommandHandler.BuildDtoAsync(reloaded!, ct);
    }
}
