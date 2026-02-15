using Microsoft.Extensions.Logging;

namespace Business.AIAgent.Plugins.Base;

/// <summary>
/// Base class for all plugins with common functionality
/// </summary>
public abstract class BasePlugin
{
    protected ILogger Logger { get; }

    protected BasePlugin(ILogger logger)
    {
        Logger = logger;
    }

    protected void LogFunctionInvocation(string functionName, params object[] parameters)
    {
        Logger.LogInformation(
            "Invoking function {FunctionName} with {ParameterCount} parameters",
            functionName,
            parameters.Length);
    }

    protected void LogFunctionResult(string functionName, object? result)
    {
        Logger.LogInformation(
            "Function {FunctionName} completed successfully",
            functionName);
    }

    protected void LogFunctionError(string functionName, Exception ex)
    {
        Logger.LogError(
            ex,
            "Error in function {FunctionName}",
            functionName);
    }
}
