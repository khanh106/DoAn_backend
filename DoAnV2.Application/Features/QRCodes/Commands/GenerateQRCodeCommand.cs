using DoAnV2.Application.Features.QRCodes.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.QRCodes.Commands;

/// <summary>
/// TASK 08 - Mục 8.3: Processor sinh mã QR Code cho lô (BATCH/SUBBATCH/BOX/COMMERCIAL).
/// BR-14 + BR-15: Nếu là QR thương mại (BOX/COMMERCIAL), đối tượng phải ở trạng thái PACKAGED.
/// </summary>
public record GenerateQRCodeCommand(
    QRTargetType TargetType,
    Guid TargetId) : IRequest<GenerateQRCodeResponseDto>;
