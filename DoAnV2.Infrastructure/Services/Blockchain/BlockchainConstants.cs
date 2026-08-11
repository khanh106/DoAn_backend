namespace DoAnV2.Infrastructure.Services.Blockchain;

/// <summary>
/// Tên role on-chain khớp với hằng số trong FruitTraceability.sol.
/// Mỗi role = keccak256(roleName).
/// </summary>
public static class BlockchainRoleNames
{
    public const string Farmer = "FARMER_ROLE";
    public const string Processor = "PROCESSOR_ROLE";
    public const string Retailer = "RETAILER_ROLE";

    /// <summary>Map tên role app (FARMER/PROCESSOR/RETAILER/FARMER_ROLE) ➔ tên hằng số SC.</summary>
    public static string FromAppRole(string appRoleName)
    {
        if (string.IsNullOrWhiteSpace(appRoleName)) 
            return Farmer;

        var upper = appRoleName.Trim().ToUpperInvariant();
        
        return upper switch
        {
            "FARMER" or "FARMER_ROLE" => Farmer,
            "PROCESSOR" or "PROCESSOR_ROLE" => Processor,
            "RETAILER" or "RETAILER_ROLE" => Retailer,
            _ => upper.EndsWith("_ROLE", StringComparison.OrdinalIgnoreCase) ? upper : upper + "_ROLE"
        };
    }
}

/// <summary>
/// Tên hàm Smart Contract - dùng để ghi log vào cột FunctionName
/// trong bảng BlockchainTransaction (TASK 03 - Mục 3.3).
/// </summary>
public static class BlockchainFunctionNames
{
    // Role
    public const string GrantRole = "grantRole";
    public const string RevokeRole = "revokeRole";

    // Batch
    public const string CreateBatch = "createBatch";
    public const string AssignWorker = "assignWorker";
    public const string SetRepresentative = "setRepresentative";
    public const string AcceptBatch = "acceptBatch";

    // Stages
    public const string HarvestBatch = "harvestBatch";
    public const string ReceiveBatch = "receiveBatch";
    public const string ProcessBatch = "processBatch";
    public const string ClassifyOnlyBatch = "classifyOnlyBatch";
    public const string SplitBatch = "splitBatch";

    // Inspection / Packaging / Shipping / Retailer
    public const string InspectParent = "inspectParent";
    public const string InspectSub = "inspectSub";
    public const string PackageParent = "packageParent";
    public const string PackageSub = "packageSub";
    public const string ShipParent = "shipParent";
    public const string ShipSub = "shipSub";
    public const string ReceiveParent = "receiveParent";
    public const string ReceiveSub = "receiveSub";
    public const string ReadyParent = "readyParent";
    public const string ReadySub = "readySub";

    // Wallet (ETH transfer, không qua SC)
    public const string EthFundFarmer = "eth_fund_farmer";
    public const string EthSweepFarmer = "eth_sweep_farmer";
}

/// <summary>
/// Sentinel contractAddress dùng cho các giao dịch ETH transfer (không qua Smart Contract).
/// </summary>
public static class EthTransferSentinel
{
    public const string ContractAddress = "ETH_TRANSFER";
}
