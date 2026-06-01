namespace Business.AIAgent.Abstractions;

public interface IToolRegistry
{
    IAgentTool? Get(string toolName);
    IReadOnlyList<IAgentTool> GetAllowedTools(IEnumerable<string> allowedNames);
}
