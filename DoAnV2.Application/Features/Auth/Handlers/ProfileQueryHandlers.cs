using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Auth.Commands;
using DoAnV2.Application.Features.Auth.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.Auth.Handlers;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, ProfileResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetMyProfileQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ProfileResponse> Handle(GetMyProfileQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var user = await _uow.Users.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");

        return new ProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role?.RoleName.ToString() ?? string.Empty,
            user.WalletAddress,
            user.Status.ToString(),
            user.CreatedAt,
            user.UpdatedAt);
    }
}

public class GetPendingUsersQueryHandler : IRequestHandler<GetPendingUsersQuery, IReadOnlyList<PendingUserDto>>
{
    private readonly IUnitOfWork _uow;

    public GetPendingUsersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<PendingUserDto>> Handle(GetPendingUsersQuery req, CancellationToken ct)
    {
        var users = await _uow.Users.GetPendingUsersAsync(ct);
        return users.Select(u => new PendingUserDto(
            u.Id, u.FullName, u.Email, u.Phone,
            u.Role?.RoleName.ToString() ?? string.Empty,
            u.Status.ToString(), u.CreatedAt)).ToList();
    }
}
