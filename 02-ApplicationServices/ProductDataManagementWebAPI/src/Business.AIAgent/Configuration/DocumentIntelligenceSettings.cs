namespace Business.AIAgent.Configuration;

/// <summary>
/// Configuration settings for Azure Document Intelligence service
/// </summary>
public sealed class DocumentIntelligenceSettings
{
    public const string SectionName = "DocumentIntelligence";

    /// <summary>
    /// Azure Document Intelligence endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// API Key for authentication (alternative to Managed Identity)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Whether to use Managed Identity for authentication
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Model ID to use for document analysis (e.g., prebuilt-receipt, prebuilt-invoice)
    /// Default: prebuilt-receipt for receipts and invoices
    /// </summary>
    public string ModelId { get; set; } = "prebuilt-receipt";
}
