namespace Business.AIAgent.Configuration;

public sealed class AzureAIAgentOptions
{
    public const string SectionName = "AzureAIAgent";

    public string Endpoint { get; set; } = default!;

    /// <summary>Azure AD credential is used when ApiKey is empty (Managed Identity / DefaultAzureCredential).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Default deployment name (e.g. gpt-4o).</summary>
    public string DefaultDeployment { get; set; } = "gpt-4o";

    public int MaxSubAgentDepth { get; set; } = 3;
}
