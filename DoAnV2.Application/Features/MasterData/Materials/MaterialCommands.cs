using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.Materials;

// ===== Commands =====
public record CreateMaterialCommand(
    ItemType ItemType,
    string Code,
    string Name,
    string Unit,
    decimal Price,
    double? DosagePerHa,
    double? Concentration,
    string? Supplier,
    string? NPKRatio,
    string? Note) : IRequest<MaterialItemDto>;

public record UpdateMaterialCommand(
    Guid Id,
    string? Name,
    string? Unit,
    decimal? Price,
    double? DosagePerHa,
    double? Concentration,
    string? Supplier,
    string? NPKRatio,
    string? Note) : IRequest<MaterialItemDto>;

// ===== Queries =====
public record GetMaterialsQuery : IRequest<IReadOnlyList<MaterialItemDto>>;