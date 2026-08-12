using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace DoAnV2.Infrastructure.Services.Blockchain;

/// <summary>
/// Triển khai IBlockchainService bằng Nethereum Web3 (TASK 03).
///
/// Smart contract: FruitTraceability.sol (Sepolia / Hardhat local).
/// Toàn bộ method đều:
///   1. Tạo BlockchainTransaction PENDING.
///   2. Gọi hàm SC, nhận transactionHash.
///   3. Chờ receipt (Status 1 = SUCCESS, 0 = FAILED).
///   4. Cập nhật DB (BR-42: không rollback nghiệp vụ khi FAILED).
///
/// Signer mặc định = Admin private key (deploy contract).
/// Một số method (AcceptBatch, HarvestBatch) nhận `signerPrivateKey` riêng
/// vì contract yêu cầu msg.sender = worker / representative.
/// </summary>
public class BlockchainService : IBlockchainService
{
    private readonly BlockchainOptions _options;
    private readonly WalletFundingOptions _walletFunding;
    private readonly WalletOptions _walletOptions;
    private readonly IRecordBlockchainTransactionService _recorder;
    private readonly IWalletService _walletService;
    private readonly AbiLoader _abiLoader;
    private readonly ILogger<BlockchainService> _logger;

    public BlockchainService(
        IOptions<BlockchainOptions> options,
        IOptions<WalletFundingOptions> walletFunding,
        IOptions<WalletOptions> walletOptions,
        IRecordBlockchainTransactionService recorder,
        IWalletService walletService,
        AbiLoader abiLoader,
        ILogger<BlockchainService> logger)
    {
        _options = options.Value;
        _walletFunding = walletFunding.Value;
        _walletOptions = walletOptions.Value;
        _recorder = recorder;
        _walletService = walletService;
        _abiLoader = abiLoader;
        _logger = logger;
    }

    // =============================================================
    // PUBLIC API: WALLET FUNDING (TASK 02)
    // =============================================================

    public async Task<string?> FundFarmerWalletAsync(
        string farmerWalletAddress,
        decimal amountEth,
        CancellationToken ct = default)
    {
        if (!_walletFunding.Enabled)
        {
            _logger.LogInformation("FundFarmerWallet skipped: WalletFunding.Enabled=false");
            return null;
        }
        if (string.IsNullOrWhiteSpace(farmerWalletAddress))
            throw new ValidationException("farmerWalletAddress rỗng.");
        if (amountEth <= 0)
            throw new ValidationException("amountEth phải > 0.");

        var adminKey = _options.AdminPrivateKey
            ?? throw new InvalidOperationException("Blockchain:AdminPrivateKey chưa được cấu hình.");

        var web3 = CreateWeb3WithAdminKey().web3;
        var adminAddress = new EthECKey(adminKey).GetPublicAddress();

        var pending = await _recorder.RecordPendingAsync(
            functionName: BlockchainFunctionNames.EthFundFarmer,
            walletAddress: adminAddress,
            contractAddress: EthTransferSentinel.ContractAddress,
            ct: ct);

        try
        {
            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(farmerWalletAddress, amountEth, null);

            await _recorder.RecordSuccessAsync(pending, receipt.TransactionHash, (long)receipt.BlockNumber.Value, ct);
            return receipt.TransactionHash;
        }
        catch (Exception ex)
        {
            await _recorder.RecordFailedAsync(pending, ex.Message, null, ct);
            throw;
        }
    }

    public async Task<string?> SweepFarmerWalletAsync(
        string farmerWalletAddress,
        string? farmerEncryptedPrivateKey = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(farmerWalletAddress))
            throw new ValidationException("farmerWalletAddress rỗng.");

        // Cần private key để ký. Caller (SweepFarmerWalletCommandHandler) đã truyền
        // EncryptedPrivateKey của user; ta decrypt ở đây.
        if (string.IsNullOrWhiteSpace(farmerEncryptedPrivateKey))
            throw new ValidationException("farmerEncryptedPrivateKey rỗng - cần private key để sweep ETH.");

