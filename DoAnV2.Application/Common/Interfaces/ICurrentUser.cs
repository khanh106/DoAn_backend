namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Trừu tượng hoá việc truy cập thông tin user đang đăng nhập
/// từ HttpContext (đọc từ JWT claims).
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    string? WalletAddress { get; }
    bool IsAuthenticated { get; }
}
