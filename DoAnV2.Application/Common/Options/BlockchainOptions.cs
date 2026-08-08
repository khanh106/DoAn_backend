namespace DoAnV2.Application.Common.Options;

public class BlockchainOptions
{
    public const string SectionName = "Blockchain";

    /// <summary>RPC URL (Infura / Alchemy / Hardhat local). VD: https://sepolia.infura.io/v3/&lt;id&gt;.</summary>
    public string RpcUrl { get; set; } = string.Empty;

    /// <summary>Địa chỉ Smart Contract FruitTraceability đã deploy.</summary>
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>Admin private key (hex có/không 0x). Dùng để ký các tx grantRole, createBatch, etc.</summary>
    public string AdminPrivateKey { get; set; } = string.Empty;

    /// <summary>Chain ID (11155111 = Sepolia, 31337 = Hardhat local, 80001 = Mumbai).</summary>
    public long ChainId { get; set; } = 11155111;

    /// <summary>Đường dẫn tới file ABI JSON (relative tới ContentRootPath).</summary>
    public string AbiPath { get; set; } = "Contracts/abi.json";
}
