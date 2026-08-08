using DoAnV2.Application.Features.MasterData.Dtos;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.FarmAreas;

// ===== Commands =====
public record CreateFarmAreaCommand(
    string Name,
    string OwnerName,
    string Province,
    string District,
    string Ward,
    double Area,
    string? SoilType,
    string? GPS,
    string? PlantingCode) : IRequest<FarmAreaDto>;

public record UpdateFarmAreaCommand(
    Guid Id,
    string? Name,
    string? OwnerName,
    string? Province,
    string? District,
    string? Ward,
    double? Area,
    string? SoilType,
    string? GPS,
    string? PlantingCode) : IRequest<FarmAreaDto>;

// ===== Queries =====
public record GetFarmAreasQuery(
    string? Province,
    string? District,
    string? Ward,
    string? PlantingCode) : IRequest<IReadOnlyList<FarmAreaDto>>;

public record GetFarmAreaByIdQuery(Guid Id) : IRequest<FarmAreaDto>;