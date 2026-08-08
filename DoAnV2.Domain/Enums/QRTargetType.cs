namespace DoAnV2.Domain.Enums;

public enum QRTargetType
{
    BATCH = 1,          // QR cho Batch gốc (Parent)
    SUBBATCH = 2,       // QR cho SubBatch (lô con sau phân loại)
    BOX = 3,            // QR cho thùng
    COMMERCIAL = 4      // QR cho hộp thương mại
}