using Business.AIAgent;
using Business.AIAgent.Abstractions;
using Business.AIAgent.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace WebApi.Hubs;

[Authorize]
public sealed class AIHub : Hub<IAIClient>
{
    private readonly IAgentRunner _runner;
    private readonly AzureAIAgentOptions _options;
    private readonly ILogger<AIHub> _logger;

    public AIHub(
        IAgentRunner runner,
        IOptions<AzureAIAgentOptions> options,
        ILogger<AIHub> logger)
    {
        _runner = runner;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Starts an agent run with streaming. Events are pushed via IAIClient callbacks.
    /// </summary>
    /// <param name="agentName">Name of the agent to run (e.g. "main-orchestrator")</param>
    /// <param name="message">User message / task description</param>
    /// <param name="projectId">Optional project scope (UUID string)</param>
    public async Task RunAgent(string agentName, string message, string? projectId = null)
    {
        string sessionId = Guid.NewGuid().ToString();
        string connectionId = Context.ConnectionId;

        Guid tenantId = GetTenantId();
        Guid userId = GetUserId();
        Guid? parsedProjectId = Guid.TryParse(projectId, out Guid pid) ? pid : null;
        string? bearerToken = Context.GetHttpContext()?.Request.Headers.Authorization
            .ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation(
            "AIHub.RunAgent: agent={AgentName}, session={SessionId}, user={UserId}",
            agentName, sessionId, userId);

        IAIClient caller = Clients.Caller;

        AgentContext context = new()
        {
            SessionId = sessionId,
            TenantId = tenantId,
            UserId = userId,
            ProjectId = parsedProjectId,
            BearerToken = bearerToken,
            OnEvent = async (evt, ct) =>
            {
                switch (evt.Type)
                {
                    case AgentStreamEventType.Token:
                        await caller.OnToken(evt.Content ?? string.Empty, evt.SessionId);
                        break;
                    case AgentStreamEventType.ToolCallStart:
                        await caller.OnToolCallStart(evt.ToolName ?? string.Empty, evt.SessionId);
                        break;
                    case AgentStreamEventType.ToolCallResult:
                        await caller.OnToolCallResult(evt.ToolName ?? string.Empty, evt.Content ?? string.Empty, evt.SessionId);
                        break;
                    case AgentStreamEventType.SubAgentStart:
                        await caller.OnSubAgentStart(evt.AgentName ?? string.Empty, evt.SessionId);
                        break;
                    case AgentStreamEventType.SubAgentComplete:
                        await caller.OnSubAgentComplete(evt.AgentName ?? string.Empty, evt.SessionId);
                        break;
                    case AgentStreamEventType.Complete:
                        await caller.OnComplete(evt.SessionId);
                        break;
                    case AgentStreamEventType.Error:
                        await caller.OnError(evt.Content ?? string.Empty, evt.SessionId);
                        break;
                }
            }
        };

        try
        {
            await foreach (AgentStreamEvent evt in _runner.RunStreamingAsync(agentName, message, context))
            {
                if (context.OnEvent is not null)
                {
                    await context.OnEvent(evt, CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AIHub agent run failed: agent={AgentName}, session={SessionId}", agentName, sessionId);
            await caller.OnError(ex.Message, sessionId);
        }
    }

    private Guid GetUserId()
    {
        string? sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }

    private Guid GetTenantId()
    {
        string? tenantClaim = Context.User?.FindFirstValue("tenantId")
                              ?? Context.User?.FindFirstValue("tid");
        return Guid.TryParse(tenantClaim, out Guid id) ? id : Guid.Empty;
    }
}
