using System.Text.Json;

namespace Business.AIAgent.Abstractions;

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    JsonElement ParametersSchema { get; }
    Task<ToolResult> ExecuteAsync(JsonElement arguments, AgentContext context, CancellationToken cancellationToken = default);
}
