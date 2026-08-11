using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Batches.Batches.Commands;
using DoAnV2.Application.Features.ProcessorWorkers.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.ProcessorWorkers.Queries;

public record SearchWorkersQuery(string? Keyword) : IRequest<IReadOnlyList<SearchWorkerResultDto>>;

public class SearchWorkersQueryHandler : IRequestHandler<SearchWorkersQuery, IReadOnlyList<SearchWorkerResultDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public SearchWorkersQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SearchWorkerResultDto>> Handle(SearchWorkersQuery req, CancellationToken ct)
    {
        var processorId = Guard.RequireProcessor(_currentUser);

        // Lấy tất cả Farmer đã APPROVED theo keyword
        var farmers = await _uow.Users.SearchFarmersAsync(req.Keyword, ct);

        // Lấy danh sách liên kết hiện tại của Processor này
        var linksList = await _uow.ProcessorWorkers.GetByProcessorIdAsync(processorId, null, ct);
        var links = linksList.ToDictionary(w => w.WorkerId, w => w.Status.ToString());

        return farmers.Select(f => new SearchWorkerResultDto(
            WorkerId: f.Id,
            FullName: f.FullName,
            Email: f.Email,
            Phone: f.Phone,
            WalletAddress: f.WalletAddress,
            LinkStatus: links.TryGetValue(f.Id, out var status) ? status : "NONE"
        )).ToList();
    }
}
