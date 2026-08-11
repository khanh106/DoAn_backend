namespace DoAnV2.Application.Features.Admin.Blockchain.Dtos;

/// <summary>
/// TASK 11 - Mục 11.2: DTO cho danh sách giao dịch Blockchain (trang giám sát của Admin).
/// </summary>
public record BlockchainTransactionDto(
    Guid Id,
    Guid? BatchId,
    string? BatchCode,
    Guid? SubBatchId,
    string? SubBatchCode,
    string WalletAddress,
    string TransactionHash,
    string ContractAddress,
    string FunctionName,
    long? BlockNumber,
    DateTime Timestamp,
    string Status,
    string? ErrorMessage);

/// <summary>
/// TASK 11 - Mục 11.2: DTO trả về sau khi Retry thành công.
/// </summary>
public record RetryTransactionResultDto(
    Guid TransactionId,
    string FunctionName,
    string OldTransactionHash,
    string NewTransactionHash,
    long? NewBlockNumber,
    string Status,
    DateTime RetriedAt);

/// <summary>
/// TASK 11 - Mục 11.2: DTO trả về sau khi gán / thu hồi role on-chain cho một địa chỉ ví.
/// </summary>
public record WhitelistRoleResultDto(
    string RoleName,
    string AccountAddress,
    string Action,         // "GRANT" | "REVOKE"
    string TransactionHash,
    DateTime ExecutedAt);
