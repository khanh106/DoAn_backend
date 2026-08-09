using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoAnV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai IShipmentRepository (TASK 09).
/// BR-18: Retailer chỉ xác nhận nhận khi lô ở STAGE_SHIPPING.
/// </summary>
public class ShipmentRepository : IShipmentRepository
{
    private readonly ApplicationDbContext _db;

    public ShipmentRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Shipments
            .Include(x => x.Batch)
            .Include(x => x.SubBatch)
                .ThenInclude(sb => sb!.ParentBatch)
            .Include(x => x.Retailer)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Shipment>> GetByRetailerIdAsync(
        Guid retailerId, CancellationToken ct = default)
    {
        return await _db.Shipments
            .Include(x => x.Batch)
            .Include(x => x.SubBatch)
                .ThenInclude(sb => sb!.ParentBatch)
            .Where(x => x.RetailerId == retailerId)
            .OrderByDescending(x => x.ShippingDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Shipment>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default)
    {
        return await _db.Shipments
            .Include(x => x.Retailer)
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.ShippingDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Shipment>> GetBySubBatchIdAsync(
        Guid subBatchId, CancellationToken ct = default)
    {
        return await _db.Shipments
            .Include(x => x.Retailer)
            .Where(x => x.SubBatchId == subBatchId)
            .OrderByDescending(x => x.ShippingDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Shipment entity, CancellationToken ct = default)
        => await _db.Shipments.AddAsync(entity, ct);

    public void Update(Shipment entity) => _db.Shipments.Update(entity);
}