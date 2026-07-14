using Business.AIAgent.Abstractions;

namespace Business.AIAgent.Core;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools;

    public ToolRegistry(IEnumerable<IAgentTool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
    }

    public IAgentTool? Get(string toolName) =>
        _tools.TryGetValue(toolName, out IAgentTool? tool) ? tool : null;

    public IReadOnlyList<IAgentTool> GetAllowedTools(IEnumerable<string> allowedNames) =>
        allowedNames
            .Select(name => _tools.TryGetValue(name, out IAgentTool? t) ? t : null)
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();
}
