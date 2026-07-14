namespace Business.AIAgent;

public sealed class ToolResult
{
    public bool IsSuccess { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    public static ToolResult Success(string content) =>
        new() { IsSuccess = true, Content = content };

    public static ToolResult Failure(string error) =>
        new() { IsSuccess = false, Content = string.Empty, ErrorMessage = error };
}
