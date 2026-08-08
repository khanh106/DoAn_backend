namespace DoAnV2.Infrastructure.Options;

public class BlockchainOptions
{
    public const string SectionName = "Blockchain";

    public string RpcUrl { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public string AdminPrivateKey { get; set; } = string.Empty;
    public long ChainId { get; set; } = 11155111; // Sepolia default
    public string AbiPath { get; set; } = string.Empty;
}