namespace Business.AIAgent;

public sealed class AgentContext
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public Guid TenantId { get; init; }
    public Guid UserId { get; init; }
    public Guid? ProjectId { get; init; }
    public int Depth { get; init; } = 0;
    public string? BearerToken { get; init; }

    /// <summary>Streaming callback — null means non-streaming run.</summary>
    public Func<AgentStreamEvent, CancellationToken, Task>? OnEvent { get; init; }

    public AgentContext CreateSubAgentContext() =>
        new()
        {
            SessionId = SessionId,
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = ProjectId,
            Depth = Depth + 1,
            BearerToken = BearerToken,
            OnEvent = OnEvent
        };
}
