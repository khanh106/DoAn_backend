using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DoAnV2.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DoAnV2.Infrastructure.Services;

/// <summary>
/// Đọc thông tin user hiện tại từ JWT claims trong HttpContext.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;

    public CurrentUser(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    private ClaimsPrincipal? Principal =>
        _httpContext.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Principal?.FindFirstValue("UserId")
                      ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public string? Role =>
        Principal?.FindFirstValue(ClaimTypes.Role)
        ?? Principal?.FindFirstValue("Role");

    public string? WalletAddress =>
        Principal?.FindFirstValue("WalletAddress");
}
