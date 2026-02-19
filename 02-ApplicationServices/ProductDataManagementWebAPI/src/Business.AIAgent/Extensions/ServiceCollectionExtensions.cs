using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using Business.AIAgent.Configuration;
using Business.AIAgent.Core;
using Business.AIAgent.Plugins;
using Business.AIAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Business.AIAgent.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AI Agent services with Semantic Kernel and Document Intelligence
    /// </summary>
    public static IServiceCollection AddAIAgent(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Azure OpenAI settings
        services.Configure<AzureOpenAISettings>(
            configuration.GetSection(AzureOpenAISettings.SectionName));

        // Configure Document Intelligence settings
        services.Configure<DocumentIntelligenceSettings>(
            configuration.GetSection(DocumentIntelligenceSettings.SectionName));

        services.AddSingleton<Kernel>(serviceProvider =>
        {
            var settings = serviceProvider
                .GetRequiredService<IOptions<AzureOpenAISettings>>()
                .Value;

            var builder = Kernel.CreateBuilder();

            if (settings.UseManagedIdentity)
            {
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: settings.DeploymentName,
                    endpoint: settings.Endpoint,
                    credentials: new DefaultAzureCredential());
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ApiKey))
                {
                    throw new InvalidOperationException(
                        "ApiKey is required when UseManagedIdentity is false");
                }

                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: settings.DeploymentName,
                    endpoint: settings.Endpoint,
                    apiKey: settings.ApiKey);
            }

            return builder.Build();
        });

        // Register Document Intelligence client
        services.AddSingleton<DocumentIntelligenceClient>(serviceProvider =>
        {
            var settings = serviceProvider
                .GetRequiredService<IOptions<DocumentIntelligenceSettings>>()
                .Value;

            if (settings.UseManagedIdentity)
            {
                return new DocumentIntelligenceClient(
                    new Uri(settings.Endpoint),
                    new DefaultAzureCredential());
            }
            else
            {
                if (string.IsNullOrEmpty(settings.ApiKey))
                {
                    throw new InvalidOperationException(
                        "DocumentIntelligence ApiKey is required when UseManagedIdentity is false");
                }

                return new DocumentIntelligenceClient(
                    new Uri(settings.Endpoint),
                    new Azure.AzureKeyCredential(settings.ApiKey));
            }
        });

        services.AddSingleton<IKernelOrchestrator, KernelOrchestrator>();
        services.AddSingleton<IPluginRegistry, PluginRegistry>();
        services.AddSingleton<IAgentService, AgentService>();
        services.AddScoped<ProjectCostExtractionService>();

        return services;
    }

    /// <summary>
    /// Registers all AI Agent plugins in DI container
    /// Call this BEFORE app.Build()
    /// </summary>
    public static IServiceCollection AddAIPlugins(this IServiceCollection services)
    {

        return services;
    }

    /// <summary>
    /// Registers all AI Agent plugins with Semantic Kernel
    /// Call this AFTER app.Build() using app.Services.UseAIPlugins()
    /// </summary>
    public static IServiceProvider UseAIPlugins(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var pluginRegistry = scope.ServiceProvider.GetRequiredService<IPluginRegistry>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IPluginRegistry>>();

        try
        {
            // Register domain-specific plugins

            var registeredPlugins = pluginRegistry.GetRegisteredPlugins();
            logger.LogInformation(
                "✅ Successfully registered {Count} AI plugins: {Plugins}",
                registeredPlugins.Count,
                string.Join(", ", registeredPlugins));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to register AI plugins");
            throw;
        }

        return serviceProvider;
    }
}
