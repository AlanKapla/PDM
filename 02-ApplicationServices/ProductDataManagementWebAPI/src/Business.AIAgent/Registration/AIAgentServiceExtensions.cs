using Business.AIAgent.Abstractions;
using Business.AIAgent.Configuration;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.AIAgent.Tools.CostEstimate;
using Business.AIAgent.Tools.Http;
using Business.AIAgent.Tools.Projects;
using Business.AIAgent.Tools.SubAgent;
using Business.AIAgent.Tools.WorkSchedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.AIAgent.Registration;

public static class AIAgentServiceExtensions
{
    public static IServiceCollection AddAIAgent(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureAIAgentOptions>(
            configuration.GetSection(AzureAIAgentOptions.SectionName));

        services.AddHttpClient("AIAgentHttp");

        // Core
        services.AddSingleton<AgentDefinitionLoader>();
        services.AddScoped<ToolCallExecutor>();
        services.AddScoped<IAgentRunner, AgentRunner>();
        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IAICompletionService, AzureAICompletionService>();

        // Domain tools — registered as IAgentTool so ToolRegistry can discover all
        services.AddScoped<IAgentTool, GetProjectInfoTool>();
        services.AddScoped<IAgentTool, GetCostEstimateTool>();
        services.AddScoped<IAgentTool, GetCostEstimateItemsTool>();
        services.AddScoped<IAgentTool, GetWorkScheduleTool>();
        services.AddScoped<IAgentTool, HttpFetchTool>();
        services.AddScoped<IAgentTool, CallSubAgentTool>();

        return services;
    }
}
