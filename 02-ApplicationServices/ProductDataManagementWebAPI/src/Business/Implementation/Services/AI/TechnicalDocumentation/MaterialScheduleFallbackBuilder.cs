using System.Text.RegularExpressions;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class MaterialScheduleFallbackBuilder
{
    public static MaterialSchedule Build(
        IReadOnlyList<FloorPlanDrawing> drawings,
        string buildingType)
    {
        MaterialSchedule schedule = new()
        {
            CalculatedAt = DateTime.UtcNow,
            DrawingsUsed = drawings
                .Select(drawing => FormatDrawingLabel(drawing))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Warnings =
            [
                $"Harmonogram materiałów ({buildingType}) utworzony lokalnie z danych ekstrakcji — kalkulacja AI była niedostępna lub niepełna."
            ]
        };

        Dictionary<string, MaterialSummaryItem> summaryItems = new(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlanDrawing drawing in drawings)
        {
            string drawingLabel = FormatDrawingLabel(drawing);
            AddSectionMaterials(summaryItems, drawing.Foundations?.Blocks, "fundamenty", MaterialUnitSection.FoundationBlocks);
            AddSectionMaterials(summaryItems, drawing.Foundations?.Concrete, "fundamenty", MaterialUnitSection.FoundationConcrete);
            AddSectionMaterials(summaryItems, drawing.Foundations?.Steel, "fundamenty", MaterialUnitSection.FoundationSteel);
            AddSectionMaterials(summaryItems, drawing.Floors?.Concrete, "stropy", MaterialUnitSection.FloorConcrete);
            AddSectionMaterials(summaryItems, drawing.Floors?.Steel, "stropy", MaterialUnitSection.FloorSteel);

            foreach (Opening opening in drawing.Openings)
            {
                schedule.Openings.Add(new OpeningScheduleItem
                {
                    Type = opening.Type,
                    WidthCm = opening.WidthCm,
                    HeightCm = opening.HeightCm,
                    Count = opening.Count,
                    Material = opening.Material
                });
            }
        }

        schedule.Summary = summaryItems.Values.ToList();
        return MaterialQuantityFilter.PruneZeroQuantities(schedule);
    }

    private static string FormatDrawingLabel(FloorPlanDrawing drawing)
    {
        if (drawing.Source.PageNumber > 0)
        {
            return $"{drawing.Source.FileName} (str. {drawing.Source.PageNumber})";
        }

        return drawing.Source.FileName;
    }

    private static void AddSectionMaterials(
        Dictionary<string, MaterialSummaryItem> summaryItems,
        List<MaterialQuantity>? materials,
        string category,
        MaterialUnitSection section)
    {
        if (materials is null)
        {
            return;
        }

        foreach (MaterialQuantity material in materials)
        {
            if (string.IsNullOrWhiteSpace(material.MaterialType) || material.Quantity <= 0)
            {
                continue;
            }

            MaterialQuantity normalized = MaterialUnitNormalizer.Normalize(material, section);
            string key = $"{category}:{normalized.MaterialType}:{normalized.Unit}".ToLowerInvariant();
            if (!summaryItems.TryGetValue(key, out MaterialSummaryItem? existing))
            {
                summaryItems[key] = new MaterialSummaryItem
                {
                    Category = category,
                    MaterialType = normalized.MaterialType,
                    GrossQuantity = normalized.Quantity,
                    Unit = normalized.Unit
                };

                continue;
            }

            existing.GrossQuantity += normalized.Quantity;
        }
    }
}

internal static partial class MaterialScheduleJsonSanitizer
{
    public static string Sanitize(string json)
    {
        string result = json;
        result = EmptyDateFieldRegex().Replace(result, "\"$1\":null");
        result = EmptyProjectIdRegex().Replace(result, "\"projectId\":null");
        return result;
    }

    [GeneratedRegex(@"""(calculatedAt|createdAt)""\s*:\s*""""", RegexOptions.CultureInvariant)]
    private static partial Regex EmptyDateFieldRegex();

    [GeneratedRegex(@"""projectId""\s*:\s*""""", RegexOptions.CultureInvariant)]
    private static partial Regex EmptyProjectIdRegex();
}
