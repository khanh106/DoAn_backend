using System.Text.Json;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Packagings.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Packagings.Commands;

/// <summary>
/// TASK 08 - Mục 8.2: Handler Processor đóng gói cho Parent Batch (gọi SC packageParent).
/// </summary>
public class PackageParentCommandHandler
    : IRequestHandler<PackageParentCommand, PackagingResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<PackageParentCommandHandler> _logger;

    public PackageParentCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<PackageParentCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
        _logger = logger;
    }

    public async Task<PackagingResponseDto> Handle(
        PackageParentCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // ========== 1. Validate input ==========
        if (req.Input is null)
            throw new ValidationException("Thiếu thông tin đóng gói.");
        if (string.IsNullOrWhiteSpace(req.Input.Specification))
            throw new ValidationException("Specification không được trống.");
        if (req.Input.Weight <= 0)
            throw new ValidationException("Weight phải > 0.");

        // ========== 2. Validate Batch (BR-14) ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền đóng gói Batch của Processor khác.");

        if (batch.CurrentStage != BatchStage.INSPECTION_PASSED)
            throw new ValidationException(
                $"Batch hiện ở trạng thái {batch.CurrentStage}, " +
                "không thể đóng gói (yêu cầu INSPECTION_PASSED - BR-14).");

        // ========== 3. Upload ảnh (nếu có) lên IPFS ==========
        var imageUrls = new List<string>();
        var imageHashes = new List<string>();

        if (req.Images is not null && req.Images.Count > 0)
        {
            foreach (var img in req.Images)
            {
                if (img is null || img.Length == 0) continue;
                var (url, hash) = await _ipfs.UploadFileAsync(img, ct);
                imageUrls.Add(url);
                imageHashes.Add(hash);
            }
        }

        // ========== 4. Upload Metadata JSON lên IPFS ==========
        var now = DateTime.UtcNow;
        var metadata = new
        {
            assetType = "PARENT",
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            packagedByProcessorId = processorId,
            packDate = req.Input.PackDate,
            weight = req.Input.Weight,
            specification = req.Input.Specification,
            usageGuide = req.Input.UsageGuide,
            storageGuide = req.Input.StorageGuide,
            color = req.Input.Color,
            smell = req.Input.Smell,
            standard = req.Input.Standard,
            note = req.Input.Note,
            imageCount = imageUrls.Count,
            imageUrls = imageUrls,
            imageHashes = imageHashes,
            createdAt = now,
        };

        var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
            metadata,
            fileName: $"package-parent-{batch.BatchCode}-{now:yyyyMMddHHmmss}.json",
            ct: ct);

        // ========== 4.5. Lấy và giải mã Private Key của ví Processor ==========
        var processorUser = await _uow.Users.GetByIdAsync(processorId, ct)
            ?? throw new NotFoundException($"Không tìm thấy thông tin tài khoản Processor {processorId}.");

        string? signerPrivateKey = null;
        if (!string.IsNullOrWhiteSpace(processorUser.EncryptedPrivateKey))
        {
            signerPrivateKey = _walletService.DecryptPrivateKey(
                processorUser.EncryptedPrivateKey, _walletOptions.EncryptionKey);
        }

        // ========== 5. Gọi SC: packageParent(batchId, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.PackageParentAsync(
                batchId: batch.Id.ToString(),
                metadataURI: metadataURI,
                dataHash: dataHash,
                signerPrivateKey: signerPrivateKey,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "packageParent on-chain thất bại cho Batch {BatchId}.", batch.Id);
            throw;
        }

        // ========== 6. Lưu Packaging + cập nhật CurrentStage ==========
        var packaging = new Packaging
        {
            AssetType = AssetType.PARENT,
            BatchId = batch.Id,
            PackDate = req.Input.PackDate,
            Weight = req.Input.Weight,
            Specification = req.Input.Specification.Trim(),
            UsageGuide = req.Input.UsageGuide?.Trim(),
            StorageGuide = req.Input.StorageGuide?.Trim(),
            Color = req.Input.Color?.Trim(),
            Smell = req.Input.Smell?.Trim(),
            Standard = req.Input.Standard?.Trim(),
            Note = req.Input.Note?.Trim(),
            ImageUrlsJson = JsonSerializer.Serialize(imageUrls),
            // TASK 11: Lưu metadataURI/DataHash để phục vụ Retry (BR-42)
            MetadataURI = metadataURI,
            DataHash = dataHash,
        };
        await _uow.Packagings.AddAsync(packaging, ct);

        batch.CurrentStage = BatchStage.PACKAGED;
        _uow.Batches.Update(batch);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "PackageParent OK: Batch {BatchCode}, TxHash={TxHash}, NewStage={Stage}",
            batch.BatchCode, txHash, batch.CurrentStage);

        return new PackagingResponseDto(
            PackagingId: packaging.Id,
            AssetType: AssetType.PARENT.ToString(),
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            SubBatchId: null,
            SubBatchCode: null,
            PackDate: packaging.PackDate,
            Weight: packaging.Weight,
            Specification: packaging.Specification,
            UsageGuide: packaging.UsageGuide,
            StorageGuide: packaging.StorageGuide,
            Color: packaging.Color,
            Smell: packaging.Smell,
            Standard: packaging.Standard,
            ImageUrls: imageUrls,
            Note: packaging.Note,
            CurrentStage: batch.CurrentStage.ToString(),
            TransactionHash: txHash,
            CreatedAt: packaging.CreatedAt);
    }
}
