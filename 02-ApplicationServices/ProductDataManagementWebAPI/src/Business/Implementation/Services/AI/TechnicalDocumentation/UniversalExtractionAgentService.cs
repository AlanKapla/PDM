using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class UniversalExtractionAgentService : IUniversalExtractionAgent
{
    private const string AgentName = "universal-extraction-agent";

    private readonly IAICompletionService completionService;
    private readonly AgentDefinitionLoader agentDefinitionLoader;
    private readonly ILogger<UniversalExtractionAgentService> logger;

    public UniversalExtractionAgentService(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        ILogger<UniversalExtractionAgentService> logger)
    {
        this.completionService = completionService;
        this.agentDefinitionLoader = agentDefinitionLoader;
        this.logger = logger;
    }

    public Task<FloorPlanDrawing> ExtractAsync(
        byte[] imageBytes,
        string mediaType,
        DrawingClassification classification,
        TechnicalDocumentationExtractionContext? extractionContext,
        string focusPrompt,
        CancellationToken cancellationToken)
    {
        return TechnicalDocumentationExtractionHelper.ExtractWithFocusAsync(
            completionService,
            agentDefinitionLoader,
            AgentName,
            imageBytes,
            mediaType,
            classification,
            extractionContext,
            focusPrompt,
            cancellationToken,
            logger);
    }
}
