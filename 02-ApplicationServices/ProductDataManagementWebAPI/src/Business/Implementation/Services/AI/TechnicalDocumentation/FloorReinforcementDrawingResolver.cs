using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class FloorReinforcementDrawingResolver
{
    internal sealed class ReinforcementLayers
    {
        public FloorSection? Bottom { get; set; }
        public string? BottomSheet { get; set; }
        public FloorSection? Top { get; set; }
        public string? TopSheet { get; set; }
    }

    public static ReinforcementLayers Resolve(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        ReinforcementLayers layers = new();
        List<FloorPlanDrawing> candidates = drawings
            .Where(drawing => HasReinforcementData(drawing.Floors))
            .ToList();

        foreach (FloorPlanDrawing drawing in candidates)
        {
            string normalizedType = NormalizeDrawingType(drawing.Classification.DrawingType);
            string? sheetNumber = drawing.Classification.SheetNumber;

            if (IsTopLayer(normalizedType, drawing.Classification.Title, sheetNumber))
            {
                layers.Top = NormalizeFloorSection(drawing.Floors!);
                layers.TopSheet = sheetNumber ?? layers.TopSheet;
                continue;
            }

            if (IsBottomLayer(normalizedType, drawing.Classification.Title, sheetNumber))
            {
                layers.Bottom = NormalizeFloorSection(drawing.Floors!);
                layers.BottomSheet = sheetNumber ?? layers.BottomSheet;
            }
        }

        if (layers.Bottom is null || layers.Top is null)
        {
            ApplySheetFallback(candidates, layers);
        }

        if (layers.Bottom is null || layers.Top is null)
        {
            ApplyOrderFallback(candidates, layers);
        }

        return layers;
    }

    private static void ApplySheetFallback(IReadOnlyList<FloorPlanDrawing> candidates, ReinforcementLayers layers)
    {
        foreach (FloorPlanDrawing drawing in candidates)
        {
            string sheet = drawing.Classification.SheetNumber ?? string.Empty;
            if (layers.Bottom is null && ContainsSheetToken(sheet, "k-02", "k02"))
            {
                layers.Bottom = NormalizeFloorSection(drawing.Floors!);
                layers.BottomSheet = sheet;
            }

            if (layers.Top is null && ContainsSheetToken(sheet, "k-03", "k03"))
            {
                layers.Top = NormalizeFloorSection(drawing.Floors!);
                layers.TopSheet = sheet;
            }
        }
    }

    private static void ApplyOrderFallback(IReadOnlyList<FloorPlanDrawing> candidates, ReinforcementLayers layers)
    {
        List<FloorPlanDrawing> unresolved = candidates
            .Where(drawing => drawing.Floors is not null)
            .OrderBy(drawing => drawing.Classification.SheetNumber ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unresolved.Count < 2)
        {
            return;
        }

        if (layers.Bottom is null)
        {
            FloorPlanDrawing first = unresolved[0];
            layers.Bottom = NormalizeFloorSection(first.Floors!);
            layers.BottomSheet = first.Classification.SheetNumber;
        }

        if (layers.Top is null)
        {
            FloorPlanDrawing second = unresolved[1];
            layers.Top = NormalizeFloorSection(second.Floors!);
            layers.TopSheet = second.Classification.SheetNumber;
        }
    }

    private static FloorSection NormalizeFloorSection(FloorSection section)
    {
        if (section.TotalMassKg is null && section.Steel.Count > 0)
        {
            double sum = section.Steel.Sum(item => item.Quantity);
            if (sum > 0)
            {
                section.TotalMassKg = Math.Round(sum, 2);
            }
        }

        return section;
    }

    private static bool HasReinforcementData(FloorSection? section)
    {
        if (section is null)
        {
            return false;
        }

        return section.Bars.Count > 0
            || section.TotalMassKg is > 0
            || section.Steel.Count > 0
            || section.Slabs.Any(slab => !string.IsNullOrWhiteSpace(slab.Reinforcement));
    }

    private static bool IsTopLayer(string normalizedType, string? title, string? sheetNumber)
    {
        if (normalizedType.Contains("zbrojenie stropu gorne", StringComparison.Ordinal))
        {
            return true;
        }

        string combined = $"{title} {sheetNumber}".ToLowerInvariant();
        return combined.Contains("gorn", StringComparison.Ordinal)
            || combined.Contains("gór", StringComparison.Ordinal)
            || combined.Contains("k-03", StringComparison.Ordinal)
            || combined.Contains("k03", StringComparison.Ordinal);
    }

    private static bool IsBottomLayer(string normalizedType, string? title, string? sheetNumber)
    {
        if (normalizedType.Contains("zbrojenie stropu dolne", StringComparison.Ordinal))
        {
            return true;
        }

        string combined = $"{title} {sheetNumber}".ToLowerInvariant();
        return combined.Contains("doln", StringComparison.Ordinal)
            || combined.Contains("k-02", StringComparison.Ordinal)
            || combined.Contains("k02", StringComparison.Ordinal);
    }

    private static bool ContainsSheetToken(string sheet, params string[] tokens)
    {
        string normalized = sheet.Trim().ToLowerInvariant();
        foreach (string token in tokens)
        {
            if (normalized.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeDrawingType(string drawingType)
    {
        return ExtractionFocusRouter.NormalizeDrawingType(drawingType);
    }
}
