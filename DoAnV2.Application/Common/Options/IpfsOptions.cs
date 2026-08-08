namespace DoAnV2.Application.Common.Options;

public class IpfsOptions
{
    public const string SectionName = "Ipfs";

    public string ApiUrl { get; set; } = string.Empty;
    public string GatewayUrl { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
