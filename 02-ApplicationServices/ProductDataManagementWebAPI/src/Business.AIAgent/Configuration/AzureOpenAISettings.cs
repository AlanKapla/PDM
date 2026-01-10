namespace Business.AIAgent.Configuration;

/// <summary>
/// Configuration settings for Azure OpenAI service
/// </summary>
public sealed class AzureOpenAISettings
{
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// Azure OpenAI endpoint URL (e.g., https://your-resource.openai.azure.com/)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// API Key for authentication (alternative to Managed Identity)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Deployment name for the model (e.g., gpt-4o, gpt-35-turbo)
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of tokens in the response
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Temperature for response generation (0.0 - 2.0)
    /// Lower values = more deterministic, Higher values = more creative
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Top-p sampling (0.0 - 1.0)
    /// Alternative to temperature for controlling randomness
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Maximum number of iterations for agent runner loop
    /// Prevents infinite loops when using tools
    /// </summary>
    public int MaxIterations { get; set; } = 10;

    /// <summary>
    /// Timeout for single API call in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to use Managed Identity for authentication
    /// If true, ApiKey is ignored
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
}
