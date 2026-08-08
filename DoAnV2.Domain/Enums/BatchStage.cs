namespace DoAnV2.Domain.Enums;

public enum BatchStage
{
    STAGE_PLANTING = 1,            // Processor tạo lô
    STAGE_HARVESTED = 2,           // Người đại diện xác nhận thu hoạch
    STAGE_RECEIVED = 3,            // Processor tiếp nhận
    STAGE_PROCESSED = 4,           // Processor sơ chế
    STAGE_SORTED = 5,              // Phân loại (classifyOnlyBatch / splitBatch)
    INSPECTION_PASSED = 6,         // Kiểm định đạt (inspectParent / inspectSub)
    PACKAGED = 7,                  // Đóng gói (packageParent / packageSub)
    STAGE_SHIPPING = 8,            // Vận chuyển (shipParent / shipSub)
    RECEIVED_AT_RETAILER = 9,      // Cửa hàng tiếp nhận (receiveParent / receiveSub)
    READY_FOR_SALE = 10            // Sẵn sàng bán (readyParent / readySub)
}