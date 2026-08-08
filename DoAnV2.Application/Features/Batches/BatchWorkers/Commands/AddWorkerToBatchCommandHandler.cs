using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Batches.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Batches.BatchWorkers.Commands;

/// <summary>
/// TASK 05 - Mục 5.2: Thêm 1 worker vào batch (Processor).
///   1. Validate: Processor sở hữu batch, worker là FARMER+APPROVED, chưa tồn tại trong batch.
///   2. Tạo BatchWorker (PENDING, không phải đại diện - BR-06).
///   3. Gọi SC assignWorker (nếu worker có WalletAddress).
///   4. Trả về BatchDto mới.
/// </summary>
public class AddWorkerToBatchCommandHandler
    : IRequestHandler<AddWorkerToBatchCommand, BatchDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<AddWorkerToBatchCommandHandler> _logger;

    public AddWorkerToBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IBlockchainService blockchain,
        ILogger<AddWorkerToBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<BatchDto> Handle(AddWorkerToBatchCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        var batch = await _uow.Batches.GetByIdWithWorkersAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền sửa Batch của Processor khác.");

        if (await _uow.BatchWorkers.ExistsAsync(req.BatchId, req.UserId, ct))
            throw new ConflictException("Worker này đã có trong Batch.");

        var workers = await _uow.Users.GetByIdsAsync(new[] { req.UserId }, ct);
        if (workers.Count == 0)
            throw new NotFoundException($"Không tìm thấy User {req.UserId}.");

        var worker = workers[0];
        if (worker.Role?.RoleName != RoleType.FARMER)
            throw new ValidationException(
                $"User '{worker.FullName}' không phải FARMER - không thể gán vào Batch.");
        if (worker.Status != UserStatus.APPROVED)
            throw new ValidationException(
                $"User '{worker.FullName}' chưa được Admin duyệt (Status={worker.Status}).");

        var bw = new BatchWorker
        {
            BatchId = batch.Id,
            UserId = worker.Id,
            IsRepresentative = false,
            AssignedDate = DateTime.UtcNow,
            Status = WorkerAssignmentStatus.PENDING,
        };
        await _uow.BatchWorkers.AddAsync(bw, ct);
        await _uow.SaveChangesAsync(ct);

        // Gọi SC assignWorker (BR-46: skip nếu worker chưa có ví)
        if (!string.IsNullOrWhiteSpace(worker.WalletAddress))
        {
            try
            {
                await _blockchain.AssignWorkerAsync(
                    batchId: batch.Id.ToString(),
                    workerAddress: worker.WalletAddress,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "assignWorker on-chain thất bại khi thêm worker {UserId} vào Batch {BatchId}.",
                    worker.Id, batch.Id);
            }
        }
        else
        {
            _logger.LogWarning(
                "Worker {UserId} chưa có WalletAddress - bỏ qua assignWorker on-chain.",
                worker.Id);
        }

        // Reload để trả DTO đầy đủ workers mới
        var reloaded = await _uow.Batches.GetByIdWithWorkersAsync(req.BatchId, ct)!;
        return await BuildDtoAsync(reloaded!, ct);
    }

    internal static async Task<BatchDto> BuildDtoAsync(Batch batch, CancellationToken ct)
    {
        var workerUsers = batch.BatchWorkers.Select(w => w.User).Where(u => u != null).ToList();
        var repName = batch.RepresentativeWorker?.FullName
            ?? workerUsers.FirstOrDefault(w => w.Id == batch.RepresentativeWorkerId)?.FullName;

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
