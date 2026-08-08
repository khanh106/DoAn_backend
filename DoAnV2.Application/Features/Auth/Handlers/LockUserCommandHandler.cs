using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.Auth.Handlers;

/// <summary>
/// Khoá / Mở khoá user (chỉ Admin).
/// </summary>
public class LockUserCommandHandler : IRequestHandler<LockUserCommand, PendingUserDto>
{
    private readonly IUnitOfWork _uow;

    public LockUserCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PendingUserDto> Handle(LockUserCommand req, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(req.UserId, ct)
            ?? throw new NotFoundException($"Không tìm thấy user {req.UserId}.");

        user.Status = req.Lock ? UserStatus.LOCKED : UserStatus.APPROVED;
        user.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        return new PendingUserDto(
            user.Id, user.FullName, user.Email, user.Phone,
            user.Role?.RoleName.ToString() ?? string.Empty,
            user.Status.ToString(), user.CreatedAt);
    }
}
