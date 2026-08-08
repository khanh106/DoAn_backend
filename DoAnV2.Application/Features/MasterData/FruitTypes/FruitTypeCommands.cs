using DoAnV2.Application.Features.MasterData.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.FruitTypes;

// ===== Commands =====
public record CreateFruitTypeCommand(
    string Name,
    string Code,
    string? Description) : IRequest<FruitTypeDto>;

public record UpdateFruitTypeCommand(
    Guid Id,
    string? Name,
    string? Code,
    string? Description,
    string? Status) : IRequest<FruitTypeDto>;

// ===== Queries =====
public record GetFruitTypesQuery : IRequest<IReadOnlyList<FruitTypeDto>>;