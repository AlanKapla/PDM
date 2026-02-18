using Microsoft.Extensions.Logging;
using Business.AIAgent.Core;
using Business.AIAgent.Models;
using System.Runtime.CompilerServices;

namespace Business.AIAgent.Services;

public sealed class AgentService : IAgentService
{
    private readonly IKernelOrchestrator _orchestrator;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IKernelOrchestrator orchestrator,
        ILogger<AgentService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<AgentResponse> ProcessRequestAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing agent request for tenant {TenantId}",
                request.TenantId);

            var arguments = request.Context ?? new Dictionary<string, object>();
            arguments["TenantId"] = request.TenantId;

            string result;

            if (request.EnableTools)
            {
                result = await _orchestrator.ExecuteWithToolsAsync(
                    request.Prompt,
                    arguments,
                    request.SystemPrompt,
                    cancellationToken);
            }
            else
            {
                result = await _orchestrator.ExecutePromptAsync(
                    request.Prompt,
                    arguments,
                    request.SystemPrompt,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Agent request processed successfully for tenant {TenantId}",
                request.TenantId);

            return AgentResponse.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing agent request for tenant {TenantId}",
                request.TenantId);

            return AgentResponse.Error(ex.Message);
        }
    }

    public async IAsyncEnumerable<string> ProcessRequestStreamingAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing streaming agent request for tenant {TenantId}",
            request.TenantId);

        var arguments = request.Context ?? new Dictionary<string, object>();
        arguments["TenantId"] = request.TenantId;

        await foreach (var chunk in _orchestrator.ExecutePromptStreamingAsync(
            request.Prompt,
            arguments,
            request.SystemPrompt,
            cancellationToken))
        {
            yield return chunk;
        }

        _logger.LogInformation(
            "Streaming agent request completed for tenant {TenantId}",
            request.TenantId);
    }
}
