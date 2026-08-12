using System.Security.Cryptography;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// Triển khai IIpfsService dùng Filebase S3-compatible API (TASK 03 - Mục 3.1).
///
/// Filebase tự động pin file lên IPFS ngay khi upload xong qua S3.
/// CID trả về là nội dung file (CIDv1 cho hầu hết file).
///
/// Client Configuration khuyến nghị của Filebase:
///   endpoint:           https://s3.filebase.io
///   accessKeyId:        &lt;FILEBASE_ACCESS_KEY&gt;
///   secretAccessKey:    &lt;FILEBASE_SECRET_KEY&gt;
///   region:             auto
///   signatureVersion:   v4
///
/// SDK: AWSSDK.S3 (AmazonS3Client).
/// </summary>
public class IpfsService : IIpfsService
{
    private readonly IpfsOptions _options;
    private readonly IAmazonS3 _s3;
    private readonly ILogger<IpfsService> _logger;

    public IpfsService(
        IOptions<IpfsOptions> options,
        IAmazonS3 s3,
        ILogger<IpfsService> logger)
    {
        _options = options.Value;
        _s3 = s3;
        _logger = logger;
    }

    // ============ JSON ============
    public async Task<(string MetadataURI, string DataHash)> UploadJsonAsync<T>(
        T data,
        string? fileName = null,
        CancellationToken ct = default)
    {
        var json = JsonConvert.SerializeObject(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        var name = fileName ?? $"metadata-{Guid.NewGuid():N}.json";
        var (uri, hash) = await UploadBytesAsync(bytes, name, "application/json", ct);
        return (uri, hash);
    }

    // ============ File (IFormFile) ============
    public async Task<(string FileURI, string DataHash)> UploadFileAsync(
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("File rỗng.", nameof(file));

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        return await UploadBytesAsync(bytes, file.FileName, file.ContentType ?? "application/octet-stream", ct);
    }

    // ============ Bytes (core) ============
    public async Task<(string FileURI, string DataHash)> UploadBytesAsync(
        byte[] bytes,
        string fileName,
        string contentType = "application/octet-stream",
        CancellationToken ct = default)
    {
        if (bytes is null || bytes.Length == 0)
            throw new ArgumentException("Bytes rỗng.", nameof(bytes));

        if (string.IsNullOrWhiteSpace(_options.Bucket))
            throw new InvalidOperationException("Ipfs:Bucket chưa được cấu hình.");

        // 1. SHA-256 nội dung
        var dataHash = ComputeSha256Hex(bytes);

        // 2. Build key (path trong bucket) - tránh trùng tên
        var key = $"fruit/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{SanitizeFileName(fileName)}";

        // 3. Upload qua S3 SDK (Filebase tự động pin lên IPFS)
        using var stream = new MemoryStream(bytes);
        var putRequest = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            // Metadata bổ sung - Filebase đọc để sinh CID
            Metadata =
            {
                ["filebase-sha256"] = dataHash,
                ["original-name"] = fileName,
            },
        };

        try
        {
            await _s3.PutObjectAsync(putRequest, ct);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Filebase S3 upload failed: {Status} - {Message}",
                ex.StatusCode, ex.Message);
            throw new InvalidOperationException(
                $"Filebase upload failed ({(int?)ex.StatusCode} {ex.ErrorCode}): {ex.Message}", ex);
        }

                // 4. Lấy CID (Filebase pin lên IPFS rồi set vào metadata)
        var cid = await ExtractCidFromResponseAsync(key, ct);

        // 5. Trả về URI
        string fileUri;
        if (!string.IsNullOrWhiteSpace(cid))
        {
            // Có CID thật → dùng IPFS gateway
            fileUri = !string.IsNullOrWhiteSpace(_options.GatewayUrl)
                ? $"{_options.GatewayUrl.TrimEnd('/')}/{cid}"
                : $"ipfs://{cid}";
        }
        else
        {
            // Không có CID → dùng S3 key qua backend proxy
            fileUri = $"/api/v1/ipfs/{key}";
        }

        _logger.LogInformation(
            "IPFS upload OK: key={Key}, cid={Cid}, size={Size}, sha256={Hash}, uri={Uri}",
            key, cid ?? "(fallback-proxy)", bytes.Length, dataHash, fileUri);

        return (fileUri, dataHash);

    }

    // ============ Helpers ============
    private static string ComputeSha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Filebase set CID trong object metadata khi upload qua S3.
    /// Vì PutObjectResponse không trả về Metadata, ta buộc phải HEAD
    /// object vừa upload để đọc metadata.
    /// </summary>
    private async Task<string?> ExtractCidFromResponseAsync(string key, CancellationToken ct)

{
    // Filebase trả CID trong metadata với key "cid" (AWS SDK tự bỏ prefix "x-amz-meta-").
    // Thử tối đa 8 lần với delay tăng dần (Filebase cần thời gian pin).
    for (int i = 0; i < 8; i++)
    {
        try
        {
            var head = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = key,
            }, ct);

            // AWS SDK .NET: head.Metadata["cid"] tự động map từ "x-amz-meta-cid"
            if (head.Metadata is not null)
            {
                // Duyệt tất cả metadata keys mà Filebase có thể trả về
                foreach (var metaKey in head.Metadata.Keys)
                {
                    _logger.LogDebug("Metadata key: {Key} = {Value}", metaKey, head.Metadata[metaKey]);
                }

                // Ưu tiên key "cid" (Filebase chuẩn)
                var cid = head.Metadata["cid"];
                if (!string.IsNullOrWhiteSpace(cid))
                    return cid;

                // Fallback: thử các key khác
                string[] fallbackKeys = { "ipfs-cid", "x-amz-meta-cid" };
                foreach (var k in fallbackKeys)
                {
                    var val = head.Metadata[k];
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }

            // Kiểm tra cả ETag - một số trường hợp Filebase dùng ETag = CID
            if (!string.IsNullOrWhiteSpace(head.ETag))
            {
                var etag = head.ETag.Trim('"');
                // CID v1 bắt đầu bằng "bafy" hoặc "bafk"
                if (etag.StartsWith("bafy") || etag.StartsWith("bafk") || etag.StartsWith("Qm"))
                {
                    _logger.LogInformation("Lấy CID từ ETag: {CID}", etag);
                    return etag;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lần {Attempt}: Không đọc được CID từ metadata (key={Key}).", i + 1, key);
        }

        // Delay tăng dần: 500ms, 1s, 1.5s, 2s, ...
        await Task.Delay((i + 1) * 500, ct);
    }

        // Fallback: trả về null để caller biết không có CID → dùng S3 key thay thế
    _logger.LogWarning("Filebase không trả CID cho key '{Key}' sau 8 lần thử. Sẽ dùng S3 key làm fallback.", key);
    return null;
}



    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        return sb.ToString();
    }
}
