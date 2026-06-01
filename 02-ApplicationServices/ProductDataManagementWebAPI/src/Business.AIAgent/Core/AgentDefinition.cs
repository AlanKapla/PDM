namespace Business.AIAgent.Core;

public sealed class AgentDefinition
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string Model { get; init; } = "gpt-4o";
    public float Temperature { get; init; } = 0.7f;
    public int MaxTokens { get; init; } = 4096;
    public int MaxIterations { get; init; } = 10;
    public IReadOnlyList<string> Tools { get; init; } = [];
    public IReadOnlyList<string> SubAgents { get; init; } = [];
    public string SystemPrompt { get; init; } = default!;
}
