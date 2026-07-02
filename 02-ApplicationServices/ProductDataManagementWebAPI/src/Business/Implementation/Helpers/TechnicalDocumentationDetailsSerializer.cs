using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Helpers;

public static class TechnicalDocumentationDetailsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    public static string Serialize(ProjectTechnicalDocumentationDetails details)
    {
        ProjectModel model = ProjectModelSection81Enricher.PrepareForSerialization(details);

        TechnicalDocumentationDetailsJsonRoot root = new()
        {
            ProjectModel = model,
            MaterialSchedule = details.MaterialSchedule,
            AuditResult = details.AuditResult,
            TokenUsage = details.TokenUsage,
            ProcessedAt = details.ProcessedAt
        };

        return JsonSerializer.Serialize(root, JsonOptions);
    }

    public static ProjectTechnicalDocumentationDetails? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        string sanitized = AiGeneratedJsonSanitizer.Sanitize(json);

        if (IsNewFormat(sanitized))
        {
            return DeserializeNewFormat(sanitized);
        }

        return DeserializeLegacyFormat(sanitized);
    }

    public static bool IsNewFormat(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("projectModel", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static ProjectTechnicalDocumentationDetails DeserializeNewFormat(string json)
    {
        TechnicalDocumentationDetailsJsonRoot? root = JsonSerializer.Deserialize<TechnicalDocumentationDetailsJsonRoot>(
            json,
            JsonOptions);

        if (root is null)
        {
            return new ProjectTechnicalDocumentationDetails();
        }

        return new ProjectTechnicalDocumentationDetails
        {
            ProjectModel = root.ProjectModel,
            MaterialSchedule = root.MaterialSchedule,
            AuditResult = root.AuditResult,
            TokenUsage = root.TokenUsage,
            ProcessedAt = root.ProcessedAt
        };
    }

    private static ProjectTechnicalDocumentationDetails? DeserializeLegacyFormat(string json)
    {
        return JsonSerializer.Deserialize<ProjectTechnicalDocumentationDetails>(json, JsonOptions);
    }
}
