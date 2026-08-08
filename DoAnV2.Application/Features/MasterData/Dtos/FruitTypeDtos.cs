namespace DoAnV2.Application.Features.MasterData.Dtos;

public record FruitTypeDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateFruitTypeRequest(
    string Name,
    string Code,
    string? Description);

public record UpdateFruitTypeRequest(
    string? Name,
    string? Code,
    string? Description,
    string? Status);