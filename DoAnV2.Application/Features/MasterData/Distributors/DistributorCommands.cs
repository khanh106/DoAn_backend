using DoAnV2.Application.Features.MasterData.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.Distributors;

public record CreateDistributorCommand(
    string Code,
    string Name,
    string Phone,
    string? Email,
    string Address,
    string? TaxCode) : IRequest<DistributorDto>;

public record DeleteDistributorCommand(Guid Id) : IRequest<bool>;

public record GetDistributorsQuery : IRequest<IReadOnlyList<DistributorDto>>;

public record SearchRetailersQuery(string? Keyword) : IRequest<IReadOnlyList<SearchRetailerResultDto>>;

public record LinkRetailerCommand(Guid RetailerId) : IRequest<DistributorDto>;
