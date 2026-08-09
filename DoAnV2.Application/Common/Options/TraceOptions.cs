namespace DoAnV2.Application.Common.Options;

/// <summary>
/// Cấu hình URL truy xuất công khai (TASK 08 - Mục 8.3).
/// Mỗi QR code sẽ chứa link dạng `{TraceBaseUrl}?code={TargetId}`.
/// </summary>
public class TraceOptions
{
    public const string SectionName = "Trace";

    /// <summary>
    /// URL gốc của trang truy xuất công khai.
    /// Mặc định: https://truyxuat.domain.com/trace
    /// </summary>
    public string TraceBaseUrl { get; set; } = "https://truyxuat.domain.com/trace";
}
