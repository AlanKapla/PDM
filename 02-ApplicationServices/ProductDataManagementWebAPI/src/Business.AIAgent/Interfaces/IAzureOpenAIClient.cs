using Business.AIAgent.Models;

namespace Business.AIAgent.Interfaces;

/// <summary>
/// Client for communicating with Azure OpenAI service
/// Abstracts the underlying SDK for easier testing and flexibility
/// </summary>
public interface IAzureOpenAIClient
{
    /// <summary>
    /// Sends a completion request to Azure OpenAI
    /// </summary>
    /// <param name="request">LLM request with messages and tools</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LLM response</returns>
    Task<LLMResponse> GetCompletionAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a completion response (for future use)
    /// </summary>
    /// <param name="request">LLM request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async stream of response chunks</returns>
    IAsyncEnumerable<LLMResponse> GetCompletionStreamAsync(LLMRequest request, CancellationToken cancellationToken = default);
}
