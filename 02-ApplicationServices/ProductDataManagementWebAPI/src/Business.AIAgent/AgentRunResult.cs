namespace Business.AIAgent;

public sealed class AgentRunResult
{
    public bool IsSuccess { get; init; }
    public string Response { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public int Iterations { get; init; }

    public static AgentRunResult Success(string response, int iterations) =>
        new() { IsSuccess = true, Response = response, Iterations = iterations };

    public static AgentRunResult Failure(string error) =>
        new() { IsSuccess = false, Response = string.Empty, ErrorMessage = error };
}
