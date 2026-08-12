namespace DoAnV2.Application.Common.Interfaces;

/// <summary>
/// Tổng hợp các thao tác on-chain trên Smart Contract FruitTraceability
/// (TASK 02: Fund/Sweep + TASK 03: Role/Stage).
///
/// Tất cả method trả về TransactionHash (string). Trả về `null` cho 2 method
/// `FundFarmerWalletAsync` / `SweepFarmerWalletAsync` khi bị NoOp (ví dụ:
/// WalletFunding.Enabled=false, hoặc không tìm thấy user...).
/// </summary>
public interface IBlockchainService
{
    // ============================================================
    // WALLET FUNDING (TASK 02 - ETH Transfer thuần, không qua SC)
    // ============================================================

    /// <summary>
    /// Cấp ETH cho ví Custodial Wallet của Farmer (Admin ví gửi ➔ Farmer ví nhận).
    /// BR-46.2: Ví Farmer được Admin tài trợ gas fee.
    /// Trả về transaction hash (null nếu NoOp).
    /// </summary>
    Task<string?> FundFarmerWalletAsync(
        string farmerWalletAddress,
        decimal amountEth,
        CancellationToken ct = default);

    /// <summary>
    /// Thu hồi toàn bộ ETH (trừ MinFarmerBalanceToKeep) từ ví Farmer về ví Admin.
    /// BR-46.2: Cơ chế thu hồi khi ví không sử dụng / user bị REJECT / admin bấm Sweep.
    /// Trả về transaction hash (null nếu NoOp hoặc không có gì để sweep).
    /// </summary>
    Task<string?> SweepFarmerWalletAsync(
        string farmerWalletAddress,
        string? farmerEncryptedPrivateKey = null,
        CancellationToken ct = default);

    // ============================================================
    // ROLE MANAGEMENT (TASK 03 - Mục 3.2)
    // ============================================================

    /// <summary>Cấp role on-chain. roleName: "FARMER_ROLE" / "PROCESSOR_ROLE" / "RETAILER_ROLE".</summary>
    Task<string> GrantRoleAsync(
        string roleName,
        string accountAddress,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    /// <summary>Thu hồi role on-chain.</summary>
    Task<string> RevokeRoleAsync(
        string roleName,
        string accountAddress,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    // ============================================================
    // BATCH & ASSIGN (TASK 03)
    // ============================================================

    Task<string> CreateBatchAsync(
        string batchId,
        string batchCode,
        string fruitType,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    Task<string> AssignWorkerAsync(
        string batchId,
        string workerAddress,
        CancellationToken ct = default);

    Task<string> SetRepresentativeAsync(
        string batchId,
        string repAddress,
        CancellationToken ct = default);

    Task<string> AcceptBatchAsync(
        string batchId,
        string workerPrivateKey,
        CancellationToken ct = default);

    // ============================================================
    // STAGES (TASK 03)
    // ============================================================

    Task<string> HarvestBatchAsync(
        string batchId,
        string metadataURI,
        string dataHash,
        string signerPrivateKey,
        CancellationToken ct = default);

    Task<string> ReceiveBatchAsync(
        string batchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    Task<string> ProcessBatchAsync(
        string batchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    // ============================================================
    // SORTING (TASK 03)
    // ============================================================

    Task<string> ClassifyOnlyBatchAsync(
        string batchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    Task<string> SplitBatchAsync(
        string batchId,
        string[] subBatchIds,
        string[] metadataURIs,
        string[] dataHashes,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    // ============================================================
    // INSPECTION (TASK 03)
    // ============================================================

    Task<string> InspectParentAsync(
        string batchId,
        bool passed,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    Task<string> InspectSubAsync(
        string subBatchId,
        bool passed,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    // ============================================================
    // PACKAGING (TASK 03)
    // ============================================================

    Task<string> PackageParentAsync(
        string batchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    Task<string> PackageSubAsync(
        string subBatchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    // ============================================================
    // SHIPPING (TASK 03)
    // ============================================================

    Task<string> ShipParentAsync(
        string batchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    Task<string> ShipSubAsync(
        string subBatchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default);

    // ============================================================
    // RETAILER (TASK 03)
    // ============================================================

        Task<string> ReceiveParentAsync(
        string batchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null, 
        CancellationToken ct = default);

    Task<string> ReceiveSubAsync(
        string subBatchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null, 
        CancellationToken ct = default);

    Task<string> ReadyParentAsync(
        string batchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null, 
        CancellationToken ct = default);

    Task<string> ReadySubAsync(
        string subBatchId,
        string metadataURI,
        string dataHash,
        string? signerPrivateKey = null, 
        CancellationToken ct = default);

}
