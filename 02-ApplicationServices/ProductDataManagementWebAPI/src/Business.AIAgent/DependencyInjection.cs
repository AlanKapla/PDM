using Business.AIAgent.Configuration;
using Business.AIAgent.Interfaces;
using Business.AIAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.AIAgent;

/// <summary>
/// Extension methods for registering AI Agent services in DI container
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all AI Agent framework services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAIAgent(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration - use Configure overload that accepts IConfiguration
        services.Configure<AzureOpenAISettings>(
            configuration.GetSection(AzureOpenAISettings.SectionName));

        // Register core services
        services.AddSingleton<IAzureOpenAIClient, AzureOpenAIClient>();
        services.AddScoped<IAgentRunner, AgentRunner>();
        services.AddScoped<IOrchestrator, Orchestrator>();

        return services;
    }

    /// <summary>
    /// Registers a tool in the DI container
    /// Tools are automatically discovered by Orchestrator
    /// </summary>
    /// <typeparam name="TTool">Tool implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddTool<TTool>(this IServiceCollection services)
        where TTool : class, ITool
    {
        services.AddScoped<ITool, TTool>();
        return services;
    }

    /// <summary>
    /// Registers multiple tools at once
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="toolTypes">Tool types to register</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddTools(
        this IServiceCollection services,
        params Type[] toolTypes)
    {
        foreach (var toolType in toolTypes)
        {
            if (!typeof(ITool).IsAssignableFrom(toolType))
            {
                throw new ArgumentException(
                    $"Type {toolType.Name} does not implement ITool",
                    nameof(toolTypes));
            }

            services.AddScoped(typeof(ITool), toolType);
        }

        return services;
    }
}