        var farmerPrivKey = _walletService.DecryptPrivateKey(
            farmerEncryptedPrivateKey, _walletOptions.EncryptionKey);

        // Đo số dư hiện tại
        var rpcClient = new Nethereum.JsonRpc.Client.RpcClient(new Uri(_options.RpcUrl));
        var tempWeb3 = new Web3(rpcClient);
        var balanceWei = await tempWeb3.Eth.GetBalance.SendRequestAsync(farmerWalletAddress);
        var balanceEth = Web3.Convert.FromWei(balanceWei.Value);

        var minKeep = _walletFunding.MinFarmerBalanceToKeep;
        var sweepAmount = balanceEth - minKeep;
        if (sweepAmount <= 0)
        {
            _logger.LogInformation(
                "Sweep skipped: balance {Balance} ETH <= MinKeep {Min} ETH",
                balanceEth, minKeep);
            return null;
        }

        var recipient = !string.IsNullOrWhiteSpace(_walletFunding.SweepRecipientAddress)
            ? _walletFunding.SweepRecipientAddress
            : new EthECKey(_options.AdminPrivateKey).GetPublicAddress();

        var web3 = CreateWeb3WithKey(farmerPrivKey).web3;
        var pending = await _recorder.RecordPendingAsync(
            functionName: BlockchainFunctionNames.EthSweepFarmer,
            walletAddress: farmerWalletAddress,
            contractAddress: EthTransferSentinel.ContractAddress,
            ct: ct);

