using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Features.Admin.Blockchain.Dtos;
using DoAnV2.Domain.Entities;
using DoAnV2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoAnV2.Application.Features.Admin.Blockchain.Commands;

/// <summary>
/// TASK 11 - Mục 11.2: Handler thực thi Retry theo BR-42.
///   1. Đọc BlockchainTransaction bị FAILED.
///   2. Validate: Tx phải FAILED, và dữ liệu off-chain + IPFS vẫn còn (BR-42).
///   3. Tuỳ FunctionName, dispatch lại đúng hàm IBlockchainService với cùng
///      (metadataURI, dataHash) đã upload trước đó.
///   4. Cập nhật TransactionHash mới + Status = SUCCESS.
///
/// BR-20: KHÔNG xoá bản ghi BlockchainTransaction cũ.
/// </summary>
public class RetryBlockchainTransactionCommandHandler
    : IRequestHandler<RetryBlockchainTransactionCommand, RetryTransactionResultDto>
{
    // ========== FunctionName constants (mirror BlockchainFunctionNames) ==========
    private const string FN_CreateBatch = "createBatch";
    private const string FN_AssignWorker = "assignWorker";
    private const string FN_SetRepresentative = "setRepresentative";
    private const string FN_AcceptBatch = "acceptBatch";
    private const string FN_HarvestBatch = "harvestBatch";
    private const string FN_ReceiveBatch = "receiveBatch";
    private const string FN_ProcessBatch = "processBatch";
    private const string FN_ClassifyOnlyBatch = "classifyOnlyBatch";
    private const string FN_SplitBatch = "splitBatch";
    private const string FN_InspectParent = "inspectParent";
    private const string FN_InspectSub = "inspectSub";
    private const string FN_PackageParent = "packageParent";
    private const string FN_PackageSub = "packageSub";
    private const string FN_ShipParent = "shipParent";
    private const string FN_ShipSub = "shipSub";
    private const string FN_ReceiveParent = "receiveParent";
    private const string FN_ReceiveSub = "receiveSub";
    private const string FN_ReadyParent = "readyParent";
    private const string FN_ReadySub = "readySub";
    private const string FN_GrantRole = "grantRole";
    private const string FN_RevokeRole = "revokeRole";
    private const string FN_EthFundFarmer = "eth_fund_farmer";
    private const string FN_EthSweepFarmer = "eth_sweep_farmer";

    private readonly IUnitOfWork _uow;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<RetryBlockchainTransactionCommandHandler> _logger;

    public RetryBlockchainTransactionCommandHandler(
        IUnitOfWork uow,
        IBlockchainService blockchain,
        ILogger<RetryBlockchainTransactionCommandHandler> logger)
    {
        _uow = uow;
        _blockchain = blockchain;
        _logger = logger;
    }

    public async Task<RetryTransactionResultDto> Handle(
        RetryBlockchainTransactionCommand req, CancellationToken ct)
    {
        // ========== 1. Đọc Tx cũ ==========
        var failedTx = await _uow.BlockchainTransactions.GetByIdAsync(req.TransactionId, ct)
            ?? throw new NotFoundException($"Không tìm thấy BlockchainTransaction {req.TransactionId}.");

        if (failedTx.Status != TransactionStatus.FAILED)
            throw new ValidationException(
                $"Chỉ retry được Tx ở trạng thái FAILED. Tx hiện ở {failedTx.Status}.");

        var fn = failedTx.FunctionName;
        var oldHash = failedTx.TransactionHash;

        _logger.LogInformation(
            "Admin Retry: txId={Id}, fn={Fn}, oldHash={OldHash}",
            failedTx.Id, fn, oldHash);

        // ========== 2-3. Dispatch theo FunctionName ==========
        string newTxHash = fn switch
        {
            // Stages - Parent
            FN_CreateBatch        => await RetryCreateBatchAsync(failedTx, ct),
            FN_AssignWorker       => await RetryAssignWorkerAsync(failedTx, ct),
            FN_SetRepresentative  => await RetrySetRepresentativeAsync(failedTx, ct),
            FN_HarvestBatch       => await RetryHarvestBatchAsync(failedTx, ct),
            FN_ReceiveBatch       => await RetryReceiveBatchAsync(failedTx, ct),
            FN_ProcessBatch       => await RetryProcessBatchAsync(failedTx, ct),
            FN_ClassifyOnlyBatch  => await RetryClassifyOnlyBatchAsync(failedTx, ct),
            FN_SplitBatch         => await RetrySplitBatchAsync(failedTx, ct),
            FN_InspectParent      => await RetryInspectParentAsync(failedTx, ct),
            FN_PackageParent      => await RetryPackageParentAsync(failedTx, ct),
            FN_ShipParent         => await RetryShipParentAsync(failedTx, ct),
            FN_ReceiveParent      => await RetryReceiveParentAsync(failedTx, ct),
            FN_ReadyParent        => await RetryReadyParentAsync(failedTx, ct),

            // Stages - Sub
            FN_InspectSub         => await RetryInspectSubAsync(failedTx, ct),
            FN_PackageSub         => await RetryPackageSubAsync(failedTx, ct),
            FN_ShipSub            => await RetryShipSubAsync(failedTx, ct),
            FN_ReceiveSub         => await RetryReceiveSubAsync(failedTx, ct),
            FN_ReadySub           => await RetryReadySubAsync(failedTx, ct),

            // Role management (TASK 03)
            FN_GrantRole          => await RetryGrantRoleAsync(failedTx, ct),
            FN_RevokeRole         => await RetryRevokeRoleAsync(failedTx, ct),

            // ETH transfer (TASK 02) - không retry tự động
            FN_EthFundFarmer or FN_EthSweepFarmer
                => throw new ValidationException(
                    $"Function '{fn}' là ETH transfer, không hỗ trợ retry tự động. " +
                    "Vui lòng thực hiện thủ công (FundFarmerWallet / SweepFarmerWallet)."),

            FN_AcceptBatch
                => throw new ValidationException(
                    $"Function '{fn}' cần private key của Worker, không thể retry tự động."),

            _ => throw new ValidationException(
                $"Function '{fn}' chưa được hỗ trợ retry."),
        };

        // ========== 4. Cập nhật record BlockchainTransaction ==========
        failedTx.TransactionHash = newTxHash;
        failedTx.Status = TransactionStatus.SUCCESS;
        failedTx.ErrorMessage = null;
        failedTx.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin Retry OK: txId={Id}, fn={Fn}, newHash={NewHash}",
            failedTx.Id, fn, newTxHash);

        return new RetryTransactionResultDto(
            TransactionId: failedTx.Id,
            FunctionName: fn,
            OldTransactionHash: oldHash,
            NewTransactionHash: newTxHash,
            NewBlockNumber: failedTx.BlockNumber,
            Status: TransactionStatus.SUCCESS.ToString(),
            RetriedAt: DateTime.UtcNow);
    }

    // ============================================================
    // RETRY DISPATCHERS
    // ============================================================

    private async Task<string> RetryCreateBatchAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        RequireNotEmpty(batch.MetadataURI, batch.DataHash, "Batch.MetadataURI/DataHash");
        RequireNotEmpty(batch.BatchCode, nameof(Batch.BatchCode));

        return await _blockchain.CreateBatchAsync(
            batchId: batch.Id.ToString(),
            batchCode: batch.BatchCode,
            fruitType: batch.FruitType?.Code ?? throw new ValidationException("Batch.FruitType null."),
            metadataURI: batch.MetadataURI!,
            dataHash: batch.DataHash!,
            ct: ct);
    }

    private Task<string> RetryAssignWorkerAsync(BlockchainTransaction tx, CancellationToken ct)
        => throw new ValidationException(
            "assignWorker cần (batchId, workerAddress) - không thể retry tự động. " +
            "Vui lòng tạo lại lô hoặc xử lý thủ công.");

    private Task<string> RetrySetRepresentativeAsync(BlockchainTransaction tx, CancellationToken ct)
        => throw new ValidationException(
            "setRepresentative cần (batchId, repAddress) - không thể retry tự động. " +
            "Vui lòng xử lý thủ công.");

    private Task<string> RetryHarvestBatchAsync(BlockchainTransaction tx, CancellationToken ct)
        => throw new ValidationException(
            "harvestBatch cần private key của người đại diện - không thể retry tự động.");

    private async Task<string> RetryReceiveBatchAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var processing = batch.Processings.FirstOrDefault()
            ?? throw new ValidationException("Batch không có Processing record.");
        RequireNotEmpty(processing.MetadataURI, processing.DataHash, "Processing.MetadataURI/DataHash");

        return await _blockchain.ReceiveBatchAsync(
            batchId: batch.Id.ToString(),
            metadataURI: processing.MetadataURI!,
            dataHash: processing.DataHash!,
            ct: ct);
    }

    private async Task<string> RetryProcessBatchAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var processing = batch.Processings.FirstOrDefault()
            ?? throw new ValidationException("Batch không có Processing record.");
        RequireNotEmpty(processing.MetadataURI, processing.DataHash, "Processing.MetadataURI/DataHash");

        return await _blockchain.ProcessBatchAsync(
            batchId: batch.Id.ToString(),
            metadataURI: processing.MetadataURI!,
            dataHash: processing.DataHash!,
            ct: ct);
    }

    private async Task<string> RetryClassifyOnlyBatchAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var processing = batch.Processings.FirstOrDefault()
            ?? throw new ValidationException("Batch không có Processing record.");
        RequireNotEmpty(processing.MetadataURI, processing.DataHash, "Processing.MetadataURI/DataHash");

        return await _blockchain.ClassifyOnlyBatchAsync(
            batchId: batch.Id.ToString(),
            metadataURI: processing.MetadataURI!,
            dataHash: processing.DataHash!,
            ct: ct);
    }

    private async Task<string> RetrySplitBatchAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var subBatches = batch.SubBatches?.ToList()
            ?? throw new ValidationException("Batch không có SubBatch nào.");

        if (subBatches.Count == 0)
            throw new ValidationException("Batch không có SubBatch nào để retry splitBatch.");

        foreach (var sb in subBatches)
            RequireNotEmpty(sb.MetadataURI, sb.DataHash, $"SubBatch {sb.SubBatchCode}.MetadataURI/DataHash");

        var ids = subBatches.Select(s => s.Id.ToString()).ToArray();
        var uris = subBatches.Select(s => s.MetadataURI!).ToArray();
        var hashes = subBatches.Select(s => s.DataHash!).ToArray();

        return await _blockchain.SplitBatchAsync(
            batchId: batch.Id.ToString(),
            subBatchIds: ids,
            metadataURIs: uris,
            dataHashes: hashes,
            ct: ct);
    }

    private async Task<string> RetryInspectParentAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var inspection = batch.Inspections.FirstOrDefault()
            ?? throw new ValidationException("Batch không có Inspection record.");
        RequireNotEmpty(inspection.MetadataURI, inspection.DataHash, "Inspection.MetadataURI/DataHash");

        return await _blockchain.InspectParentAsync(
            batchId: batch.Id.ToString(),
            passed: inspection.Result == InspectionResult.PASSED,
            metadataURI: inspection.MetadataURI!,
            dataHash: inspection.DataHash!,
            ct: ct);
    }

    private async Task<string> RetryPackageParentAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var packaging = batch.Packagings.FirstOrDefault()
            ?? throw new ValidationException("Batch không có Packaging record.");
        RequireNotEmpty(packaging.MetadataURI, packaging.DataHash, "Packaging.MetadataURI/DataHash");

        return await _blockchain.PackageParentAsync(
            batchId: batch.Id.ToString(),
            metadataURI: packaging.MetadataURI!,
            dataHash: packaging.DataHash!,
            ct: ct);
    }

    private async Task<string> RetryShipParentAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var shipment = batch.Shipments.FirstOrDefault()
            ?? throw new ValidationException("Batch không có Shipment record.");
        RequireNotEmpty(shipment.MetadataURI, shipment.DataHash, "Shipment.MetadataURI/DataHash");

        var newHash = await _blockchain.ShipParentAsync(
            batchId: batch.Id.ToString(),
            metadataURI: shipment.MetadataURI!,
            dataHash: shipment.DataHash!,
            ct: ct);

        shipment.ShipTransactionHash = newHash;
        _uow.Shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return newHash;
    }

    private async Task<string> RetryReceiveParentAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var shipment = batch.Shipments.FirstOrDefault(s =>
                s.ReceiveTransactionHash == null || s.ReceiveTransactionHash == tx.TransactionHash)
            ?? batch.Shipments.FirstOrDefault()
            ?? throw new ValidationException("Batch không có Shipment record.");
        RequireNotEmpty(shipment.ReceiveMetadataURI, shipment.ReceiveDataHash,
            "Shipment.ReceiveMetadataURI/ReceiveDataHash");

        var newHash = await _blockchain.ReceiveParentAsync(
            batchId: batch.Id.ToString(),
            metadataURI: shipment.ReceiveMetadataURI!,
            dataHash: shipment.ReceiveDataHash!,
            ct: ct);

        shipment.ReceiveTransactionHash = newHash;
        _uow.Shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return newHash;
    }

    private async Task<string> RetryReadyParentAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var batch = await RequireBatchAsync(tx, ct);
        var shipment = batch.Shipments.FirstOrDefault(s =>
                s.ReadyTransactionHash == null || s.ReadyTransactionHash == tx.TransactionHash)
            ?? batch.Shipments.FirstOrDefault()
            ?? throw new ValidationException("Batch không có Shipment record.");
        RequireNotEmpty(shipment.ReadyMetadataURI, shipment.ReadyDataHash,
            "Shipment.ReadyMetadataURI/ReadyDataHash");

        var newHash = await _blockchain.ReadyParentAsync(
            batchId: batch.Id.ToString(),
            metadataURI: shipment.ReadyMetadataURI!,
            dataHash: shipment.ReadyDataHash!,
            ct: ct);

        shipment.ReadyTransactionHash = newHash;
        _uow.Shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return newHash;
    }

    private async Task<string> RetryInspectSubAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var subBatch = await RequireSubBatchAsync(tx, ct);
        var inspection = subBatch.Inspections.FirstOrDefault()
            ?? throw new ValidationException("SubBatch không có Inspection record.");
        RequireNotEmpty(inspection.MetadataURI, inspection.DataHash, "Inspection.MetadataURI/DataHash");

        return await _blockchain.InspectSubAsync(
            subBatchId: subBatch.Id.ToString(),
            passed: inspection.Result == InspectionResult.PASSED,
            metadataURI: inspection.MetadataURI!,
            dataHash: inspection.DataHash!,
            ct: ct);
    }

    private async Task<string> RetryPackageSubAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var subBatch = await RequireSubBatchAsync(tx, ct);
        var packaging = subBatch.Packagings.FirstOrDefault()
            ?? throw new ValidationException("SubBatch không có Packaging record.");
        RequireNotEmpty(packaging.MetadataURI, packaging.DataHash, "Packaging.MetadataURI/DataHash");

        return await _blockchain.PackageSubAsync(
            subBatchId: subBatch.Id.ToString(),
            metadataURI: packaging.MetadataURI!,
            dataHash: packaging.DataHash!,
            ct: ct);
    }

    private async Task<string> RetryShipSubAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var subBatch = await RequireSubBatchAsync(tx, ct);
        var shipment = subBatch.Shipments.FirstOrDefault()
            ?? throw new ValidationException("SubBatch không có Shipment record.");
        RequireNotEmpty(shipment.MetadataURI, shipment.DataHash, "Shipment.MetadataURI/DataHash");

        var newHash = await _blockchain.ShipSubAsync(
            subBatchId: subBatch.Id.ToString(),
            metadataURI: shipment.MetadataURI!,
            dataHash: shipment.DataHash!,
            ct: ct);

        shipment.ShipTransactionHash = newHash;
        _uow.Shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return newHash;
    }

    private async Task<string> RetryReceiveSubAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var subBatch = await RequireSubBatchAsync(tx, ct);
        var shipment = subBatch.Shipments.FirstOrDefault(s =>
                s.ReceiveTransactionHash == null || s.ReceiveTransactionHash == tx.TransactionHash)
            ?? subBatch.Shipments.FirstOrDefault()
            ?? throw new ValidationException("SubBatch không có Shipment record.");
        RequireNotEmpty(shipment.ReceiveMetadataURI, shipment.ReceiveDataHash,
            "Shipment.ReceiveMetadataURI/ReceiveDataHash");

        var newHash = await _blockchain.ReceiveSubAsync(
            subBatchId: subBatch.Id.ToString(),
            metadataURI: shipment.ReceiveMetadataURI!,
            dataHash: shipment.ReceiveDataHash!,
            ct: ct);

        shipment.ReceiveTransactionHash = newHash;
        _uow.Shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return newHash;
    }

    private async Task<string> RetryReadySubAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        var subBatch = await RequireSubBatchAsync(tx, ct);
        var shipment = subBatch.Shipments.FirstOrDefault(s =>
                s.ReadyTransactionHash == null || s.ReadyTransactionHash == tx.TransactionHash)
            ?? subBatch.Shipments.FirstOrDefault()
            ?? throw new ValidationException("SubBatch không có Shipment record.");
        RequireNotEmpty(shipment.ReadyMetadataURI, shipment.ReadyDataHash,
            "Shipment.ReadyMetadataURI/ReadyDataHash");

        var newHash = await _blockchain.ReadySubAsync(
            subBatchId: subBatch.Id.ToString(),
            metadataURI: shipment.ReadyMetadataURI!,
            dataHash: shipment.ReadyDataHash!,
            ct: ct);

        shipment.ReadyTransactionHash = newHash;
        _uow.Shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return newHash;
    }

    private Task<string> RetryGrantRoleAsync(BlockchainTransaction tx, CancellationToken ct)
        => throw new ValidationException(
            "grantRole không lưu (roleName, accountAddress) trong BlockchainTransaction. " +
            "Vui lòng dùng endpoint /admin/blockchain/whitelist/grant-role.");

    private Task<string> RetryRevokeRoleAsync(BlockchainTransaction tx, CancellationToken ct)
        => throw new ValidationException(
            "revokeRole không lưu (roleName, accountAddress) trong BlockchainTransaction. " +
            "Vui lòng dùng endpoint /admin/blockchain/whitelist/revoke-role.");

    // ============================================================
    // HELPERS
    // ============================================================

    private async Task<Batch> RequireBatchAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        if (tx.BatchId is null)
            throw new ValidationException(
                $"Tx {tx.Id} (fn={tx.FunctionName}) không gắn với Parent Batch.");

        var batch = await _uow.Batches.GetByIdWithFullChainAsync(tx.BatchId.Value, ct)
            ?? throw new NotFoundException($"Không tìm thấy Batch {tx.BatchId}.");

        if (batch.IsDeleted)
            throw new ValidationException($"Batch {batch.BatchCode} đã bị xoá - không thể retry.");

        return batch;
    }

    private async Task<SubBatch> RequireSubBatchAsync(BlockchainTransaction tx, CancellationToken ct)
    {
        if (tx.SubBatchId is null)
            throw new ValidationException(
                $"Tx {tx.Id} (fn={tx.FunctionName}) không gắn với SubBatch.");

        var subBatch = await _uow.SubBatches.GetByIdWithDetailsAsync(tx.SubBatchId.Value, ct)
            ?? throw new NotFoundException($"Không tìm thấy SubBatch {tx.SubBatchId}.");

        return subBatch;
    }

    private static void RequireNotEmpty(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException($"{field} trống - không thể retry (BR-42).");
    }

    private static void RequireNotEmpty(string? v1, string? v2, string fields)
    {
        if (string.IsNullOrWhiteSpace(v1) || string.IsNullOrWhiteSpace(v2))
            throw new ValidationException($"{fields} trống - không thể retry (BR-42).");
    }
}
