namespace DoAnV2.Application.Features.MasterData.Dtos;

public record DistributorDto(
    Guid Id,
    string Code,
    string Name,
    string Phone,
    string? Email,
    string Address,
    string? TaxCode,
    string Status,
    Guid? RetailerId = null);

public record CreateDistributorRequest(
    string Code,
    string Name,
    string Phone,
    string? Email,
    string Address,
    string? TaxCode);

public record SearchRetailerResultDto(
    Guid RetailerId,
    string FullName,
    string Email,
    string Phone,
    string? WalletAddress,
    bool IsLinked,
    Guid? DistributorId);
