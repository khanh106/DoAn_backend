using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Batches.Batches.Commands;

/// <summary>
/// TASK 05 - Mục 5.1: API Tạo Lô sản xuất.
///   1. Kiểm tra BatchCode duy nhất (BR-01).
///   2. Tạo bản ghi Batch (CurrentStage = STAGE_PLANTING).
///   3. Tạo danh sách BatchWorker (IsRepresentative=true cho representative).
///   4. Upload Metadata JSON lên IPFS ➔ (MetadataURI, DataHash).
///   5. Gọi SC: createBatch.
///   6. Với mỗi worker: gọi SC assignWorker.
///   7. Với người đại diện: gọi SC setRepresentative.
///   8. Lưu BlockchainTransaction (đã làm trong BlockchainService).
///
/// BR-06: Bắt buộc 1 đại diện (IsRepresentative=true).
/// BR-46: Worker chưa có WalletAddress ➔ vẫn assign vào DB, SC assignWorker bị bỏ qua (ghi log warning).
/// </summary>
public class CreateBatchCommandHandler : IRequestHandler<CreateBatchCommand, BatchDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<CreateBatchCommandHandler> _logger;

    public CreateBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        IOptions<WalletOptions> walletOptions,
        ILogger<CreateBatchCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _walletOptions = walletOptions.Value;
        _logger = logger;
    }

    public async Task<BatchDto> Handle(CreateBatchCommand req, CancellationToken ct)
    {
        // ========== 1. Validate Processor ==========
        var processorId = Guard.RequireProcessor(_currentUser);

        if (string.IsNullOrWhiteSpace(req.BatchCode))
            throw new ValidationException("BatchCode không được trống.");
        if (req.ExpectedQuantity <= 0)
            throw new ValidationException("ExpectedQuantity phải > 0.");
        if (req.AssignedWorkerIds is null || req.AssignedWorkerIds.Count == 0)
            throw new ValidationException("Phải chỉ định ít nhất 1 công nhân.");
        if (!req.AssignedWorkerIds.Contains(req.RepresentativeWorkerId))
            throw new ValidationException(
                "RepresentativeWorkerId phải nằm trong danh sách AssignedWorkerIds.");

        var batchCode = req.BatchCode.Trim();

        // ========== 2. BR-01: BatchCode Unique ==========
        if (await _uow.Batches.BatchCodeExistsAsync(batchCode, ct))
            throw new ConflictException($"BatchCode '{batchCode}' đã tồn tại.");

        // ========== 3. Validate FKs (FruitType/Product/FarmArea thuộc Processor) ==========
        var fruitType = await _uow.FruitTypes.GetByIdAsync(req.FruitTypeId, ct)
            ?? throw new NotFoundException($"Không tìm thấy FruitType {req.FruitTypeId}.");
        if (fruitType.ProcessorId != processorId)
            throw new ForbiddenException("FruitType này không thuộc Processor của bạn.");

        var product = await _uow.Products.GetByIdAsync(req.ProductId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Product {req.ProductId}.");
        if (product.FruitTypeId != fruitType.Id)
            throw new ValidationException("Product không thuộc FruitType đã chọn.");

        var farmArea = await _uow.FarmAreas.GetByIdAsync(req.FarmAreaId, ct)
            ?? throw new NotFoundException($"Không tìm thấy FarmArea {req.FarmAreaId}.");
        if (farmArea.ProcessorId != processorId)
            throw new ForbiddenException("FarmArea này không thuộc Processor của bạn.");

        // ========== 4. Validate Workers (BR-03 + phải là FARMER + APPROVED) ==========
        var workers = await _uow.Users.GetByIdsAsync(req.AssignedWorkerIds.Distinct(), ct);
        if (workers.Count != req.AssignedWorkerIds.Distinct().Count())
            throw new NotFoundException("Một hoặc nhiều WorkerId không tồn tại.");

        foreach (var w in workers)
        {
            if (w.Role?.RoleName != RoleType.FARMER)
                throw new ValidationException(
                    $"User '{w.FullName}' không phải FARMER - không thể gán vào Batch.");
            if (w.Status != UserStatus.APPROVED)
                throw new ValidationException(
                    $"User '{w.FullName}' chưa được Admin duyệt (Status={w.Status}).");
        }

        // ========== 5. Tạo Batch entity (chưa save - để gom transaction) ==========
        var batch = new Batch
        {
            BatchCode = batchCode,
            FruitTypeId = fruitType.Id,
            ProductId = product.Id,
            FarmAreaId = farmArea.Id,
            PlantingDate = req.PlantingDate,
            ExpectedQuantity = req.ExpectedQuantity,
            RepresentativeWorkerId = req.RepresentativeWorkerId,
            ProcessorId = processorId,
            CurrentStage = BatchStage.STAGE_PLANTING,
        };

        // ========== 6. Tạo danh sách BatchWorker ==========
        var now = DateTime.UtcNow;
        foreach (var uid in req.AssignedWorkerIds.Distinct())
        {
            batch.BatchWorkers.Add(new BatchWorker
            {
                BatchId = batch.Id,
                UserId = uid,
                IsRepresentative = uid == req.RepresentativeWorkerId,
                AssignedDate = now,
                Status = WorkerAssignmentStatus.PENDING,
            });
        }

        await _uow.Batches.AddAsync(batch, ct);
        await _uow.SaveChangesAsync(ct); // Save trước để có batch.Id

        // ========== 7. Upload Metadata JSON lên IPFS ==========
        var metadata = new
        {
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            fruitType = fruitType.Name,
            fruitTypeCode = fruitType.Code,
            product = product.Name,
            productVariety = product.Variety,
            farmArea = farmArea.Name,
            farmProvince = farmArea.Province,
            plantingDate = batch.PlantingDate,
            expectedQuantity = batch.ExpectedQuantity,
            representativeWorkerId = batch.RepresentativeWorkerId,
            workers = workers.Select(w => new
            {
                userId = w.Id,
                fullName = w.FullName,
                isRepresentative = w.Id == batch.RepresentativeWorkerId,
            }),
            createdAt = batch.CreatedAt,
        };

        var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
            metadata,
            fileName: $"batch-{batch.BatchCode}-metadata.json",
            ct: ct);

        batch.MetadataURI = metadataURI;
        batch.DataHash = dataHash;
        _uow.Batches.Update(batch);

        // ========== 8. Gọi Smart Contract: createBatch ==========
        // �️ batchId truyền cho SC là Guid dạng string - BlockchainService đã tự keccak256(bytes32).
        var createTxHash = await _blockchain.CreateBatchAsync(
            batchId: batch.Id.ToString(),
            batchCode: batch.BatchCode,
            fruitType: fruitType.Code,
            metadataURI: metadataURI,
            dataHash: dataHash,
            ct: ct);

        _logger.LogInformation(
            "Batch {BatchCode} created on-chain. TxHash={TxHash}", batch.BatchCode, createTxHash);

        // ========== 9. Với từng worker: assignWorker + setRepresentative ==========
        var repUser = workers.First(w => w.Id == req.RepresentativeWorkerId);

        foreach (var w in workers)
        {
            if (string.IsNullOrWhiteSpace(w.WalletAddress))
            {
                _logger.LogWarning(
                    "Worker {UserId} chưa có WalletAddress - bỏ qua assignWorker on-chain.",
                    w.Id);
                continue;
            }

            try
            {
                await _blockchain.AssignWorkerAsync(
                    batchId: batch.Id.ToString(),
                    workerAddress: w.WalletAddress,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "assignWorker on-chain thất bại cho worker {UserId}.", w.Id);
                // BR-42: Không rollback DB - lỗi đã ghi vào BlockchainTransaction.
            }
        }

        // setRepresentative (chỉ 1 lần)
        if (!string.IsNullOrWhiteSpace(repUser.WalletAddress))
        {
            try
            {
                await _blockchain.SetRepresentativeAsync(
                    batchId: batch.Id.ToString(),
                    repAddress: repUser.WalletAddress,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "setRepresentative on-chain thất bại cho rep {UserId}.", repUser.Id);
            }
        }

        await _uow.SaveChangesAsync(ct);

        // ========== 10. Trả về DTO ==========
        return MapToDto(batch, workers, fruitType.Name, product.Name,
            farmArea.Name, repUser.FullName);
    }

    internal static BatchDto MapToDto(
        Batch batch,
        IReadOnlyList<User> workers,
        string fruitTypeName,
        string productName,
        string farmAreaName,
        string? representativeWorkerName)
    {
        var repName = representativeWorkerName
            ?? workers.FirstOrDefault(w => w.Id == batch.RepresentativeWorkerId)?.FullName;

        var workerDtos = batch.BatchWorkers.Select(bw =>
        {
            var u = workers.First(w => w.Id == bw.UserId);
            return new BatchWorkerDto(
                UserId: u.Id,
                FullName: u.FullName,
                WalletAddress: u.WalletAddress,
                IsRepresentative: bw.IsRepresentative,
                AssignedDate: bw.AssignedDate,
                Status: bw.Status.ToString());
        }).ToList();

        return new BatchDto(
            Id: batch.Id,
            BatchCode: batch.BatchCode,
            FruitTypeId: batch.FruitTypeId,
            FruitTypeName: fruitTypeName,
            ProductId: batch.ProductId,
            ProductName: productName,
            FarmAreaId: batch.FarmAreaId,
            FarmAreaName: farmAreaName,
            PlantingDate: batch.PlantingDate,
            ExpectedQuantity: batch.ExpectedQuantity,
            RepresentativeWorkerId: batch.RepresentativeWorkerId,
            RepresentativeWorkerName: repName,
            CurrentStage: batch.CurrentStage.ToString(),
            MetadataURI: batch.MetadataURI,
            DataHash: batch.DataHash,
            BlockchainBatchId: batch.BlockchainBatchId,
            ProcessorId: batch.ProcessorId,
            ProcessorName: string.Empty,
            CreatedAt: batch.CreatedAt,
            UpdatedAt: batch.UpdatedAt,
            Workers: workerDtos);
    }
}
