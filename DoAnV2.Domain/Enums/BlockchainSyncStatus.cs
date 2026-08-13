namespace DoAnV2.Domain.Enums;

/// <summary>
/// Trạng thái đồng bộ blockchain của một Batch.
/// PENDING: lô vừa tạo off-chain, chưa push on-chain.
/// CONFIRMED: đã push createBatch + assignWorker + setRepresentative thành công.
/// FAILED: push on-chain thất bại (sẽ retry ở background).
/// </summary>
public enum BlockchainSyncStatus
{
    PENDING = 0,
    CONFIRMED = 1,
    FAILED = 2,
}