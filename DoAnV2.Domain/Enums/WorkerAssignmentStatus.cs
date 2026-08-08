namespace DoAnV2.Domain.Enums;

public enum WorkerAssignmentStatus
{
    PENDING = 0,        // Mới được phân công, chờ worker xác nhận
    ACCEPTED = 1,       // Worker đã xác nhận nhận lô (acceptBatch)
    REJECTED = 2        // Worker từ chối nhận lô
}