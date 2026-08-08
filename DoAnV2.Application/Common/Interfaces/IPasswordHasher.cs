using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Interface cho việc hash / verify mật khẩu người dùng.
/// Triển khai bằng BCrypt (BR-46.3 - không bao giờ lưu plaintext).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hash mật khẩu plaintext → chuỗi hash BCrypt.</summary>
    string Hash(string password);

    /// <summary>Verify mật khẩu plaintext với chuỗi hash đã lưu trong DB.</summary>
    bool Verify(string password, string hash);
}