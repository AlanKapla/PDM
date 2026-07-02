using System.Text;
using Business.AIAgent.Core;
using Business.AIAgent.Services;
using Business.Implementation.Helpers;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services.TechnicalDocumentation;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class GroupExtractionAgentService : IGroupExtractionAgentService
{
    private const string AgentAName = "group-extraction-agent-a";
    private const string AgentBName = "group-extraction-agent-b";

    private readonly IAICompletionService completionService;
    private readonly AgentDefinitionLoader agentDefinitionLoader;
    private readonly TechnicalDocumentationOptions options;

    public GroupExtractionAgentService(
        IAICompletionService completionService,
        AgentDefinitionLoader agentDefinitionLoader,
        IOptions<TechnicalDocumentationOptions> options)
    {
        this.completionService = completionService;
        this.agentDefinitionLoader = agentDefinitionLoader;
        this.options = options.Value;
    }

    public async Task<(string ResultAJson, string ResultBJson)> ExtractGroupAsync(
        ThematicDrawingGroup group,
        CancellationToken cancellationToken)
    {
        List<string> mergedA = [];
        List<string> mergedB = [];

        foreach (IReadOnlyList<ClassifiedTechnicalDocumentationImage> batch in ChunkImages(group.Images))
        {
            string userText = BuildUserPrompt(group.GroupName, batch);
            List<(byte[] ImageBytes, string MediaType)> images = batch
                .Select(item => (item.Image.ImageBytes, item.Image.MediaType))
                .ToList();

            Task<string> taskA = TechnicalDocumentationAgentInvoker.CompleteWithImagesAsync(
                completionService,
                agentDefinitionLoader,
                AgentAName,
                userText,
                images,
                cancellationToken,
                Options.Create(options));

            Task<string> taskB = TechnicalDocumentationAgentInvoker.CompleteWithImagesAsync(
                completionService,
                agentDefinitionLoader,
                AgentBName,
                userText,
                images,
                cancellationToken,
                Options.Create(options));

            string[] results = await Task.WhenAll(taskA, taskB);
            mergedA.Add(AiGeneratedJsonSanitizer.Sanitize(results[0]));
            mergedB.Add(AiGeneratedJsonSanitizer.Sanitize(results[1]));
        }

        return (GroupExtractionJsonMerger.Merge(mergedA), GroupExtractionJsonMerger.Merge(mergedB));
    }

    private IEnumerable<IReadOnlyList<ClassifiedTechnicalDocumentationImage>> ChunkImages(
        IReadOnlyList<ClassifiedTechnicalDocumentationImage> images)
    {
        int maxImages = options.MaxImagesPerGroup;

        for (int index = 0; index < images.Count; index += maxImages)
        {
            yield return images.Skip(index).Take(maxImages).ToList();
        }
    }

    private static string BuildUserPrompt(
        string groupName,
        IReadOnlyList<ClassifiedTechnicalDocumentationImage> images)
    {
        StringBuilder builder = new();
        builder.AppendLine($"Grupa tematyczna: {groupName}");
        builder.AppendLine("Przeanalizuj powyższe rysunki łącznie i zwróć dane w schemacie JSON dla tej grupy.");
        builder.AppendLine("Rysunki w tej partii:");

        foreach (ClassifiedTechnicalDocumentationImage image in images)
        {
            string sheet = image.Classification.SheetNumber ?? image.Image.FileName;
            string title = image.Classification.Title ?? string.Empty;
            builder.AppendLine($"- {sheet}: {title} ({image.Classification.DrawingType})");
        }

        return builder.ToString();
    }
}
