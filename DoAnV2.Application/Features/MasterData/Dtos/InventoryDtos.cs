using DoAnV2.Domain.Enums;

namespace DoAnV2.Application.Features.MasterData.Dtos;

public record InventoryLogDto(
    Guid Id,
    Guid MaterialItemId,
    string MaterialName,
    InventoryTransactionType TransactionType,
    double Quantity,
    DateTime TransactionDate,
    Guid UserId,
    string UserFullName,
    string? Note);

public record CreateInventoryTransactionRequest(
    Guid MaterialItemId,
    InventoryTransactionType TransactionType,
    double Quantity,
    string? Note);

public record StockItemDto(
    Guid MaterialItemId,
    string Code,
    string Name,
    ItemType ItemType,
    string Unit,
    double QuantityInStock);