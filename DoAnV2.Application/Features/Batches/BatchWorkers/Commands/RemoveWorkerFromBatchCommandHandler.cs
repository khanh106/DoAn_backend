using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Batches.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Batches.BatchWorkers.Commands;

/// <summary>
/// TASK 05 - Mục 5.2: Xóa 1 worker khỏi batch (Processor).
///   Không cho xóa Worker đang là Representative - phải đổi đại diện trư�c.
/// </summary>
public class RemoveWorkerFromBatchCommandHandler
    : IRequestHandler<RemoveWorkerFromBatchCommand, BatchDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RemoveWorkerFromBatchCommandHandler> _logger;

    public RemoveWorkerFromBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        ILogger<RemoveWorkerFromBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BatchDto> Handle(RemoveWorkerFromBatchCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        var batch = await _uow.Batches.GetByIdWithWorkersAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền sửa Batch của Processor khác.");

        var bw = await _uow.BatchWorkers.GetAsync(req.BatchId, req.UserId, ct)
            ?? throw new NotFoundException("Worker này không có trong Batch.");

        if (bw.IsRepresentative)
            throw new ValidationException(
                "Không thể xóa Worker đang là Người đại diện. Hãy đổi đại diện trước.");

        _uow.BatchWorkers.Remove(bw);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Removed worker {UserId} from Batch {BatchId}", req.UserId, req.BatchId);

        var reloaded = await _uow.Batches.GetByIdWithWorkersAsync(req.BatchId, ct)!;
        return await AddWorkerToBatchCommandHandler.BuildDtoAsync(reloaded!, ct);
    }
}
