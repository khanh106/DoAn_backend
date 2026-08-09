using DoAnV2.Application.Features.QRCodes.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.QRCodes.Queries;

/// <summary>Lấy danh sách QR code đã phát hành cho 1 đối tượng (TASK 08 - Mục 8.3).</summary>
public record GetQRCodesByTargetQuery(QRTargetType TargetType, Guid TargetId)
    : IRequest<IReadOnlyList<QRCodeInfoDto>>;
