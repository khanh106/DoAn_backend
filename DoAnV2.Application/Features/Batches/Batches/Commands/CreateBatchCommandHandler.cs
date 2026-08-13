using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Common.Queues;
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
///   2. Tạo bản ghi Batch (CurrentStage = STAGE_PLANTING, BlockchainSyncStatus = PENDING).
///   3. Tạo danh sách BatchWorker (IsRepresentative=true cho representative).
///   4. Upload Metadata JSON lên IPFS ➔ (MetadataURI, DataHash).
///   5. Enqueue blockchain job (createBatch + assignWorker + setRepresentative chạy nền).
///
/// BR-06: Bắt buộc 1 đại diện (IsRepresentative=true).
/// BR-46: Worker chưa có WalletAddress ➔ vẫn assign vào DB, SC assignWorker bị bỏ qua (ghi log warning).
/// </summary>
public class CreateBatchCommandHandler : IRequestHandler<CreateBatchCommand, BatchDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<CreateBatchCommandHandler> _logger;
    private readonly IBlockchainJobQueue _blockchainQueue;

    public CreateBatchCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<CreateBatchCommandHandler> logger,
        IBlockchainJobQueue blockchainQueue)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
        _logger = logger;
        _blockchainQueue = blockchainQueue;
    }

    public async Task<BatchDto> Handle(CreateBatchCommand req, CancellationToken ct)
    {
        // ========== 1. Validate Processor ==========
        var processorId = Guard.RequireProcessor(_currentUser);
        var processorUser = await _uow.Users.GetByIdAsync(processorId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Processor User {processorId}.");

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

        // ========== 3. Validate FKs ==========
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

        // ========== 4. Validate Workers ==========
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

        var repUser = workers.First(w => w.Id == req.RepresentativeWorkerId);

        // ========== 5. Tạo Batch entity ==========
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
            BlockchainSyncStatus = BlockchainSyncStatus.PENDING,
        };

        // ========== 6. Tạo danh sách BatchWorker (BR-06: 1 đại diện) ==========
        var batchWorkers = req.AssignedWorkerIds
            .Distinct()
            .Select(workerId => new BatchWorker
            {
                BatchId = batch.Id,
                UserId = workerId,
                IsRepresentative = workerId == req.RepresentativeWorkerId,
                AssignedDate = DateTime.UtcNow,
                Status = WorkerAssignmentStatus.PENDING,
            })
            .ToList();
        batch.BatchWorkers = batchWorkers;

        // ========== INSERT BATCH + BATCH WORKERS VÀO DB TRƯỚC ==========
        // Phải insert trước để:
        //   (a) có batch.Id chính xác trong DB cho IPFS metadata
        //   (b) tránh lỗi "batch không tìm thấy" khi reload sau upload IPFS
        await _uow.Batches.AddAsync(batch, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Batch {BatchCode} (Id={BatchId}) đã tạo off-chain với {WorkerCount} workers.",
            batch.BatchCode, batch.Id, batchWorkers.Count);

       // ========== 7. Upload Metadata JSON lên IPFS ==========
        // QUAN TRỌNG: phải upload IPFS TRƯỚC rồi mới enqueue blockchain job.
        // Nếu enqueue trước, BlockchainJobProcessor chạy nền sẽ touch batch →
        // EF tracking ở đây bị lệch → UPDATE trả về 0 rows → DbUpdateConcurrencyException.
        try
        {
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

            // Update bằng ExecuteUpdate (raw SQL UPDATE) để hoàn toàn bypass EF change tracker.
            // Cách này tránh lỗi "association has been severed" khi EF tracking navigation
            // và FK required không khớp giữa entity và DB state.
            var updatedAt = DateTime.UtcNow;
            await _uow.Batches.UpdateMetadataAsync(batch.Id, metadataURI, dataHash, updatedAt, ct);

            // Đồng bộ lại các property trong memory để DTO trả về đúng.
            batch.MetadataURI = metadataURI;
            batch.DataHash = dataHash;
            batch.UpdatedAt = updatedAt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Upload metadata IPFS thất bại cho Batch {BatchCode}. Xóa batch khỏi DB.",
                batch.BatchCode);

            // Xóa batch + workers bằng raw SQL DELETE để bypass EF change tracker,
            // tránh lỗi "association has been severed".
            await _uow.Batches.DeleteBatchWithWorkersAsync(batch.Id, ct);

            throw;
        }

        // ========== 8. Enqueue job blockchain (SAU khi upload IPFS xong) ==========
        // batch.Id lúc này đã được EF generate thật (sau SaveChangesAsync ở bước 6).
        // Không gọi batch.Id trước SaveChanges → sẽ là Guid.Empty.
        if (batch.Id == Guid.Empty)
        {
            _logger.LogError(
                "BUG: batch.Id vẫn là Guid.Empty sau SaveChanges. Kiểm tra BatchRepository.");
            throw new InvalidOperationException("batch.Id chưa được generate.");
        }
        await _blockchainQueue.EnqueueAsync(batch.Id, ct);

        _logger.LogInformation(
            "Batch {BatchCode} đã tạo off-chain, enqueue blockchain job. Response trả về ngay.",
            batch.BatchCode);

        // ========== 9. Trả về DTO ==========
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
            Workers: workerDtos,
            BlockchainSyncStatus: batch.BlockchainSyncStatus.ToString(),
            CreateBatchTxHash: batch.CreateBatchTxHash,
            BlockchainSyncedAt: batch.BlockchainSyncedAt,
            BlockchainSyncError: batch.BlockchainSyncError);
    }
}