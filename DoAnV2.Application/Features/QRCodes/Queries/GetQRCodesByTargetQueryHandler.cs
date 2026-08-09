using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.QRCodes.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.QRCodes.Queries;

/// <summary>
/// TASK 08 - Mục 8.3: Lấy danh sách QR code đã phát hành cho 1 đối tượng.
/// </summary>
public class GetQRCodesByTargetQueryHandler
    : IRequestHandler<GetQRCodesByTargetQuery, IReadOnlyList<QRCodeInfoDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetQRCodesByTargetQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<QRCodeInfoDto>> Handle(
        GetQRCodesByTargetQuery req, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Người dùng chưa đăng nhập.");

        var list = await _uow.QRCodes.GetByTargetAsync(req.TargetType, req.TargetId, ct);

        return list.Select(q => new QRCodeInfoDto(
            Id: q.Id,
            TargetType: q.TargetType.ToString(),
            TargetId: q.TargetId,
            QRValue: q.QRValue,
            Status: q.Status.ToString(),
            CreatedAt: q.CreatedAt)).ToList();
    }
}
