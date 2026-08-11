using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.Auth.Handlers;

/// <summary>
/// Handler xử lý Admin phân quyền / đổi Role cho tài khoản:
/// - Ràng buộc 1: Tài khoản admin@gmail.com là ADMIN tối cao, không thể chuyển đổi sang Role khác.
/// - Ràng buộc 2: Không tài khoản nào khác được phép gán vai trò ADMIN ngoài admin@gmail.com.
/// </summary>
public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, UserAccountDto>
{
    private readonly IUnitOfWork _uow;

    public ChangeUserRoleCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UserAccountDto> Handle(ChangeUserRoleCommand req, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(req.UserId, ct)
            ?? throw new NotFoundException($"Không tìm thấy tài khoản với ID {req.UserId}.");

        // RÀNG BUỘC 1: Tài khoản admin@gmail.com luôn phải giữ vai trò ADMIN
        if (string.Equals(user.Email, "admin@gmail.com", StringComparison.OrdinalIgnoreCase) && req.NewRole != RoleType.ADMIN)
        {
            throw new ForbiddenException("Tài khoản admin@gmail.com là Quản trị viên tối cao của hệ thống và không thể thay đổi vai trò.");
        }

        // RÀNG BUỘC 2: Chỉ duy nhất admin@gmail.com mới có quyền sở hữu vai trò ADMIN
        if (req.NewRole == RoleType.ADMIN && !string.Equals(user.Email, "admin@gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Chỉ duy nhất tài khoản admin@gmail.com mới có quyền sở hữu vai trò ADMIN.");
        }

        // Cập nhật vai trò
        user.RoleId = (int)req.NewRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        // Lấy thông tin user vừa cập nhật
        var updatedUser = await _uow.Users.GetByIdAsync(user.Id, ct) ?? user;

        return new UserAccountDto(
            updatedUser.Id,
            updatedUser.FullName,
            updatedUser.Email,
            updatedUser.Phone,
            updatedUser.Role?.RoleName.ToString() ?? req.NewRole.ToString(),
            updatedUser.WalletAddress,
            updatedUser.Status.ToString(),
            updatedUser.CreatedAt
        );
    }
}
