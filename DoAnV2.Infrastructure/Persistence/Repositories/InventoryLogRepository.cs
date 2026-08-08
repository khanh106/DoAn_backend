using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

public class InventoryLogRepository : IInventoryLogRepository
{
    private readonly ApplicationDbContext _db;

    public InventoryLogRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(InventoryLog entity, CancellationToken ct = default)
        => await _db.InventoryLogs.AddAsync(entity, ct);

    public async Task<IReadOnlyList<InventoryLog>> GetLogsAsync(Guid processorId, Guid? materialItemId = null, CancellationToken ct = default)
    {
        var query = _db.InventoryLogs
            .Include(x => x.MaterialItem)
            .Include(x => x.User)
            .Where(x => x.MaterialItem.ProcessorId == processorId);

        if (materialItemId.HasValue)
            query = query.Where(x => x.MaterialItemId == materialItemId.Value);

        return await query
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(ct);
    }
}