using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.Inspections.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Inspections.Commands;

/// <summary>
/// TASK 08 - Mục 8.1: Handler Processor ghi nhận Kiểm định cho Parent Batch (gọi SC inspectParent).
///   1. Validate Batch tồn tại &amp; Processor sở hữu &amp; ở STAGE_SORTED (BR-12, BR-14).
///   2. Validate CertificateFile không rỗng, Result là PASSED hoặc FAILED.
///   3. Upload file chứng nhận lên IPFS ➔ (FileURI, DataHash).
///   4. Upload Metadata JSON (DocumentName, DocumentNumber, InspectionUnit, InspectionDate, Result, Note) lên IPFS ➔ (MetadataURI, DataHash).
///   5. Processor gọi SC inspectParent(batchId, passed, metadataURI, dataHash).
///   6. Lưu Inspection (AssetType=PARENT) + cập nhật CurrentStage:
///        - PASSED ➔ INSPECTION_PASSED
///        - FAILED ➔ giữ nguyên STAGE_SORTED (BR-15).
/// </summary>
public class InspectParentCommandHandler
    : IRequestHandler<InspectParentCommand, InspectionResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIpfsService _ipfs;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<InspectParentCommandHandler> _logger;

    public InspectParentCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IIpfsService ipfs,
        IBlockchainService blockchain,
        ILogger<InspectParentCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _ipfs = ipfs;
        _blockchain = blockchain;
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

        // ========== 5. Gọi SC: inspectParent(batchId, passed, metadataURI, dataHash) ==========
        string txHash;
        try
        {
            txHash = await _blockchain.InspectParentAsync(
                batchId: batch.Id.ToString(),
                passed: resultEnum == InspectionResult.PASSED,
                metadataURI: metadataURI,
                dataHash: dataHash,
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
