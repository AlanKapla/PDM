using Business.AIAgent.Abstractions;
using Business.AIAgent.Core;
using Business.AIAgent.Tools.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Business.AIAgent.Tools.SubAgent;

public sealed class CallSubAgentTool : AgentToolBase
{
    // IAgentRunner is resolved lazily to break the DI cycle:
    // AgentRunner -> IToolRegistry -> IEnumerable<IAgentTool> -> CallSubAgentTool -> IAgentRunner.
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentDefinitionLoader _loader;

    public CallSubAgentTool(IServiceProvider serviceProvider, AgentDefinitionLoader loader)
    {
        _serviceProvider = serviceProvider;
        _loader = loader;
    }

    public override string Name => "call_sub_agent";

    public override string Description =>
        "Delegates a complex task to a specialized sub-agent and returns its full response. " +
        "Use when the task requires deep domain knowledge (cost estimates, work schedule, project data).";

    public override JsonElement ParametersSchema => BuildSchema("""
        {
          "type": "object",
          "properties": {
            "agent_name": {
              "type": "string",
              "description": "Name of the sub-agent to call (e.g. cost-estimate-agent, work-schedule-agent, project-agent)"
            },
            "task": {
              "type": "string",
              "description": "Full natural language description of the task for the sub-agent"
            },
            "context": {
              "type": "string",
              "description": "Optional: additional context to pass (e.g. specific project ID, date range)"
            }
          },
          "required": ["agent_name", "task"]
        }
        """);

    public override async Task<ToolResult> ExecuteAsync(
        JsonElement arguments,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        string? agentName = GetString(arguments, "agent_name");
        string? task = GetString(arguments, "task");
        string? additionalContext = GetString(arguments, "context");

        if (string.IsNullOrWhiteSpace(agentName))
        {
            return ToolResult.Failure("agent_name is required");
        }
        if (string.IsNullOrWhiteSpace(task))
        {
            return ToolResult.Failure("task is required");
        }

        string fullTask = string.IsNullOrWhiteSpace(additionalContext)
            ? task
            : $"{task}\n\nAdditional context: {additionalContext}";

        if (context.OnEvent is not null)
        {
            await context.OnEvent(
                AgentStreamEvent.SubAgentStartEvent(agentName, context.SessionId),
                cancellationToken);
        }

        AgentContext subContext = context.CreateSubAgentContext();
        IAgentRunner runner = _serviceProvider.GetRequiredService<IAgentRunner>();
        AgentRunResult result = await runner.RunAsync(agentName, fullTask, subContext, cancellationToken);

        if (context.OnEvent is not null)
        {
            await context.OnEvent(
                AgentStreamEvent.SubAgentCompleteEvent(agentName, context.SessionId),
                cancellationToken);
        }

        return result.IsSuccess
            ? ToolResult.Success(result.Response)
            : ToolResult.Failure($"Sub-agent '{agentName}' failed: {result.ErrorMessage}");
    }
}
