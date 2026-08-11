using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.ProcessorWorkers.Commands;

public record RespondWorkerInvitationCommand(Guid InvitationId, string Action) : IRequest<bool>;

public class RespondWorkerInvitationCommandHandler : IRequestHandler<RespondWorkerInvitationCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public RespondWorkerInvitationCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(RespondWorkerInvitationCommand req, CancellationToken ct)
    {
        var farmerId = Guard.RequireFarmer(_currentUser);

        var link = await _uow.ProcessorWorkers.GetByIdAsync(req.InvitationId, ct)
            ?? throw new NotFoundException("Không tìm thấy lời mời liên kết.");

        if (link.WorkerId != farmerId)
            throw new ForbiddenException("Bạn không có quyền phản hồi lời mời này.");

        if (req.Action.ToUpper() == "ACCEPT")
            link.Status = CoopWorkerLinkStatus.ACCEPTED;
        else if (req.Action.ToUpper() == "REJECT")
            link.Status = CoopWorkerLinkStatus.REJECTED;
        else
            throw new ValidationException("Hành động không hợp lệ (chỉ nhận ACCEPT hoặc REJECT).");

        link.RespondedAt = DateTime.UtcNow;
        _uow.ProcessorWorkers.Update(link);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
