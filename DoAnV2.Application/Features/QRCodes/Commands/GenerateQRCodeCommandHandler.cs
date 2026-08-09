using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.QRCodes.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoAnV2.Application.Features.QRCodes.Commands;

/// <summary>
/// TASK 08 - Mục 8.3: Handler sinh QR Code cho lô (BATCH/SUBBATCH/BOX/COMMERCIAL).
///   1. Validate đối tượng tồn tại theo TargetType:
///        - BATCH � lấy Batch.
///        - SUBBATCH ➔ lấy SubBatch.
///        - BOX / COMMERCIAL ➔ tạm thời mapping sang Batch/SubBatch (xem dưới).
///   2. Nếu là QR thương mại (BOX/COMMERCIAL): yêu cầu PACKAGED (BR-14).
///   3. Sinh URL truy xuất công khai: {TraceBaseUrl}?code={TargetId}.
///   4. Render ảnh QR (PNG Base64) bằng QRCoder.
///   5. Lưu QRCode vào DB.
/// </summary>
public class GenerateQRCodeCommandHandler
    : IRequestHandler<GenerateQRCodeCommand, GenerateQRCodeResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IQrCodeGeneratorService _qr;
    private readonly TraceOptions _traceOptions;
    private readonly ILogger<GenerateQRCodeCommandHandler> _logger;

    public GenerateQRCodeCommandHandler(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IQrCodeGeneratorService qr,
        IOptions<TraceOptions> traceOptions,
        ILogger<GenerateQRCodeCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _qr = qr;
        _traceOptions = traceOptions.Value;
        _logger = logger;
    }

    public async Task<GenerateQRCodeResponseDto> Handle(
        GenerateQRCodeCommand req, CancellationToken ct)
    {
        Guard.RequireProcessor(_currentUser);

        string targetCode;
        BatchStage currentStage;

        switch (req.TargetType)
        {
            case QRTargetType.BATCH:
                {
                    var batch = await _uow.Batches.GetByIdAsync(req.TargetId, ct)
                        ?? throw new NotFoundException($"Không tìm thấy Batch {req.TargetId}.");
                    targetCode = batch.BatchCode;
                    currentStage = batch.CurrentStage;
                    break;
                }
            case QRTargetType.SUBBATCH:
                {
                    var subBatch = await _uow.SubBatches.GetByIdAsync(req.TargetId, ct)
                        ?? throw new NotFoundException($"Không tìm thấy SubBatch {req.TargetId}.");
                    targetCode = subBatch.SubBatchCode;
                    currentStage = subBatch.CurrentStage;
                    break;
                }
            case QRTargetType.BOX:
            case QRTargetType.COMMERCIAL:
                {
                    // Hiện tại hệ thống mapping BOX/COMMERCIAL về Batch/SubBatch theo ID target.
                    // Ưu tiên SubBatch trước, fallback Batch.
                    var subBatch = await _uow.SubBatches.GetByIdAsync(req.TargetId, ct);
                    if (subBatch != null)
                    {
                        targetCode = subBatch.SubBatchCode;
                        currentStage = subBatch.CurrentStage;
                    }
                    else
                    {
                        var batch = await _uow.Batches.GetByIdAsync(req.TargetId, ct)
                            ?? throw new NotFoundException(
                                $"Không tìm thấy đối tượng Batch/SubBatch với Id {req.TargetId}.");
                        targetCode = batch.BatchCode;
                        currentStage = batch.CurrentStage;
                    }

                    // BR-14: QR thương mại chỉ phát hành khi đã PACKAGED.
                    if (currentStage != BatchStage.PACKAGED)
                        throw new ValidationException(
                            $"Đối tượng hiện ở trạng thái {currentStage}, " +
                            "không thể phát hành QR thương mại (yêu cầu PACKAGED - BR-14).");
                    break;
                }
            default:
                throw new ValidationException(
                    $"TargetType '{req.TargetType}' không được hỗ trợ.");
        }

        // ========== Sinh URL truy xuất công khai ==========
        var qrValue = $"{_traceOptions.TraceBaseUrl}?code={req.TargetId}";

        // ========== Render QR PNG (Base64) ==========
        var imageBase64 = _qr.GeneratePngBase64(qrValue);

        // ========== Lưu QRCode ==========
        var qrCode = new QRCode
        {
            TargetType = req.TargetType,
            TargetId = req.TargetId,
            QRValue = qrValue,
            Status = QRCodeStatus.ACTIVE,
        };
        await _uow.QRCodes.AddAsync(qrCode, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "GenerateQRCode OK: TargetType={TargetType}, TargetId={TargetId}, QRCodeId={QRCodeId}",
            req.TargetType, req.TargetId, qrCode.Id);

        return new GenerateQRCodeResponseDto(
            QRCodeId: qrCode.Id,
            TargetType: qrCode.TargetType.ToString(),
            TargetId: qrCode.TargetId,
            TargetCode: targetCode,
            QRValue: qrCode.QRValue,
            ImageBase64: imageBase64,
            ImageContentType: "image/png",
            Status: qrCode.Status.ToString(),
            CreatedAt: qrCode.CreatedAt);
    }
}
