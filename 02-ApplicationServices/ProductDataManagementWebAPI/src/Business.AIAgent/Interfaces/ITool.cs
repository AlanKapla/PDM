using Business.AIAgent.Models;

namespace Business.AIAgent.Interfaces;

/// <summary>
/// Represents a tool that can be called by the AI agent
/// Tools are injected via DI and discovered automatically
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique name of the tool
    /// Must match the name in function calls from LLM
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what the tool does
    /// Used by LLM to decide when to use this tool
    /// </summary>
    string Description { get; }

    /// <summary>
    /// JSON Schema describing the tool's parameters
    /// Used by LLM to generate valid function calls
    /// Example:
    /// {
    ///   "type": "object",
    ///   "properties": {
    ///     "location": { "type": "string", "description": "City name" },
    ///     "unit": { "type": "string", "enum": ["celsius", "fahrenheit"] }
    ///   },
    ///   "required": ["location"]
    /// }
    /// </summary>
    object GetParametersSchema();

    /// <summary>
    /// Executes the tool with given arguments
    /// </summary>
    /// <param name="arguments">JSON-encoded arguments from LLM</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    Task<ToolResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base abstract class for tools with common functionality
/// </summary>
public abstract class ToolBase : ITool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract object GetParametersSchema();
    public abstract Task<ToolResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Helper to create a function definition for this tool
    /// </summary>
    public ToolDefinition ToToolDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = GetParametersSchema()
            }
        };
    }
}