        try
        {
            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(recipient, sweepAmount, null);

            await _recorder.RecordSuccessAsync(pending, receipt.TransactionHash, (long)receipt.BlockNumber.Value, ct);
            return receipt.TransactionHash;
        }
        catch (Exception ex)
        {
            await _recorder.RecordFailedAsync(pending, ex.Message, null, ct);
            throw;
        }
    }

    // =============================================================
    // PUBLIC API: ROLE MANAGEMENT
    // =============================================================

    public Task<string> GrantRoleAsync(
        string roleName,
        string accountAddress,
        string? signerPrivateKey = null,
        CancellationToken ct = default)
        => SendRoleCallAsync(
            BlockchainFunctionNames.GrantRole,
            roleName,
            accountAddress,
            signerPrivateKey,
            batchId: null, subBatchId: null, ct: ct);

    public Task<string> RevokeRoleAsync(
        string roleName,
        string accountAddress,
        string? signerPrivateKey = null,
        CancellationToken ct = default)
        => SendRoleCallAsync(
            BlockchainFunctionNames.RevokeRole,
            roleName,
            accountAddress,
            signerPrivateKey,
            batchId: null, subBatchId: null, ct: ct);

    // =============================================================
    // PUBLIC API: BATCH & ASSIGN
    // =============================================================

    public Task<string> CreateBatchAsync(
        string batchId, string batchCode, string fruitType,
        string metadataURI, string dataHash,
        string? signerPrivateKey = null,
        CancellationToken ct = default)
    {
        string? signerAddress = null;
        if (!string.IsNullOrWhiteSpace(signerPrivateKey))
        {
            signerAddress = new EthECKey(signerPrivateKey).GetPublicAddress();
        }

        return SendSimpleAsync(
            BlockchainFunctionNames.CreateBatch, batchId, null,
            () => BuildBatchInputs(batchId, batchCode, fruitType, metadataURI, dataHash),
            signerPrivateKey: signerPrivateKey,
            signerAddress: signerAddress,
            ct: ct);
    }

    public Task<string> AssignWorkerAsync(
        string batchId, string workerAddress, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.AssignWorker, batchId, null,
            () => new object[] { SmartContractIds.CodeToBytes32(batchId), workerAddress },
            ct: ct);

    public Task<string> SetRepresentativeAsync(
        string batchId, string repAddress, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.SetRepresentative, batchId, null,
            () => new object[] { SmartContractIds.CodeToBytes32(batchId), repAddress },
            ct: ct);

    public async Task<string> AcceptBatchAsync(
        string batchId, string workerPrivateKey, CancellationToken ct = default)
    {
        var workerAddress = new EthECKey(workerPrivateKey).GetPublicAddress();
        return await SendSimpleAsync(
            BlockchainFunctionNames.AcceptBatch, batchId, null,
            () => new object[] { SmartContractIds.CodeToBytes32(batchId) },
            signerPrivateKey: workerPrivateKey,
            signerAddress: workerAddress,
            ct: ct);
    }

    // =============================================================
    // PUBLIC API: STAGES
    // =============================================================

    public async Task<string> HarvestBatchAsync(
        string batchId, string metadataURI, string dataHash,
        string signerPrivateKey, CancellationToken ct = default)
    {
        var signerAddress = new EthECKey(signerPrivateKey).GetPublicAddress();
        return await SendSimpleAsync(
            BlockchainFunctionNames.HarvestBatch, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: signerAddress,
            ct: ct);
    }

        public Task<string> ReceiveBatchAsync(
        string batchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ReceiveBatch, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);


    public Task<string> ProcessBatchAsync(
        string batchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ProcessBatch, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    // =============================================================
    // PUBLIC API: SORTING
    // =============================================================

    public Task<string> ClassifyOnlyBatchAsync(
        string batchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ClassifyOnlyBatch, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    public Task<string> SplitBatchAsync(
        string batchId, string[] subBatchIds, string[] metadataURIs, string[] dataHashes,
        string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.SplitBatch, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                subBatchIds.Select(SmartContractIds.CodeToBytes32).ToArray(),
                metadataURIs,
                dataHashes.Select(h => SmartContractIds.HexToBytes32(h) ?? SmartContractIds.CodeToBytes32(h)).ToArray(),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    // =============================================================
    // PUBLIC API: INSPECTION
    // =============================================================

    public Task<string> InspectParentAsync(
        string batchId, bool passed, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.InspectParent, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                passed,
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    public Task<string> InspectSubAsync(
        string subBatchId, bool passed, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.InspectSub, null, subBatchId,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(subBatchId),
                passed,
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    // =============================================================
    // PUBLIC API: PACKAGING
    // =============================================================

    public Task<string> PackageParentAsync(
        string batchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.PackageParent, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    public Task<string> PackageSubAsync(
        string subBatchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.PackageSub, null, subBatchId,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(subBatchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    // =============================================================
    // PUBLIC API: SHIPPING
    // =============================================================

    public Task<string> ShipParentAsync(
        string batchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ShipParent, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    public Task<string> ShipSubAsync(
        string subBatchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ShipSub, null, subBatchId,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(subBatchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey,
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null,
            ct: ct);

    // =============================================================
    // PUBLIC API: RETAILER
    // =============================================================

        public Task<string> ReceiveParentAsync(
        string batchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ReceiveParent, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey, // <-- Bổ sung truyền key ký
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null, // <-- Bổ sung địa chỉ ví tương ứng
            ct: ct);

    public Task<string> ReceiveSubAsync(
        string subBatchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ReceiveSub, null, subBatchId,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(subBatchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey, // <-- Bổ sung truyền key ký
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null, // <-- Bổ sung địa chỉ ví tương ứng
            ct: ct);

    public Task<string> ReadyParentAsync(
        string batchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ReadyParent, batchId, null,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(batchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey, // <-- Bổ sung truyền key ký
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null, // <-- Bổ sung địa chỉ ví tương ứng
            ct: ct);

    public Task<string> ReadySubAsync(
        string subBatchId, string metadataURI, string dataHash, string? signerPrivateKey = null, CancellationToken ct = default)
        => SendSimpleAsync(
            BlockchainFunctionNames.ReadySub, null, subBatchId,
            () => new object[]
            {
                SmartContractIds.CodeToBytes32(subBatchId),
                metadataURI,
                SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
            },
            signerPrivateKey: signerPrivateKey, // <-- Bổ sung truyền key ký
            signerAddress: !string.IsNullOrWhiteSpace(signerPrivateKey) ? new EthECKey(signerPrivateKey).GetPublicAddress() : null, // <-- Bổ sung địa chỉ ví tương ứng
            ct: ct);


    // =============================================================
    // CORE: GỬI TRANSACTION QUA Nethereum + GHI LOG
    // =============================================================

    /// <summary>
    /// Helper chính: gọi 1 hàm SC, ghi PENDING ➔ SUCCESS/FAILED.
    /// </summary>
        /// <summary>
    /// Helper chính: gọi 1 hàm SC, ghi PENDING ➔ SUCCESS/FAILED.
    /// </summary>
    private async Task<string> SendSimpleAsync(
        string functionName,
        string? batchId,
        string? subBatchId,
        Func<object[]> buildInputs,
        string? signerPrivateKey = null,
        string? signerAddress = null,
        CancellationToken ct = default)
    {
        var abi = _abiLoader.LoadAbi();

        // 1. Tạo Web3 + xác định signer
        var (web3, fromAddress) = CreateWeb3(signerPrivateKey, signerAddress);

        // 2. Ghi PENDING
        var pending = await _recorder.RecordPendingAsync(
            functionName: functionName,
            walletAddress: fromAddress,
            contractAddress: _options.ContractAddress,
            batchId: TryParseGuid(batchId),
            subBatchId: TryParseGuid(subBatchId),
            ct: ct);

        try
        {
            // 3. Gọi hàm SC
            var contract = web3.Eth.GetContract(abi, _options.ContractAddress);
            var function = contract.GetFunction(functionName);
            var inputs = buildInputs();
            var txHash = await function.SendTransactionAsync(
                from: fromAddress,
                gas: GetGasForFunction(functionName),
                value: new HexBigInteger(0),
                functionInput: inputs);

            pending.TransactionHash = txHash;

            // 4. Chờ receipt (Polling Retry 2 giây/lần để Infura/Sepolia kịp đào block)
            Nethereum.RPC.Eth.DTOs.TransactionReceipt? receipt = null;
            var timeoutSeconds = _walletFunding.ReceiptTimeoutSeconds > 0 ? _walletFunding.ReceiptTimeoutSeconds : 60;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < timeout)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
                    if (receipt != null) break;
                }
                catch
                {
                    // Khi tx mới vào Mempool chưa được đào, Infura sẽ quăng lỗi "Internal error: eth_getTransactionReceipt"
                    // Bỏ qua ngoại lệ tạm thời này và lặp lại sau 2 giây
                }

                await Task.Delay(2000, ct);
            }

            if (receipt is null)
            {
                var msg = $"Timeout chờ receipt cho tx {txHash}.";
                await _recorder.RecordFailedAsync(pending, msg, txHash, ct);
                throw new DomainException(msg, 504);
            }

            if (receipt.Status.Value == 1)
            {
                await _recorder.RecordSuccessAsync(pending, txHash, (long)receipt.BlockNumber.Value, ct);
            }
            else
            {
                var msg = $"Transaction REVERTED (status 0). TxHash={txHash}";
                await _recorder.RecordFailedAsync(pending, msg, txHash, ct);
                throw new DomainException(msg, 400);
            }

            return txHash;
        }
        catch (Exception ex)
        {
            await _recorder.RecordFailedAsync(pending, ex.Message, pending.TransactionHash, ct);
            throw;
        }
    }

    /// <summary>Biến thể cho grantRole/revokeRole (không gắn batch/subbatch).</summary>
    private Task<string> SendRoleCallAsync(
        string functionName,
        string roleName,
        string accountAddress,
        string? signerPrivateKey,
        string? batchId,
        string? subBatchId,
        CancellationToken ct)
    {
        var roleBytes32 = SmartContractIds.CodeToBytes32(roleName);
        return SendSimpleAsync(
            functionName: functionName,
            batchId: batchId, subBatchId: subBatchId,
            buildInputs: () => new object[] { roleBytes32, accountAddress },
            signerPrivateKey: signerPrivateKey,
            signerAddress: null,
            ct: ct);
    }

    // =============================================================
    // HELPERS
    // =============================================================

    private static object[] BuildBatchInputs(
        string batchId, string batchCode, string fruitType,
        string metadataURI, string dataHash) => new object[]
    {
        SmartContractIds.CodeToBytes32(batchId),
        SmartContractIds.CodeToBytes32(batchCode),
        SmartContractIds.CodeToBytes32(fruitType),
        metadataURI,
        SmartContractIds.HexToBytes32(dataHash) ?? SmartContractIds.CodeToBytes32(dataHash),
    };

    private (Web3 web3, string fromAddress) CreateWeb3(string? privateKey, string? expectedAddress)
    {
        if (!string.IsNullOrWhiteSpace(privateKey))
            return CreateWeb3WithKey(privateKey, expectedAddress);
        return CreateWeb3WithAdminKey();
    }

    private (Web3 web3, string fromAddress) CreateWeb3WithAdminKey()
    {
        var key = _options.AdminPrivateKey
            ?? throw new InvalidOperationException("Blockchain:AdminPrivateKey chưa được cấu hình.");
        return CreateWeb3WithKey(key, null);
    }

    private (Web3 web3, string fromAddress) CreateWeb3WithKey(string privateKey, string? expectedAddress = null)
    {
        var account = new Account(privateKey, _options.ChainId);
        var web3 = new Web3(account, _options.RpcUrl);
        var address = expectedAddress ?? account.Address;
        return (web3, address);
    }

    /// <summary>
    /// Ước lượng gas theo function (Nethereum sẽ tự estimate nếu để null,
    /// nhưng trên một số testnet estimate lỗi nên ta hard-code theo pattern).
    /// </summary>
    private static HexBigInteger GetGasForFunction(string fn)
    {
        // Đặt hạn mức Gas Limit rộng rãi cho từng hàm Smart Contract
        // (Lưu ý: Lượng gas dư thừa không dùng hết sẽ được EVM tự động hoàn trả lại ví)
        //
        // ⚠️ FIX: Dùng giá trị string thực tế thay vì nameof()
        //    nameof(BlockchainFunctionNames.CreateBatch) = "CreateBatch" (C hoa)
        //    Nhưng fn nhận vào = "createBatch" (c thường)
        //    => Switch KHÔNG BAO GIỜ match => luôn rơi vào default 500k => OUT OF GAS!
        var g = fn switch
        {
            BlockchainFunctionNames.CreateBatch       => 800_000,   // "createBatch"
            BlockchainFunctionNames.SplitBatch         => 1_500_000, // "splitBatch"
            BlockchainFunctionNames.AssignWorker       => 500_000,   // "assignWorker"
            BlockchainFunctionNames.SetRepresentative  => 500_000,   // "setRepresentative"
            BlockchainFunctionNames.HarvestBatch       => 500_000,   // "harvestBatch"
            BlockchainFunctionNames.ReceiveBatch       => 500_000,   // "receiveBatch"
            BlockchainFunctionNames.ProcessBatch       => 500_000,   // "processBatch"
            BlockchainFunctionNames.ClassifyOnlyBatch  => 500_000,   // "classifyOnlyBatch"
            BlockchainFunctionNames.InspectParent      => 500_000,   // "inspectParent"
            BlockchainFunctionNames.InspectSub         => 500_000,   // "inspectSub"
            BlockchainFunctionNames.PackageParent      => 500_000,   // "packageParent"
            BlockchainFunctionNames.PackageSub         => 500_000,   // "packageSub"
            BlockchainFunctionNames.ShipParent         => 500_000,   // "shipParent"
            BlockchainFunctionNames.ShipSub            => 500_000,   // "shipSub"
            BlockchainFunctionNames.ReceiveParent      => 500_000,   // "receiveParent"
            BlockchainFunctionNames.ReceiveSub         => 500_000,   // "receiveSub"
            BlockchainFunctionNames.ReadyParent        => 500_000,   // "readyParent"
            BlockchainFunctionNames.ReadySub           => 500_000,   // "readySub"
            _ => 500_000,
        };
        return new HexBigInteger(g);
    }



    private static Guid? TryParseGuid(string? s)
        => Guid.TryParse(s, out var g) ? g : null;
}
