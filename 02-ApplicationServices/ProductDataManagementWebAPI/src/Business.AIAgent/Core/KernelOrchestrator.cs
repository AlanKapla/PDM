using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Business.AIAgent.Configuration;
using System.Runtime.CompilerServices;

namespace Business.AIAgent.Core;

public sealed class KernelOrchestrator : IKernelOrchestrator
{
    private readonly Kernel _kernel;
    private readonly ILogger<KernelOrchestrator> _logger;
    private readonly AzureOpenAISettings _settings;

    public KernelOrchestrator(
        Kernel kernel,
        ILogger<KernelOrchestrator> logger,
        IOptions<AzureOpenAISettings> settings)
    {
        _kernel = kernel;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<string> ExecutePromptAsync(
        string prompt,
        Dictionary<string, object>? arguments = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing prompt");

            // If system prompt provided, use ChatHistory
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(prompt);

                var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

                var result = await chatCompletion.GetChatMessageContentsAsync(
                    chatHistory,
                    kernel: _kernel,
                    cancellationToken: cancellationToken);

                var lastMessage = result.LastOrDefault();
                _logger.LogInformation("Prompt execution with system prompt completed successfully");
                return lastMessage?.Content ?? string.Empty;
            }
            else
            {
                // No system prompt - use simple prompt execution
                var kernelArguments = CreateKernelArguments(arguments);
                var result = await _kernel.InvokePromptAsync(prompt, kernelArguments);

                var response = result.GetValue<string>() ?? string.Empty;
                _logger.LogInformation("Prompt execution completed successfully");
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing prompt");
            throw;
        }
    }

    public async IAsyncEnumerable<string> ExecutePromptStreamingAsync(
        string prompt,
        Dictionary<string, object>? arguments = null,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing streaming prompt");

        // If system prompt provided, use ChatHistory
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(prompt);

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

            await foreach (var chunk in chatCompletion.GetStreamingChatMessageContentsAsync(
                chatHistory,
                kernel: _kernel,
                cancellationToken: cancellationToken))
            {
                var content = chunk.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }
            }

            _logger.LogInformation("Streaming prompt with system prompt execution completed");
        }
        else
        {
            // No system prompt - use simple streaming
            var kernelArguments = CreateKernelArguments(arguments);

            await foreach (var chunk in _kernel.InvokePromptStreamingAsync(prompt, kernelArguments))
            {
                var content = chunk.ToString();
                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }
            }

            _logger.LogInformation("Streaming prompt execution completed");
        }
    }

    public async Task<string> ExecutePromptWithImageAsync(
        string prompt,
        byte[] imageContent,
        string mimeType = "image/jpeg",
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing prompt with image content (size: {Size} bytes)", imageContent.Length);

            // Create ChatHistory
            var chatHistory = new ChatHistory();
            
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                chatHistory.AddSystemMessage(systemPrompt);
            }

            // Create ChatMessageContentItemCollection with text and image
            // Use ImageContent with Uri property pointing to data URL
            var contentItems = new ChatMessageContentItemCollection
            {
                new TextContent(prompt),
                new ImageContent(imageContent, mimeType)
            };

            // Add user message with both text and image
            chatHistory.AddUserMessage(contentItems);

            // Get chat completion service
            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

            // Execute with vision support
            var result = await chatCompletion.GetChatMessageContentsAsync(
                chatHistory,
                kernel: _kernel,
                cancellationToken: cancellationToken);

            var lastMessage = result.LastOrDefault();
            
            _logger.LogInformation("Prompt execution with image completed successfully");
            
            return lastMessage?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing prompt with image");
            throw;
        }
    }

    public async Task<string> ExecuteWithToolsAsync(
        string prompt,
        Dictionary<string, object>? arguments = null,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing prompt with tools enabled");

            var executionSettings = new AzureOpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                MaxTokens = _settings.MaxTokens,
                Temperature = _settings.Temperature
            };

            if (_settings.TopP.HasValue)
            {
                executionSettings.TopP = _settings.TopP.Value;
            }

            // If system prompt provided, use ChatHistory
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(prompt);

                var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

                var result = await chatCompletion.GetChatMessageContentsAsync(
                    chatHistory,
                    executionSettings,
                    _kernel,
                    cancellationToken);

                var lastMessage = result.LastOrDefault();
                _logger.LogInformation("Tool-enabled execution with system prompt completed successfully");
                return lastMessage?.Content ?? string.Empty;
            }
            else
            {
                // No system prompt - use simple execution
                var kernelArguments = CreateKernelArguments(arguments);
                kernelArguments.ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
                {
                    { "default", executionSettings }
                };

                var result = await _kernel.InvokePromptAsync(prompt, kernelArguments);

                var response = result.GetValue<string>() ?? string.Empty;
                _logger.LogInformation("Tool-enabled execution completed successfully");
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing with tools");
            throw;
        }
    }

    public void RegisterPlugin<TPlugin>() where TPlugin : class
    {
        try
        {
            _logger.LogInformation("Registering plugin: {PluginType}", typeof(TPlugin).Name);
            _kernel.Plugins.AddFromType<TPlugin>();
            _logger.LogInformation("Plugin registered successfully: {PluginType}", typeof(TPlugin).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering plugin: {PluginType}", typeof(TPlugin).Name);
            throw;
        }
    }

    public void RegisterPlugin(object plugin, string? pluginName = null)
    {
        try
        {
            var name = pluginName ?? plugin.GetType().Name;
            _logger.LogInformation("Registering plugin instance: {PluginName}", name);
            _kernel.Plugins.AddFromObject(plugin, name);
            _logger.LogInformation("Plugin instance registered successfully: {PluginName}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering plugin instance: {PluginName}", pluginName);
            throw;
        }
    }

    private KernelArguments CreateKernelArguments(Dictionary<string, object>? arguments)
    {
        var kernelArguments = new KernelArguments();

        if (arguments is not null)
        {
            foreach (var arg in arguments)
            {
                kernelArguments[arg.Key] = arg.Value;
            }
        }

        return kernelArguments;
    }
}
