using Business.AIAgent.Abstractions;
using System.Text.Json;

namespace Business.AIAgent.Tools.Base;

public abstract class AgentToolBase : IAgentTool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract JsonElement ParametersSchema { get; }
    public abstract Task<ToolResult> ExecuteAsync(JsonElement arguments, AgentContext context, CancellationToken cancellationToken = default);

    protected static string? GetString(JsonElement args, string key)
    {
        if (args.TryGetProperty(key, out JsonElement prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    protected static Guid? GetGuid(JsonElement args, string key)
    {
        string? value = GetString(args, key);
        return Guid.TryParse(value, out Guid result) ? result : null;
    }

    protected static int GetInt(JsonElement args, string key, int defaultValue = 0)
    {
        if (args.TryGetProperty(key, out JsonElement prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt32();
        }
        return defaultValue;
    }

    protected static JsonElement BuildSchema(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();
}
