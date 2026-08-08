namespace DoAnV2.Domain.Enums;

public enum AssetType
{
    PARENT = 1,     // Parent Batch (gọi hàm *Parent trên Blockchain)
    SUB = 2         // SubBatch (gọi hàm *Sub trên Blockchain)
}