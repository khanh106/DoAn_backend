using DoAnV2.Domain.Entities;

namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Repository cho bảng Shipment - Vận đơn giao hàng tới Retailer (TASK 09).
///   - Processor tạo shipment cho Parent/Sub batch (shipParent / shipSub).
///   - Retailer xác nhận nhận hàng (receiveParent / receiveSub) và đưa lên kệ (readyParent / readySub).
/// BR-18: Retailer chỉ được nhận khi lô ở STAGE_SHIPPING.
/// </summary>
public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Shipment>> GetByRetailerIdAsync(
        Guid retailerId, CancellationToken ct = default);

    Task<IReadOnlyList<Shipment>> GetByBatchIdAsync(
        Guid batchId, CancellationToken ct = default);

    Task<IReadOnlyList<Shipment>> GetBySubBatchIdAsync(
        Guid subBatchId, CancellationToken ct = default);

    Task AddAsync(Shipment entity, CancellationToken ct = default);

    /// <summary>Đánh dấu entity đã thay đổi - dùng cho EF Core change tracking.</summary>
    void Update(Shipment entity);
}