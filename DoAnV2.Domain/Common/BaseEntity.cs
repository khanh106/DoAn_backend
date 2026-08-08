namespace DoAnV2.Domain.Common;

/// <summary>
/// Lớp cơ sở cho mọi Entity trong hệ thống.
/// Cung cấp 4 trường chung: Id (GUID), CreatedAt, UpdatedAt, IsDeleted.
/// Soft Delete: thay vì xóa cứng khỏi DB, ta chỉ đánh dấu IsDeleted = true
/// và dùng Global Query Filter trong DbContext để tự động lọc ra khỏi truy vấn.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Khóa chính kiểu GUID, tự sinh khi tạo mới.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Thời điểm tạo bản ghi (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật gần nhất (UTC), null nếu chưa sửa.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Cờ xóa mềm (Soft Delete). Mặc định false.</summary>
    public bool IsDeleted { get; set; } = false;
}