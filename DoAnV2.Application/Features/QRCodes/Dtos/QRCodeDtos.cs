namespace DoAnV2.Application.Features.QRCodes.Dtos;

/// <summary>
/// DTO trả về sau khi sinh mã QR Code (TASK 08 - Mục 8.3).
/// Áp dụng cho BATCH / SUBBATCH / BOX / COMMERCIAL (BR-17).
/// </summary>
public record GenerateQRCodeResponseDto(
    Guid QRCodeId,
    string TargetType,
    Guid TargetId,
    string TargetCode,
    string QRValue,
    string ImageBase64,
    string ImageContentType,
    string Status,
    DateTime CreatedAt);

/// <summary>
/// Thông tin QR Code truy xuất (trả về khi xem danh sách).
/// </summary>
public record QRCodeInfoDto(
    Guid Id,
    string TargetType,
    Guid TargetId,
    string QRValue,
    string Status,
    DateTime CreatedAt);
