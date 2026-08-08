namespace DoAnV2.Domain.Enums;

public enum InspectionResult
{
    PENDING = 0,            // Chờ kiểm định
    PASSED = 1,             // Đạt - tiếp tục đóng gói
    FAILED = 2              // Không đạt - dừng quy trình
}