using DoAnV2.Application.Features.MasterData.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.Products;

// ===== Commands =====
public record CreateProductCommand(
    Guid FruitTypeId,
    string GroupName,
    string ProductType,
    string Variety,
    string Name,
    string ShortName,
    string? Description) : IRequest<ProductDto>;

public record UpdateProductCommand(
    Guid Id,
    string? GroupName,
    string? ProductType,
    string? Variety,
    string? Name,
    string? ShortName,
    string? Description,
    string? Status) : IRequest<ProductDto>;

// ===== Queries =====
public record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;