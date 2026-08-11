namespace DoAnV2.Application.Features.MasterData.Dtos;

public record ProductDto(
    Guid Id,
    Guid FruitTypeId,
    string FruitTypeName,
    string GroupName,
    string ProductType,
    string Variety,
    string Name,
    string ShortName,
    string? Description,
    string Status,
    DateTime CreatedAt);

public record CreateProductRequest(
    Guid FruitTypeId,
    string? GroupName,
    string? ProductType,
    string? Variety,
    string Name,
    string? ShortName,
    string? Description);


public record UpdateProductRequest(
    string? GroupName,
    string? ProductType,
    string? Variety,
    string? Name,
    string? ShortName,
    string? Description,
    string? Status);