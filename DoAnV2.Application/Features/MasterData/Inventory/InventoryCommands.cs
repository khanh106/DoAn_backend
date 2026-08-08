using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.Inventory;

// ===== Commands =====
public record CreateInventoryTransactionCommand(
    Guid MaterialItemId,
    InventoryTransactionType TransactionType,
    double Quantity,
    string? Note) : IRequest<InventoryLogDto>;

// ===== Queries =====
public record GetInventoryLogsQuery(Guid? MaterialItemId) : IRequest<IReadOnlyList<InventoryLogDto>>;

public record GetInventoryStockQuery : IRequest<IReadOnlyList<StockItemDto>>;