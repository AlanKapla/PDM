using System.Diagnostics;
using Business.AIAgent.Configuration;
using Business.AIAgent.Interfaces;
using Business.AIAgent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.AIAgent.Services;

/// <summary>
/// Main agent execution loop
/// Calls LLM, executes tools, and repeats until completion
/// </summary>
public sealed class AgentRunner : IAgentRunner
{
    private readonly IAzureOpenAIClient llmClient;
    private readonly AzureOpenAISettings settings;
    private readonly ILogger<AgentRunner> logger;

    public AgentRunner(
        IAzureOpenAIClient llmClient,
        IOptions<AzureOpenAISettings> options,
        ILogger<AgentRunner> logger)
    {
        this.llmClient = llmClient;
        settings = options.Value;
        this.logger = logger;
    }

    public async Task<AgentRunResult> RunAsync(
        List<LLMMessage> messages,
        List<ITool> tools,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new AgentRunResult
        {
            ConversationHistory = new List<LLMMessage>(messages),
            Success = false
        };

        try
        {
            // Build tool definitions
            var toolDefinitions = tools.Select(t => new ToolDefinition
            {
                Type = "function",
                Function = new FunctionDefinition
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = t.GetParametersSchema()
                }
            }).ToList();

            logger.LogInformation("Starting agent run with {ToolCount} tools, max iterations: {MaxIterations}",
                tools.Count, settings.MaxIterations);

            // Main loop
            for (int iteration = 0; iteration < settings.MaxIterations; iteration++)
            {
                result.IterationCount = iteration + 1;

                logger.LogDebug("Agent iteration {Iteration}/{MaxIterations}", iteration + 1, settings.MaxIterations);

                // Prepare LLM request
                var llmRequest = new LLMRequest
                {
                    Messages = new List<LLMMessage>(result.ConversationHistory),
                    Tools = toolDefinitions.Count > 0 ? toolDefinitions : null,
                    MaxTokens = settings.MaxTokens,
                    Temperature = settings.Temperature,
                    TopP = settings.TopP
                };

                // Call LLM
                var llmResponse = await llmClient.GetCompletionAsync(llmRequest, cancellationToken);
                result.LLMResponses.Add(llmResponse);

                // Check for errors
                if (!string.IsNullOrEmpty(llmResponse.Error))
                {
                    result.Error = llmResponse.Error;
                    logger.LogError("LLM returned error: {Error}", llmResponse.Error);
                    break;
                }

                // Track token usage
                if (llmResponse.Usage != null)
                {
                    result.TotalTokensUsed += llmResponse.Usage.TotalTokens;
                }

                // Add assistant message to conversation
                if (llmResponse.Message != null)
                {
                    result.ConversationHistory.Add(llmResponse.Message);
                    result.FinalMessage = llmResponse.Message;
                }

                // Check finish reason
                if (llmResponse.FinishReason == FinishReason.Stop)
                {
                    // Natural completion - we're done
                    logger.LogInformation("Agent completed successfully after {Iterations} iterations", iteration + 1);
                    result.Success = true;
                    break;
                }

                if (llmResponse.FinishReason == FinishReason.Length)
                {
                    logger.LogWarning("Agent reached token limit at iteration {Iteration}", iteration + 1);
                    result.Error = "Reached maximum token limit";
                    break;
                }

                if (llmResponse.FinishReason == FinishReason.ContentFilter)
                {
                    logger.LogWarning("Content filtered at iteration {Iteration}", iteration + 1);
                    result.Error = "Content was filtered by safety system";
                    break;
                }

                if (llmResponse.FinishReason == FinishReason.ToolCalls)
                {
                    // Execute tool calls
                    if (llmResponse.Message?.ToolCalls == null || llmResponse.Message.ToolCalls.Count == 0)
                    {
                        logger.LogWarning("Finish reason was ToolCalls but no tool calls present");
                        result.Error = "Invalid tool call response";
                        break;
                    }

                    logger.LogInformation("Executing {ToolCallCount} tool calls", llmResponse.Message.ToolCalls.Count);

                    // Execute all tool calls in parallel
                    var toolTasks = llmResponse.Message.ToolCalls.Select(async toolCall =>
                    {
                        // Validate tool call ID
                        if (string.IsNullOrWhiteSpace(toolCall.Id))
                        {
                            logger.LogError("Tool call has empty ID for tool: {ToolName}", toolCall.Function.Name);
                            return ToolResult.Failure(
                                "unknown",
                                toolCall.Function.Name,
                                $"Tool call ID is missing or empty",
                                0);
                        }

                        var tool = tools.FirstOrDefault(t => t.Name == toolCall.Function.Name);

                        if (tool == null)
                        {
                            logger.LogError("Tool not found: {ToolName}", toolCall.Function.Name);
                            return ToolResult.Failure(
                                toolCall.Id,
                                toolCall.Function.Name,
                                $"Tool '{toolCall.Function.Name}' not found",
                                0);
                        }

                        logger.LogDebug("Executing tool: {ToolName} with ID: {ToolCallId} and arguments: {Arguments}",
                            tool.Name, toolCall.Id, toolCall.Function.Arguments);

                        var toolStopwatch = Stopwatch.StartNew();
                        try
                        {
                            var toolResult = await tool.ExecuteAsync(toolCall.Function.Arguments, cancellationToken);
                            toolStopwatch.Stop();

                            // Ensure the tool result has the correct tool call ID
                            if (string.IsNullOrWhiteSpace(toolResult.ToolCallId))
                            {
                                logger.LogWarning("Tool {ToolName} returned empty ToolCallId, setting it to: {ToolCallId}",
                                    tool.Name, toolCall.Id);
                                toolResult.ToolCallId = toolCall.Id;
                            }

                            logger.LogInformation("Tool {ToolName} (ID: {ToolCallId}) executed successfully in {Time}ms",
                                tool.Name, toolCall.Id, toolStopwatch.ElapsedMilliseconds);

                            return toolResult;
                        }
                        catch (Exception ex)
                        {
                            toolStopwatch.Stop();
                            logger.LogError(ex, "Tool {ToolName} (ID: {ToolCallId}) execution failed",
                                tool.Name, toolCall.Id);

                            return ToolResult.Failure(
                                toolCall.Id,
                                tool.Name,
                                $"Tool execution error: {ex.Message}",
                                toolStopwatch.ElapsedMilliseconds);
                        }
                    });

                    var toolResults = await Task.WhenAll(toolTasks);
                    result.ToolResults.AddRange(toolResults);

                    // Add tool results to conversation
                    foreach (var toolResult in toolResults)
                    {
                        // Validate tool call ID before creating tool message
                        if (string.IsNullOrWhiteSpace(toolResult.ToolCallId))
                        {
                            logger.LogError("ToolResult has empty ToolCallId for tool: {ToolName}. Skipping this tool message.",
                                toolResult.ToolName);
                            result.Error = $"Tool {toolResult.ToolName} returned empty ToolCallId";
                            result.Success = false;
                            break;
                        }

                        var toolMessage = LLMMessage.Tool(
                            toolResult.ToolCallId,
                            toolResult.ToolName,
                            toolResult.IsSuccess ? toolResult.Content : $"Error: {toolResult.Error}");

                        result.ConversationHistory.Add(toolMessage);
                        
                        logger.LogDebug("Added tool message to conversation: Tool={ToolName}, ToolCallId={ToolCallId}, Success={Success}",
                            toolResult.ToolName, toolResult.ToolCallId, toolResult.IsSuccess);
                    }

                    // If we encountered an error with tool call IDs, stop the loop
                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        break;
                    }

                    // Continue loop to get LLM's response to tool results
                    continue;
                }

                // Unknown finish reason
                logger.LogWarning("Unknown finish reason: {FinishReason}", llmResponse.FinishReason);
                result.Error = $"Unknown finish reason: {llmResponse.FinishReason}";
                break;
            }

            // Check if we hit max iterations
            if (result.IterationCount >= settings.MaxIterations && !result.Success)
            {
                logger.LogWarning("Agent reached maximum iterations ({MaxIterations}) without completion", settings.MaxIterations);
                result.Error = "Reached maximum number of iterations";
            }

            stopwatch.Stop();
            result.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            logger.LogInformation("Agent run completed. Success: {Success}, Iterations: {Iterations}, Tokens: {Tokens}, Time: {Time}ms",
                result.Success, result.IterationCount, result.TotalTokensUsed, result.TotalExecutionTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Error = $"Unexpected error: {ex.Message}";
            logger.LogError(ex, "Unexpected error in agent run");
            return result;
        }
    }
}
