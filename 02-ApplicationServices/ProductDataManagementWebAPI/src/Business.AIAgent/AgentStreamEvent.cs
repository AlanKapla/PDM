namespace Business.AIAgent;

public enum AgentStreamEventType
{
    Token,
    ToolCallStart,
    ToolCallResult,
    SubAgentStart,
    SubAgentComplete,
    Complete,
    Error
}

public sealed class AgentStreamEvent
{
    public AgentStreamEventType Type { get; init; }
    public string? Content { get; init; }
    public string? ToolName { get; init; }
    public string? AgentName { get; init; }
    public string SessionId { get; init; } = string.Empty;

    public static AgentStreamEvent TokenEvent(string token, string sessionId) =>
        new() { Type = AgentStreamEventType.Token, Content = token, SessionId = sessionId };

    public static AgentStreamEvent ToolCallStartEvent(string toolName, string sessionId) =>
        new() { Type = AgentStreamEventType.ToolCallStart, ToolName = toolName, SessionId = sessionId };

    public static AgentStreamEvent ToolCallResultEvent(string toolName, string result, string sessionId) =>
        new() { Type = AgentStreamEventType.ToolCallResult, ToolName = toolName, Content = result, SessionId = sessionId };

    public static AgentStreamEvent SubAgentStartEvent(string agentName, string sessionId) =>
        new() { Type = AgentStreamEventType.SubAgentStart, AgentName = agentName, SessionId = sessionId };

    public static AgentStreamEvent SubAgentCompleteEvent(string agentName, string sessionId) =>
        new() { Type = AgentStreamEventType.SubAgentComplete, AgentName = agentName, SessionId = sessionId };

    public static AgentStreamEvent CompleteEvent(string sessionId) =>
        new() { Type = AgentStreamEventType.Complete, SessionId = sessionId };

    public static AgentStreamEvent ErrorEvent(string error, string sessionId) =>
        new() { Type = AgentStreamEventType.Error, Content = error, SessionId = sessionId };
}
