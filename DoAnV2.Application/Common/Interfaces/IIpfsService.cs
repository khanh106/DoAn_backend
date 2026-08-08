using Microsoft.AspNetCore.Http;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// IPFS Storage Service (TASK 03 - Mục 3.1).
/// Upload Metadata JSON, file ảnh nhật ký, file PDF chứng nhận kiểm định lên IPFS
/// thông qua Filebase / Pinata / Infura và trả về (URI, DataHash SHA-256).
/// </summary>
public interface IIpfsService
{
    /// <summary>
    /// Upload object JSON lên IPFS (Metadata chung cho Batch / SubBatch / Inspection...).
    /// </summary>
    /// <returns>
    /// MetadataURI: chuỗi `ipfs://&lt;CID&gt;` hoặc Gateway URL tương ứng.
    /// DataHash: SHA-256 hex (lowercase) của nội dung JSON (64 ký tự).
    /// </returns>
    Task<(string MetadataURI, string DataHash)> UploadJsonAsync<T>(
        T data,
        string? fileName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Upload file (ảnh / PDF) từ HTTP form lên IPFS.
    /// </summary>
    Task<(string FileURI, string DataHash)> UploadFileAsync(
        IFormFile file,
        CancellationToken ct = default);

    /// <summary>
    /// Upload file từ byte[] lên IPFS (dùng nội bộ khi đã có byte[] sẵn).
    /// </summary>
    Task<(string FileURI, string DataHash)> UploadBytesAsync(
        byte[] bytes,
        string fileName,
        string contentType = "application/octet-stream",
        CancellationToken ct = default);
}
