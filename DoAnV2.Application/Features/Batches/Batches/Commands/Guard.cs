using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;

namespace DoAnV2.Application.Features.Batches.Batches.Commands;

/// <summary>
/// Helper ép user hiện tại phải là PROCESSOR (APPROVED).
/// Trả về Guid ProcessorId (= UserId).
/// </summary>
internal static class Guard
{
    public static Guid RequireProcessor(ICurrentUser current)
    {
        if (!current.IsAuthenticated || current.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var role = current.Role;
        if (!string.Equals(role, "PROCESSOR", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Chỉ tài khoản PROCESSOR mới được phép thao tác.");

        return current.UserId.Value;
    }

    public static Guid RequireFarmer(ICurrentUser current)
    {
        if (!current.IsAuthenticated || current.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var role = current.Role;
        if (!string.Equals(role, "FARMER", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Chỉ tài khoản FARMER mới được phép thao tác.");

        return current.UserId.Value;
    }

    /// <summary>
    /// Helper ép user hiện tại phải là RETAILER (APPROVED) - TASK 09.
    /// Trả về Guid RetailerId (= UserId).
    /// </summary>
    public static Guid RequireRetailer(ICurrentUser current)
    {
        if (!current.IsAuthenticated || current.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var role = current.Role;
        if (!string.Equals(role, "RETAILER", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Chỉ tài khoản RETAILER mới được phép thao tác.");

        return current.UserId.Value;
    }
}
