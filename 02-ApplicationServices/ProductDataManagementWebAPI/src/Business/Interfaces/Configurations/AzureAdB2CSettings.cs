namespace Business.Interfaces.Configurations;

public class AzureAdB2CSettings
{
    public const string SectionName = "AzureAdB2C";

    public string Instance { get; set; } = null!;
    public string Domain { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string? ClientSecret { get; set; }
}
