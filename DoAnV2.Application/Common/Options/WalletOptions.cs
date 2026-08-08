namespace DoAnV2.Application.Common.Options;

public class WalletOptions
{
    public const string SectionName = "Wallet";

    public string EncryptionKey { get; set; } = string.Empty;
    public bool CustodialMode { get; set; } = true;
}
