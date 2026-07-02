using System.Text;
using System.Text.Json;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Implementation.Helpers;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services.TechnicalDocumentation;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class ExtractionVerificationAgentService : IExtractionVerificationAgentService
{
    private const string AgentName = "extraction-verification-agent";

    private readonly IAICompletionService completionService;
    private readonly AgentDefinitionLoader agentDefinitionLoader;
    private readonly TechnicalDocumentationOptions options;

    public ExtractionVerificationAgentService(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        IOptions<TechnicalDocumentationOptions> options)
    {
        this.completionService = completionService;
        this.agentDefinitionLoader = agentDefinitionLoader;
        this.options = options.Value;
    }

    public async Task<string> VerifyCriticalDiffsAsync(
        ThematicDrawingGroup group,
        ExtractionDiffResult diff,
        string resultAJson,
        string resultBJson,
        CancellationToken cancellationToken)
    {
        List<ExtractionFieldDiff> criticalDiffs = diff.Differences
            .Where(item => item.IsCritical)
            .ToList();

        StringBuilder builder = new();
        builder.AppendLine($"Grupa: {group.GroupName}");
        builder.AppendLine("Poprzednie dwa odczyty dały różne wyniki dla tych pól:");

        foreach (ExtractionFieldDiff fieldDiff in criticalDiffs)
        {
            builder.AppendLine($"- {fieldDiff.FieldPath}: A={fieldDiff.ValueA} | B={fieldDiff.ValueB}");
        }

        builder.AppendLine("Przeczytaj te wartości ponownie z rysunków i zwróć TYLKO JSON z rozbieżnymi polami.");

        List<(byte[] ImageBytes, string MediaType)> images = group.Images
            .Take(options.MaxImagesPerGroup)
            .Select(item => (item.Image.ImageBytes, item.Image.MediaType))
            .ToList();

        string response = await TechnicalDocumentationAgentInvoker.CompleteWithImagesAsync(
            completionService,
            agentDefinitionLoader,
            AgentName,
            builder.ToString(),
            images,
            cancellationToken,
            Options.Create(options));

        return AiGeneratedJsonSanitizer.Sanitize(response);
    }
}
