using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>
/// Thực thể quản lý liên kết giữa Hợp tác xã (Processor) và Công nhân (Farmer).
/// </summary>
public class ProcessorWorker : BaseEntity
{
    public Guid ProcessorId { get; set; }
    public User Processor { get; set; } = null!;

    public Guid WorkerId { get; set; }
    public User Worker { get; set; } = null!;

    public CoopWorkerLinkStatus Status { get; set; } = CoopWorkerLinkStatus.PENDING;
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}
