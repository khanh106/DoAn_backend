using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.FruitTypes;
using DoAnV2.Domain.Entities;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.Distributors;

public class CreateDistributorCommandHandler : IRequestHandler<CreateDistributorCommand, DistributorDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateDistributorCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<DistributorDto> Handle(CreateDistributorCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var entity = new Distributor
        {
            ProcessorId = processorId,
            Code = req.Code.Trim().ToUpperInvariant(),
            Name = req.Name.Trim(),
            Phone = req.Phone.Trim(),
            Email = req.Email?.Trim(),
            Address = req.Address.Trim(),
            TaxCode = req.TaxCode?.Trim(),
            Status = "ACTIVE",
        };

        await _uow.Distributors.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return new DistributorDto(
            entity.Id, entity.Code, entity.Name, entity.Phone,
            entity.Email, entity.Address, entity.TaxCode, entity.Status, entity.RetailerId);
    }
}

public class GetDistributorsQueryHandler : IRequestHandler<GetDistributorsQuery, IReadOnlyList<DistributorDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetDistributorsQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DistributorDto>> Handle(GetDistributorsQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var list = await _uow.Distributors.GetByProcessorIdAsync(processorId, ct);

        return list.Select(e => new DistributorDto(
            e.Id, e.Code, e.Name, e.Phone, e.Email, e.Address, e.TaxCode, e.Status, e.RetailerId)).ToList();
    }
}

public class SearchRetailersQueryHandler : IRequestHandler<SearchRetailersQuery, IReadOnlyList<SearchRetailerResultDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public SearchRetailersQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SearchRetailerResultDto>> Handle(SearchRetailersQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var retailers = await _uow.Users.SearchRetailersAsync(req.Keyword, ct);
        var existingDistributors = await _uow.Distributors.GetByProcessorIdAsync(processorId, ct);
        
        var linkedMap = existingDistributors
            .Where(d => d.RetailerId.HasValue)
            .ToDictionary(d => d.RetailerId!.Value, d => d.Id);

        return retailers.Select(r =>
        {
            var isLinked = linkedMap.TryGetValue(r.Id, out var distId);
            return new SearchRetailerResultDto(
                RetailerId: r.Id,
                FullName: r.FullName,
                Email: r.Email,
                Phone: r.Phone,
                WalletAddress: r.WalletAddress,
                IsLinked: isLinked,
                DistributorId: isLinked ? distId : null
            );
        }).ToList();
    }
}

public class LinkRetailerCommandHandler : IRequestHandler<LinkRetailerCommand, DistributorDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public LinkRetailerCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<DistributorDto> Handle(LinkRetailerCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var retailer = await _uow.Users.GetByIdAsync(req.RetailerId, ct)
            ?? throw new NotFoundException($"Không tìm thấy tài khoản Siêu thị {req.RetailerId}.");

        if (retailer.Role?.RoleName != Domain.Enums.RoleType.RETAILER || retailer.Status != Domain.Enums.UserStatus.APPROVED)
        {
            throw new ValidationException("Tài khoản được chọn không phải là Siêu thị/Cửa hàng bán lẻ đã được phê duyệt.");
        }

        var existingDistributors = await _uow.Distributors.GetByProcessorIdAsync(processorId, ct);
        var existing = existingDistributors.FirstOrDefault(d => d.RetailerId == req.RetailerId);
        if (existing != null)
        {
            return new DistributorDto(
                existing.Id, existing.Code, existing.Name, existing.Phone,
                existing.Email, existing.Address, existing.TaxCode, existing.Status, existing.RetailerId);
        }

        var shortId = retailer.Id.ToString("N")[..6].ToUpper();
        var entity = new Distributor
        {
            ProcessorId = processorId,
            RetailerId = retailer.Id,
            Code = $"SM-{shortId}",
            Name = retailer.FullName,
            Phone = retailer.Phone,
            Email = retailer.Email,
            Address = !string.IsNullOrWhiteSpace(retailer.CooperativeProfileInfo) ? retailer.CooperativeProfileInfo : "Hệ thống Siêu thị liên kết",
            TaxCode = null,
            Status = "ACTIVE"
        };

        await _uow.Distributors.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return new DistributorDto(
            entity.Id, entity.Code, entity.Name, entity.Phone,
            entity.Email, entity.Address, entity.TaxCode, entity.Status, entity.RetailerId);
    }
}

public class DeleteDistributorCommandHandler : IRequestHandler<DeleteDistributorCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public DeleteDistributorCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(DeleteDistributorCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);

        var entity = await _uow.Distributors.GetByIdAsync(req.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy Nhà phân phối {req.Id}.");

        if (entity.ProcessorId != processorId)
            throw new ForbiddenException("Bạn không có quyền xóa nhà phân phối của đơn vị khác.");

        _uow.Distributors.Delete(entity);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
