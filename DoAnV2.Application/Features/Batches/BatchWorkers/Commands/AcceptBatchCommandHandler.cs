using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Batches.BatchWorkers.Commands;

/// <summary>
/// TASK 05 - Mục 5.3: Công nhân xác nhận nhận lô (PUT /farmer/batches/{id}/accept).
///   1. Verify Worker hiện tại có trong BatchWorker của batch.
///   2. Cập nhật Status = ACCEPTED trong DB.
///   3. Decrypt EncryptedPrivateKey bằng AES ➔ Worker tự ký SC acceptBatch(batchId).
///   4. Trả về BatchWorkerAcceptedDto (kèm TxHash).
///
/// BR-03: Worker chỉ thao tác trên lô được phân công.
/// BR-46: Worker PHẢI có EncryptedPrivateKey (Custodial Wallet).
/// </summary>
public class AcceptBatchCommandHandler
    : IRequestHandler<AcceptBatchCommand, BatchWorkerAcceptedDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IBlockchainService _blockchain;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<AcceptBatchCommandHandler> _logger;

    public AcceptBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IBlockchainService blockchain,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<AcceptBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _blockchain = blockchain;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
        _logger = logger;
    }

    public async Task<BatchWorkerAcceptedDto> Handle(AcceptBatchCommand req, CancellationToken ct)
    {
        var userId = Guard.RequireFarmer(_currentUser);

        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        var bw = await _uow.BatchWorkers.GetAsync(req.BatchId, userId, ct)
            ?? throw new ForbiddenException("Bạn không được phân công vào Batch này.");

        if (bw.Status == WorkerAssignmentStatus.ACCEPTED)
            throw new ValidationException("Bạn đã xác nhận nhận lô này rồi.");

        var worker = bw.User;

        if (string.IsNullOrWhiteSpace(worker.EncryptedPrivateKey))
            throw new ValidationException(
                "Tài khoản của bạn chưa có Custodial Wallet - không thể ký giao dịch on-chain.");

        // Giải mã private key
        var workerPrivateKey = _walletService.DecryptPrivateKey(
            worker.EncryptedPrivateKey, _walletOptions.EncryptionKey);

        // Gọi SC acceptBatch(batchId) - Worker tự ký
        string? txHash;
        try
        {
            txHash = await _blockchain.AcceptBatchAsync(
                batchId: batch.Id.ToString(),
                workerPrivateKey: workerPrivateKey,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "acceptBatch on-chain thất bại cho worker {UserId}.", userId);
            throw;
        }

        // Cập nhật status DB (chỉ khi SC thành công)
        bw.Status = WorkerAssignmentStatus.ACCEPTED;
        _uow.BatchWorkers.Update(bw);
        await _uow.SaveChangesAsync(ct);

        return new BatchWorkerAcceptedDto(
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            Status: bw.Status.ToString(),
            TransactionHash: txHash);
    }
}
