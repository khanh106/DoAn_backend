namespace DoAnV2.Application.Features.MasterData.Dtos;

public record FarmAreaDto(
    Guid Id,
    string Name,
    string OwnerName,
    string Province,
    string District,
    string Ward,
    double Area,
    string? SoilType,
    string? GPS,
    string? PlantingCode,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateFarmAreaRequest(
    string Name,
    string OwnerName,
    string Province,
    string District,
    string Ward,
    double Area,
    string? SoilType,
    string? GPS,
    string? PlantingCode);

public record UpdateFarmAreaRequest(
    string? Name,
    string? OwnerName,
    string? Province,
    string? District,
    string? Ward,
    double? Area,
    string? SoilType,
    string? GPS,
    string? PlantingCode);