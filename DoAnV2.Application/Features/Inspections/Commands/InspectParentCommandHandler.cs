using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Inspections.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.Inspections.Commands;

/// <summary>
/// TASK 08 - Mục 8.1: Handler Processor ghi nhận Kiểm định cho Parent Batch (gọi SC inspectParent).
/// </summary>
public class InspectParentCommandHandler
    : IRequestHandler<InspectParentCommand, InspectionResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly IWalletService _walletService;
    private readonly WalletOptions _walletOptions;
    private readonly ILogger<InspectParentCommandHandler> _logger;

    public InspectParentCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        IWalletService walletService,
        IOptions<WalletOptions> walletOptions,
        ILogger<InspectParentCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
        _walletService = walletService;
        _walletOptions = walletOptions.Value;
        _logger = logger;
    }

    public async Task<InspectionResponseDto> Handle(
        InspectParentCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // ========== 1. Validate input ==========
        if (string.IsNullOrWhiteSpace(req.DocumentName))
            throw new ValidationException("DocumentName không được trống.");
        if (string.IsNullOrWhiteSpace(req.DocumentNumber))
            throw new ValidationException("DocumentNumber không được trống.");
        if (string.IsNullOrWhiteSpace(req.InspectionUnit))
            throw new ValidationException("InspectionUnit không được trống.");

        if (req.CertificateFile is null || req.CertificateFile.Length == 0)
            throw new ValidationException("CertificateFile không được trống.");

        if (!Enum.TryParse<InspectionResult>(req.Result, ignoreCase: true, out var resultEnum))
            throw new ValidationException("Result chỉ chấp nhận PASSED hoặc FAILED.");
        if (resultEnum == InspectionResult.PENDING)
            throw new ValidationException("Result không được ở trạng thái PENDING.");

        // ========== 2. Validate Batch (BR-14) ==========
        var batch = await _uow.Batches.GetByIdAsync(req.BatchId, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {req.BatchId}.");

        if (batch.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền kiểm định Batch của Processor khác.");

        if (batch.CurrentStage != BatchStage.STAGE_SORTED)
            throw new ValidationException(
                $"Batch hiện ở trạng thái {batch.CurrentStage}, không thể kiểm định (chỉ chấp nhận STAGE_SORTED).");

        // ========== 3. Upload CertificateFile lên IPFS ==========
        var (fileURI, fileDataHash) = await _ipfs.UploadFileAsync(req.CertificateFile, ct);

        // ========== 4. Upload Metadata JSON lên IPFS ==========
        var now = DateTime.UtcNow;
        var metadata = new
        {
            assetType = "PARENT",
            batchId = batch.Id,
            batchCode = batch.BatchCode,
            inspectedByProcessorId = processorId,
            documentName = req.DocumentName,
            documentNumber = req.DocumentNumber,
            inspectionUnit = req.InspectionUnit,
            inspectionDate = req.InspectionDate,
            result = req.Result.ToUpperInvariant(),
            note = req.Note,
            certificateFileURI = fileURI,
            certificateDataHash = fileDataHash,
            createdAt = now,
        };

        var (metadataURI, dataHash) = await _ipfs.UploadJsonAsync(
            metadata,
            fileName: $"inspect-parent-{batch.BatchCode}-{now:yyyyMMddHHmmss}.json",
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

        // ========== 5. Gọi SC: inspectParent(batchId, passed, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.InspectParentAsync(
                batchId: batch.Id.ToString(),
                passed: resultEnum == InspectionResult.PASSED,
                metadataURI: metadataURI,
                dataHash: dataHash,
                signerPrivateKey: signerPrivateKey,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "inspectParent on-chain thất bại cho Batch {BatchId}.", batch.Id);
            throw;
        }

        // ========== 6. Lưu Inspection + cập nhật CurrentStage ==========
        var inspection = new Inspection
        {
            AssetType = AssetType.PARENT,
            BatchId = batch.Id,
            DocumentName = req.DocumentName.Trim(),
            DocumentNumber = req.DocumentNumber.Trim(),
            InspectionUnit = req.InspectionUnit.Trim(),
            InspectionDate = req.InspectionDate,
            Result = resultEnum,
            FileURI = fileURI,
            Note = req.Note?.Trim(),
            // TASK 11: Lưu metadataURI/DataHash để phục vụ Retry (BR-42)
            MetadataURI = metadataURI,
            DataHash = dataHash,
        };
        await _uow.Inspections.AddAsync(inspection, ct);

        // BR-15: Chỉ chuyển stage khi PASSED
        if (resultEnum == InspectionResult.PASSED)
        {
            batch.CurrentStage = BatchStage.INSPECTION_PASSED;
            _uow.Batches.Update(batch);
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "InspectParent OK: Batch {BatchCode}, Result={Result}, TxHash={TxHash}, NewStage={Stage}",
            batch.BatchCode, resultEnum, txHash, batch.CurrentStage);

        return new InspectionResponseDto(
            InspectionId: inspection.Id,
            AssetType: AssetType.PARENT.ToString(),
            BatchId: batch.Id,
            BatchCode: batch.BatchCode,
            SubBatchId: null,
            SubBatchCode: null,
            DocumentName: inspection.DocumentName,
            DocumentNumber: inspection.DocumentNumber,
            InspectionUnit: inspection.InspectionUnit,
            InspectionDate: inspection.InspectionDate,
            Result: inspection.Result.ToString(),
            FileURI: inspection.FileURI,
            DataHash: fileDataHash,
            MetadataURI: metadataURI,
            Note: inspection.Note,
            CurrentStage: batch.CurrentStage.ToString(),
            TransactionHash: txHash,
            CreatedAt: inspection.CreatedAt);
    }
}
