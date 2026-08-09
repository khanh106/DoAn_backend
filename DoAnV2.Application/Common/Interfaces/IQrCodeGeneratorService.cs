namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Service sinh ảnh QR Code từ chuỗi nội dung (TASK 08 - Mục 8.3).
/// Triển khai bằng thư viện QRCoder (đã có sẵn trong Infrastructure).
/// </summary>
public interface IQrCodeGeneratorService
{
    /// <summary>
    /// Sinh ảnh QR (PNG) từ nội dung text. Trả về mảng byte[] PNG.
    /// </summary>
    /// <param name="content">Chuỗi URL truy xuất hoặc bất kỳ text nào cần encode.</param>
    /// <param name="pixelsPerModule">Kích thước mỗi module (mặc định 10px).</param>
    byte[] GeneratePng(string content, int pixelsPerModule = 10);

    /// <summary>
    /// Sinh ảnh QR (PNG) từ nội dung text và trả về Base64 string.
    /// </summary>
    string GeneratePngBase64(string content, int pixelsPerModule = 10);

    /// <summary>
    /// Sinh ảnh QR (SVG) từ nội dung text.
    /// </summary>
    string GenerateSvg(string content);
}
