using DoAnV2.Application.Common.Interfaces;
using QRCoder;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// Triển khai IQrCodeGeneratorService dùng thư viện QRCoder (TASK 08 - Mục 8.3).
/// Render QR code PNG với kích thước tuỳ chỉnh.
/// </summary>
public class QrCodeGeneratorService : IQrCodeGeneratorService
{
    public byte[] GeneratePng(string content, int pixelsPerModule = 10)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Nội dung QR không được trống.", nameof(content));

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    public string GeneratePngBase64(string content, int pixelsPerModule = 10)
    {
        var bytes = GeneratePng(content, pixelsPerModule);
        return Convert.ToBase64String(bytes);
    }

    public string GenerateSvg(string content)
    {
        // QRCoder 1.4.3 không expose SvgQRCode trong package mặc định.
        // Fallback: trả về PNG Base64 với data URI prefix để dùng được cho web.
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Nội dung QR không được trống.", nameof(content));

        var pngBase64 = GeneratePngBase64(content);
        return $"data:image/png;base64,{pngBase64}";
    }
}
