using DoAnV2.Domain.Common;
using DoAnV2.Domain.Enums;

namespace DoAnV2.Domain.Entities;

/// <summary>Bảng trung gian N-N: Batch - User .
/// BR-03: Worker chỉ thao tác trên lô được phân công.</summary>
public class BatchWorker : BaseEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Cờ đại diện - chỉ 1 user/Batch (BR-06).</summary>
    public bool IsRepresentative { get; set; }

    public DateTime AssignedDate { get; set; }

    /// <summary>PENDING → ACCEPTED khi worker gọi acceptBatch.</summary>
    public WorkerAssignmentStatus Status { get; set; } = WorkerAssignmentStatus.PENDING;
}