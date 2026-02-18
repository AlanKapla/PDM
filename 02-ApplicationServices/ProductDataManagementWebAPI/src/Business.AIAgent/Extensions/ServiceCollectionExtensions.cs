using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Business.AIAgent.Configuration;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.AIAgent.Plugins;
using Business.AIAgent.Plugins.WorkSchedule;
using Business.AIAgent.Plugins.CostEstimate;

namespace Business.AIAgent.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AI Agent services with Semantic Kernel
    /// </summary>
    public static IServiceCollection AddAIAgent(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureOpenAISettings>(
            configuration.GetSection(AzureOpenAISettings.SectionName));

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

        services.AddScoped<IKernelOrchestrator, KernelOrchestrator>();
        services.AddSingleton<IPluginRegistry, PluginRegistry>();
        services.AddScoped<IAgentService, AgentService>();

        return services;
    }

    /// <summary>
    /// Registers all AI Agent plugins in DI container
    /// Call this BEFORE app.Build()
    /// </summary>
    public static IServiceCollection AddAIPlugins(this IServiceCollection services)
    {
        services.AddScoped<WorkSchedulePlugin>();
        services.AddScoped<ExcelAnalysisPlugin>();

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
            pluginRegistry.RegisterPlugin<WorkSchedulePlugin>();
            pluginRegistry.RegisterPlugin<ExcelAnalysisPlugin>();

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
