using DoAnV2.Application.Common.Interfaces;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// BCrypt implementation for IPasswordHasher (BR-46.3 - không lưu plaintext).
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
