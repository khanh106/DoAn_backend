using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.ProcessorWorkers.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.ProcessorWorkers.Commands;

public record SendWorkerInvitationCommand(Guid WorkerId) : IRequest<ProcessorWorkerLinkDto>;

public class SendWorkerInvitationCommandHandler : IRequestHandler<SendWorkerInvitationCommand, ProcessorWorkerLinkDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public SendWorkerInvitationCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ProcessorWorkerLinkDto> Handle(SendWorkerInvitationCommand req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        var worker = await _uow.Users.GetByIdAsync(req.WorkerId, ct)
            ?? throw new NotFoundException("Không tìm thấy thông tin công nhân.");

        if (worker.Role?.RoleName != RoleType.FARMER)
            throw new ValidationException("Tài khoản này không phải là Công nhân/Nông dân.");

        var existing = await _uow.ProcessorWorkers.GetAsync(processorId, req.WorkerId, ct);
        if (existing != null)
        {
            if (existing.Status == CoopWorkerLinkStatus.ACCEPTED)
                throw new ConflictException("Công nhân này đã liên kết với Hợp tác xã.");
            
            // Nếu từng bị từ chối, cập nhật lại trạng thái PENDING để gửi lại lời mời
            existing.Status = CoopWorkerLinkStatus.PENDING;
            existing.InvitedAt = DateTime.UtcNow;
            existing.RespondedAt = null;
            _uow.ProcessorWorkers.Update(existing);
            await _uow.SaveChangesAsync(ct);
            return MapToDto(existing, worker);
        }

        var link = new ProcessorWorker
        {
            ProcessorId = processorId,
            WorkerId = req.WorkerId,
            Status = CoopWorkerLinkStatus.PENDING,
            InvitedAt = DateTime.UtcNow
        };

        await _uow.ProcessorWorkers.AddAsync(link, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToDto(link, worker);
    }

    private static ProcessorWorkerLinkDto MapToDto(ProcessorWorker entity, User worker)
        => new(entity.Id, entity.ProcessorId, string.Empty, entity.WorkerId, worker.FullName, worker.Email, worker.Phone, worker.WalletAddress, entity.Status.ToString(), entity.InvitedAt, entity.RespondedAt);
}
