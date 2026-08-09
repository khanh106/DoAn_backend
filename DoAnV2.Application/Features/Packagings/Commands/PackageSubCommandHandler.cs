using System.Text.Json;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Packagings.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Packagings.Commands;

/// <summary>
/// TASK 08 - Mục 8.2: Handler Processor đóng gói cho SubBatch (gọi SC packageSub).
///   1. Validate SubBatch tồn tại &amp; Processor sở hữu Parent &amp; SubBatch ở INSPECTION_PASSED (BR-14).
///   2. Upload ảnh (nếu có) + Metadata JSON lên IPFS ➔ (MetadataURI, DataHash).
///   3. Processor gọi SC packageSub(subBatchId, metadataURI, dataHash).
///   4. Lưu Packaging (AssetType=SUB) + cập nhật SubBatch.CurrentStage = PACKAGED.
/// </summary>
public class PackageSubCommandHandler
    : IRequestHandler<PackageSubCommand, PackagingResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<PackageSubCommandHandler> _logger;

    public PackageSubCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        ILogger<PackageSubCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<PackagingResponseDto> Handle(
        PackageSubCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // ========== 1. Validate input ==========
        if (req.Input is null)
            throw new ValidationException("Thiếu thông tin đóng gói.");
        if (string.IsNullOrWhiteSpace(req.Input.Specification))
            throw new ValidationException("Specification không được trống.");
        if (req.Input.Weight <= 0)
            throw new ValidationException("Weight phải > 0.");

        // ========== 2. Validate SubBatch (BR-14, BR-16) ==========
        var subBatch = await _uow.SubBatches.GetByIdAsync(req.SubBatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy SubBatch {req.SubBatchId}.");

        var parentBatch = await _uow.Batches.GetByIdAsync(subBatch.ParentBatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Parent Batch.");

        if (parentBatch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền đóng gói SubBatch của Processor khác.");

        if (subBatch.CurrentStage != BatchStage.INSPECTION_PASSED)
            throw new ValidationException(
                $"SubBatch hiện ở trạng thái {subBatch.CurrentStage}, " +
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
            assetType = "SUB",
            subBatchId = subBatch.Id,
            subBatchCode = subBatch.SubBatchCode,
            parentBatchId = parentBatch.Id,
            parentBatchCode = parentBatch.BatchCode,
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
            fileName: $"package-sub-{subBatch.SubBatchCode}-{now:yyyyMMddHHmmss}.json",
            ct: ct);

        // ========== 5. Gọi SC: packageSub(subBatchId, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.PackageSubAsync(
                subBatchId: subBatch.Id.ToString(),
                metadataURI: metadataURI,
                dataHash: dataHash,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "packageSub on-chain thất bại cho SubBatch {SubBatchId}.", subBatch.Id);
            throw;
        }

        // ========== 6. Lưu Packaging + cập nhật CurrentStage ==========
        var packaging = new Packaging
        {
            AssetType = AssetType.SUB,
            SubBatchId = subBatch.Id,
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
        };
        await _uow.Packagings.AddAsync(packaging, ct);

        subBatch.CurrentStage = BatchStage.PACKAGED;
        _uow.SubBatches.Update(subBatch);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "PackageSub OK: SubBatch {SubBatchCode}, TxHash={TxHash}, NewStage={Stage}",
            subBatch.SubBatchCode, txHash, subBatch.CurrentStage);

        return new PackagingResponseDto(
            PackagingId: packaging.Id,
            AssetType: AssetType.SUB.ToString(),
            BatchId: null,
            BatchCode: parentBatch.BatchCode,
            SubBatchId: subBatch.Id,
            SubBatchCode: subBatch.SubBatchCode,
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
            CurrentStage: subBatch.CurrentStage.ToString(),
            TransactionHash: txHash,
            CreatedAt: packaging.CreatedAt);
    }
}
