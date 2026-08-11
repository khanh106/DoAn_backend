using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.ProcessorWorkers.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.ProcessorWorkers.Queries;

public record GetFarmerInvitationsQuery(CoopWorkerLinkStatus? Status = null) : IRequest<IReadOnlyList<ProcessorWorkerLinkDto>>;

public class GetFarmerInvitationsQueryHandler : IRequestHandler<GetFarmerInvitationsQuery, IReadOnlyList<ProcessorWorkerLinkDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetFarmerInvitationsQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProcessorWorkerLinkDto>> Handle(GetFarmerInvitationsQuery req, CancellationToken ct)
    {
        var farmerId = Guard.RequireFarmer(_currentUser);
        var links = await _uow.ProcessorWorkers.GetByWorkerIdAsync(farmerId, req.Status, ct);

        return links.Select(link => new ProcessorWorkerLinkDto(
            Id: link.Id,
            ProcessorId: link.ProcessorId,
            ProcessorName: link.Processor?.FullName ?? "Hợp tác xã",
            WorkerId: link.WorkerId,
            WorkerName: link.Worker?.FullName ?? "",
            WorkerEmail: link.Worker?.Email ?? "",
            WorkerPhone: link.Worker?.Phone ?? "",
            WorkerWalletAddress: link.Worker?.WalletAddress,
            Status: link.Status.ToString(),
            InvitedAt: link.InvitedAt,
            RespondedAt: link.RespondedAt
        )).ToList();
    }
}
