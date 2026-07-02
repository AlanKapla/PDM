using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class MaterialScheduleJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    public static MaterialSchedule Parse(
        string response,
        IReadOnlyList<FloorPlanDrawing> drawings,
        string buildingType,
        ILogger? logger = null)
    {
        string json = TechnicalDocumentationJsonHelper.ExtractJson(response);

        if (json == "{}")
        {
            logger?.LogWarning("Material calculation agent returned no JSON payload, using fallback");
            return MaterialScheduleFallbackBuilder.Build(drawings, buildingType);
        }

        json = MaterialScheduleJsonSanitizer.Sanitize(json);

        try
        {
            MaterialSchedule? schedule = JsonSerializer.Deserialize<MaterialSchedule>(json, JsonOptions);
            if (schedule is null || !MaterialScheduleContentHelper.HasMeaningfulContent(schedule))
            {
                logger?.LogWarning("Material calculation agent returned empty schedule, using fallback");
                return MaterialScheduleFallbackBuilder.Build(drawings, buildingType);
            }

            if (schedule.CalculatedAt == default)
            {
                schedule.CalculatedAt = DateTime.UtcNow;
            }

            if (schedule.DrawingsUsed.Count == 0)
            {
                schedule.DrawingsUsed = drawings
                    .Select(drawing => drawing.Source.FileName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return MaterialQuantityFilter.PruneZeroQuantities(schedule);
        }
        catch (JsonException ex)
        {
            string preview = json.Length > 160 ? json[..160] : json;
            logger?.LogWarning(
                ex,
                "Failed to deserialize MaterialSchedule, using fallback. Preview: {Preview}",
                preview);

            return MaterialScheduleFallbackBuilder.Build(drawings, buildingType);
        }
    }
}
