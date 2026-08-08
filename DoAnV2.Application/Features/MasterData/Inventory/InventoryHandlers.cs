using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.MasterData.Dtos;
using DoAnV2.Application.Features.MasterData.FruitTypes;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;

namespace DoAnV2.Application.Features.MasterData.Inventory;

/// <summary>
/// Tạo giao dịch nhập/xuất kho:
///   IMPORT ➔ tăng QuantityInStock.
///   EXPORT ➔ giảm QuantityInStock (không cho âm).
/// Đồng thời sinh InventoryLog.
/// </summary>
public class CreateInventoryTransactionCommandHandler
    : IRequestHandler<CreateInventoryTransactionCommand, InventoryLogDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateInventoryTransactionCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<InventoryLogDto> Handle(CreateInventoryTransactionCommand req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);
        var userId = _currentUser.UserId!.Value;

        if (req.Quantity <= 0)
            throw new ValidationException("Số lượng giao dịch phải > 0.");

        var material = await _uow.MaterialItems.GetByIdAsync(req.MaterialItemId, ct)
            ?? throw new NotFoundException($"Không tìm thấy MaterialItem {req.MaterialItemId}.");

        if (material.ProcessorId != processorId)
            throw new ForbiddenException("Vật tư này không thuộc Processor của bạn.");

        if (req.TransactionType == InventoryTransactionType.EXPORT
            && material.QuantityInStock < req.Quantity)
        {
            throw new ValidationException(
                $"Tồn kho không đủ để xuất. Hiện có: {material.QuantityInStock} {material.Unit}.");
        }

        // Cập nhật số lượng tồn kho
        material.QuantityInStock = req.TransactionType == InventoryTransactionType.IMPORT
            ? material.QuantityInStock + req.Quantity
            : material.QuantityInStock - req.Quantity;

        var log = new InventoryLog
        {
            MaterialItemId = material.Id,
            TransactionType = req.TransactionType,
            Quantity = req.Quantity,
            TransactionDate = DateTime.UtcNow,
            UserId = userId,
            Note = req.Note,
        };

        await _uow.InventoryLogs.AddAsync(log, ct);
        _uow.MaterialItems.Update(material);
        await _uow.SaveChangesAsync(ct);

        return new InventoryLogDto(
            log.Id, log.MaterialItemId, material.Name,
            log.TransactionType, log.Quantity, log.TransactionDate,
            log.UserId, _currentUser.Email ?? string.Empty, log.Note);
    }
}

public class GetInventoryLogsQueryHandler
    : IRequestHandler<GetInventoryLogsQuery, IReadOnlyList<InventoryLogDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetInventoryLogsQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InventoryLogDto>> Handle(GetInventoryLogsQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);
        var logs = await _uow.InventoryLogs.GetLogsAsync(processorId, req.MaterialItemId, ct);
        return logs.Select(l => new InventoryLogDto(
            l.Id, l.MaterialItemId, l.MaterialItem.Name,
            l.TransactionType, l.Quantity, l.TransactionDate,
            l.UserId, l.User.FullName, l.Note)).ToList();
    }
}

public class GetInventoryStockQueryHandler
    : IRequestHandler<GetInventoryStockQuery, IReadOnlyList<StockItemDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetInventoryStockQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StockItemDto>> Handle(GetInventoryStockQuery req, CancellationToken ct)
    {
        var processorId = ProcessorGuard.RequireProcessor(_currentUser);
        var items = await _uow.MaterialItems.GetByProcessorIdAsync(processorId, ct);
        return items.Select(m => new StockItemDto(
            m.Id, m.Code, m.Name, m.ItemType, m.Unit, m.QuantityInStock)).ToList();
    }
}