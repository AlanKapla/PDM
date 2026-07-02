using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class MaterialScheduleBuilder
{
    public static MaterialSchedule Build(
        ConsolidatedProjectMaterials consolidated,
        ProjectTechnicalDocumentationDetails details,
        IReadOnlyList<FloorPlanDrawing> drawings,
        string buildingType,
        IReadOnlyDictionary<string, object>? sharedState = null)
    {
        MaterialSchedule schedule = new()
        {
            CalculatedAt = DateTime.UtcNow,
            DrawingsUsed = drawings
                .Select(FormatDrawingLabel)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        schedule.Foundations.Blocks = ToMaterialItems(consolidated.FoundationBlocks, MaterialUnitSection.FoundationBlocks);
        schedule.Foundations.Concrete = ToMaterialItems(consolidated.FoundationConcrete, MaterialUnitSection.FoundationConcrete);
        schedule.Foundations.Steel = ToMaterialItems(consolidated.FoundationSteel, MaterialUnitSection.FoundationSteel);

        schedule.Walls.Masonry = ToMaterialItems(
            consolidated.WallMaterials.Where(item => IsMasonry(item.MaterialType)).ToList(),
            MaterialUnitSection.WallMasonry);
        schedule.Walls.Mortar = ToMaterialItems(
            consolidated.WallMaterials.Where(item => IsMortar(item.MaterialType)).ToList(),
            MaterialUnitSection.WallMortar);
        schedule.Walls.Insulation = ToMaterialItems(consolidated.ThermalInsulation, MaterialUnitSection.WallInsulation);

        schedule.Ceilings.Concrete = ToMaterialItems(consolidated.FloorConcrete, MaterialUnitSection.CeilingConcrete);
        schedule.Ceilings.Steel = ToMaterialItems(consolidated.FloorSteel, MaterialUnitSection.CeilingSteel);

        schedule.Roof.Timber = ToMaterialItems(consolidated.Timber, MaterialUnitSection.RoofTimber);

        if (details.Roof is not null && !string.IsNullOrWhiteSpace(details.Roof.CoveringType))
        {
            schedule.Roof.Covering.Add(new MaterialItem
            {
                Element = details.Roof.CoveringType,
                NetQuantity = details.Roof.AreaM2 ?? 0,
                GrossQuantity = details.Roof.AreaM2 ?? 0,
                Unit = "m2"
            });
        }

        schedule.Openings = BuildOpenings(details);
        schedule.AuditNotesToWarnings(consolidated.AuditNotes, buildingType);

        ProjectModel projectModel = details.ProjectModel ?? new ProjectModel();
        IReadOnlyDictionary<string, object> state = sharedState ?? new Dictionary<string, object>();
        schedule = MaterialScheduleDrawingEnricher.Enrich(schedule, projectModel, drawings, state);

        return MaterialQuantityFilter.PruneZeroQuantities(schedule);
    }

    public static List<MaterialSummaryItem> BuildSummaryPublic(MaterialSchedule schedule)
    {
        return BuildSummary(schedule);
    }

    private static List<OpeningScheduleItem> BuildOpenings(ProjectTechnicalDocumentationDetails details)
    {
        List<OpeningScheduleItem> openings = new();

        if (details.Joinery?.Exterior?.Windows is not null)
        {
            foreach (JoineryWindowEntry window in details.Joinery.Exterior.Windows)
            {
                openings.Add(new OpeningScheduleItem
                {
                    Type = "okno",
                    WidthCm = window.WidthCm ?? 0,
                    HeightCm = window.HeightCm ?? 0,
                    Count = window.Count
                });
            }
        }

        if (details.Joinery?.Exterior?.Doors is not null)
        {
            foreach (JoineryDoorEntry door in details.Joinery.Exterior.Doors)
            {
                openings.Add(new OpeningScheduleItem
                {
                    Type = "drzwi",
                    Count = door.Count
                });
            }
        }

        return openings;
    }

    private static List<MaterialSummaryItem> BuildSummary(MaterialSchedule schedule)
    {
        List<MaterialSummaryItem> summary = new();

        AddSummary(summary, "fundamenty", schedule.Foundations.Blocks);
        AddSummary(summary, "fundamenty", schedule.Foundations.Concrete);
        AddSummary(summary, "fundamenty", schedule.Foundations.Steel);
        AddSummary(summary, "ściany", schedule.Walls.Masonry);
        AddSummary(summary, "ściany", schedule.Walls.Mortar);
        AddSummary(summary, "ściany", schedule.Walls.Insulation);
        AddSummary(summary, "stropy", schedule.Ceilings.Concrete);
        AddSummary(summary, "stropy", schedule.Ceilings.Steel);
        AddSummary(summary, "dach", schedule.Roof.Covering);
        AddSummary(summary, "dach", schedule.Roof.Timber);
        AddSummary(summary, "dach", schedule.Roof.Insulation);
        AddSummary(summary, "izolacja", schedule.Insulation);

        return summary;
    }

    private static void AddSummary(List<MaterialSummaryItem> summary, string category, List<MaterialItem> items)
    {
        foreach (MaterialItem item in items)
        {
            string materialType = !string.IsNullOrWhiteSpace(item.Specification)
                ? item.Specification
                : item.Element;
            summary.Add(new MaterialSummaryItem
            {
                Category = category,
                MaterialType = materialType,
                GrossQuantity = item.GrossQuantity,
                Unit = item.Unit
            });
        }
    }

    private static List<MaterialItem> ToMaterialItems(
        List<MaterialQuantity> materials,
        MaterialUnitSection section)
    {
        List<MaterialItem> items = new();

        foreach (MaterialQuantity material in materials)
        {
            MaterialQuantity normalized = MaterialUnitNormalizer.Normalize(material, section);
            items.Add(new MaterialItem
            {
                Element = normalized.MaterialType,
                Specification = normalized.MaterialType,
                NetQuantity = normalized.Quantity,
                GrossQuantity = normalized.Quantity,
                Unit = normalized.Unit
            });
        }

        return items;
    }

    private static bool IsMasonry(string materialType)
    {
        string text = materialType.ToLowerInvariant();
        return text.Contains("beton", StringComparison.Ordinal)
            || text.Contains("pustak", StringComparison.Ordinal)
            || text.Contains("bloczek", StringComparison.Ordinal)
            || text.Contains("ytong", StringComparison.Ordinal)
            || text.Contains("silikat", StringComparison.Ordinal)
            || text.Contains("keramzyt", StringComparison.Ordinal);
    }

    private static bool IsMortar(string materialType)
    {
        string text = materialType.ToLowerInvariant();
        return text.Contains("tynk", StringComparison.Ordinal)
            || text.Contains("zaprawa", StringComparison.Ordinal);
    }

    private static string FormatDrawingLabel(FloorPlanDrawing drawing)
    {
        if (drawing.Source.PageNumber > 0)
        {
            return $"{drawing.Source.FileName} (str. {drawing.Source.PageNumber})";
        }

        return drawing.Source.FileName;
    }
}

internal static class MaterialScheduleWarningExtensions
{
    public static void AuditNotesToWarnings(
        this MaterialSchedule schedule,
        IReadOnlyList<string> auditNotes,
        string buildingType)
    {
        schedule.Warnings.Add($"Harmonogram skonsolidowany obiektywnie ({buildingType}) — bez sumowania tych samych materiałów per strona.");

        foreach (string note in auditNotes)
        {
            schedule.Warnings.Add(note);
        }
    }
}
