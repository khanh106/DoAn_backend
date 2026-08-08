namespace DoAnV2.Application.Common.Options;

/// <summary>
/// Cấu hình IPFS Storage qua Filebase (TASK 03 - Mục 3.1).
///
/// Filebase cung cấp 2 API:
///  • S3-compatible (AWS SDK): dùng cho việc upload chính thức.
///  • IPFS HTTP REST:        dùng cho việc pin / ipfs-only operations.
/// Filebase cũng tự động pin file lên IPFS sau khi upload qua S3.
/// </summary>
public class IpfsOptions
{
    public const string SectionName = "Ipfs";

    // ============ S3-compatible (ưu tiên) ============

    /// <summary>
    /// S3-compatible endpoint của Filebase.
    /// Mặc định: https://s3.filebase.io
    /// </summary>
    public string Endpoint { get; set; } = "https://s3.filebase.io";

    /// <summary>
    /// Region. Filebase yêu cầu "auto" (không phải AWS region thật).
    /// </summary>
    public string Region { get; set; } = "auto";

    /// <summary>
    /// Tên bucket trên Filebase (vd: "fruitchains-metadata").
    /// Phải tạo bucket ở https://filebase.com trước.
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>Filebase Access Key (S3 credential).</summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>Filebase Secret Key (S3 credential).</summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>AWS Signature Version. Filebase yêu cầu v4.</summary>
    public string SignatureVersion { get; set; } = "v4";

    /// <summary>
    /// Gateway URL để tạo link truy cập file sau khi pin.
    /// Mặc định: https://ipfs.io/ipfs/ (public gateway).
    /// Filebase gateway: https://gateway.filebase.io/ipfs/
    /// </summary>
    public string GatewayUrl { get; set; } = "https://ipfs.io/ipfs/";

    // ============ REST API (legacy / pin-only) ============

    /// <summary>
    /// Bearer token cho REST API (https://api.filebase.io/v1/ipfs/upload).
    /// Không bắt buộc nếu đã dùng S3 SDK.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>REST API URL (chỉ dùng khi không dùng S3 SDK).</summary>
    public string? ApiUrl { get; set; }
}
