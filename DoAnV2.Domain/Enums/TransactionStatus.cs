namespace DoAnV2.Domain.Enums;

public enum TransactionStatus
{
    PENDING = 0,        // Đang chờ gửi lên Blockchain
    SUCCESS = 1,        // Giao dịch thành công
    FAILED = 2          // Giao dịch thất bại - cho phép retry
}