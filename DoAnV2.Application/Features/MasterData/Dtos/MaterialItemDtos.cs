using DoAnV2.Domain.Enums;

namespace DoAnV2.Application.Features.MasterData.Dtos;

public record MaterialItemDto(
    Guid Id,
    ItemType ItemType,
    string Code,
    string Name,
    string Unit,
    decimal Price,
    double QuantityInStock,
    double? DosagePerHa,
    double? Concentration,
    string? Supplier,
    string? NPKRatio,
    string? Note);

public record CreateMaterialRequest(
    ItemType ItemType,
    string Code,
    string Name,
    string Unit,
    decimal Price,
    double? DosagePerHa,
    double? Concentration,
    string? Supplier,
    string? NPKRatio,
    string? Note);

public record UpdateMaterialRequest(
    string? Name,
    string? Unit,
    decimal? Price,
    double? DosagePerHa,
    double? Concentration,
    string? Supplier,
    string? NPKRatio,
    string? Note);