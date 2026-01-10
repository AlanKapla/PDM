using Business.AIAgent.Interfaces;
using Business.AIAgent.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Business.AIAgent.Services;

/// <summary>
/// Orchestrates agent execution by selecting tools and managing prompts
/// Entry point for CQRS Command Handlers
/// </summary>
public sealed class Orchestrator : IOrchestrator
{
    private readonly IAgentRunner agentRunner;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<Orchestrator> logger;

    public Orchestrator(
        IAgentRunner agentRunner,
        IServiceProvider serviceProvider,
        ILogger<Orchestrator> logger)
    {
        this.agentRunner = agentRunner;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task<AgentRunResult> ExecuteAsync(
        string systemPrompt,
        string userQuery,
        IEnumerable<string>? toolNames = null,
        Dictionary<string, object>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Orchestrator executing agent task. Tools: {ToolNames}",
            toolNames != null ? string.Join(", ", toolNames) : "all");

        // Discover and filter tools
        var tools = GetTools(toolNames);

        logger.LogInformation("Selected {ToolCount} tools for execution", tools.Count);

        // Build conversation
        var messages = BuildInitialConversation(systemPrompt, userQuery, additionalContext);

        // Execute agent
        return await agentRunner.RunAsync(messages, tools, cancellationToken);
    }

    public async Task<AgentRunResult> ExecuteWithToolsAsync(
        string systemPrompt,
        string userQuery,
        List<ITool> tools,
        Dictionary<string, object>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Orchestrator executing with explicit tools. Tool count: {ToolCount}", tools.Count);

        // Build conversation
        var messages = BuildInitialConversation(systemPrompt, userQuery, additionalContext);

        // Execute agent
        return await agentRunner.RunAsync(messages, tools, cancellationToken);
    }

    public async Task<AgentRunResult> ContinueConversationAsync(
        List<LLMMessage> conversationHistory,
        string newUserMessage,
        IEnumerable<string>? toolNames = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Orchestrator continuing conversation. Message count: {MessageCount}",
            conversationHistory.Count);

        // Add new user message
        var messages = new List<LLMMessage>(conversationHistory)
        {
            LLMMessage.User(newUserMessage)
        };

        // Get tools
        var tools = GetTools(toolNames);

        // Execute agent
        return await agentRunner.RunAsync(messages, tools, cancellationToken);
    }

    /// <summary>
    /// Gets tools from DI container, filtered by names if specified
    /// </summary>
    private List<ITool> GetTools(IEnumerable<string>? toolNames)
    {
        // Get all registered tools from DI
        var allTools = serviceProvider.GetServices<ITool>().ToList();

        if (toolNames == null)
        {
            logger.LogDebug("Using all {ToolCount} registered tools", allTools.Count);
            return allTools;
        }

        var requestedToolNames = toolNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedTools = allTools.Where(t => requestedToolNames.Contains(t.Name)).ToList();

        // Log if some tools were not found
        var foundToolNames = selectedTools.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingTools = requestedToolNames.Except(foundToolNames).ToList();

        if (missingTools.Count > 0)
        {
            logger.LogWarning("Some requested tools were not found: {MissingTools}",
                string.Join(", ", missingTools));
        }

        logger.LogDebug("Selected {SelectedCount} tools out of {RequestedCount} requested",
            selectedTools.Count, requestedToolNames.Count);

        return selectedTools;
    }

    /// <summary>
    /// Builds initial conversation with system prompt and user query
    /// </summary>
    private List<LLMMessage> BuildInitialConversation(
        string systemPrompt,
        string userQuery,
        Dictionary<string, object>? additionalContext)
    {
        var messages = new List<LLMMessage>();

        // Add system prompt
        var enhancedSystemPrompt = EnhanceSystemPrompt(systemPrompt, additionalContext);
        messages.Add(LLMMessage.System(enhancedSystemPrompt));

        // Add user query
        messages.Add(LLMMessage.User(userQuery));

        return messages;
    }

    /// <summary>
    /// Enhances system prompt with additional context
    /// </summary>
    private string EnhanceSystemPrompt(string basePrompt, Dictionary<string, object>? additionalContext)
    {
        if (additionalContext == null || additionalContext.Count == 0)
        {
            return basePrompt;
        }

        var contextLines = new List<string> { basePrompt, "", "## Additional Context:" };

        foreach (var kvp in additionalContext)
        {
            contextLines.Add($"- {kvp.Key}: {kvp.Value}");
        }

        var enhanced = string.Join(Environment.NewLine, contextLines);

        logger.LogDebug("Enhanced system prompt with {ContextCount} context items", additionalContext.Count);

        return enhanced;
    }
}
