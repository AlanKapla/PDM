using Business.Interfaces.Configurations;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ProjectModelSection81Enricher
{
    public const string PipelineVersion = "group-pipeline-v1";

    public static void Enrich(
        ProjectTechnicalDocumentationDetails details,
        IReadOnlyList<string>? thematicGroups = null)
    {
        ProjectModel model = details.ProjectModel ?? new ProjectModel();
        details.ProjectModel = model;

        MapSlabFromCeilings(model);
        SyncWarnings(model);
        PopulateExtractionMetadata(model, details, thematicGroups);
    }

    public static void EnrichFromDrawings(
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings)
    {
        if (model.Elevations.Count > 0)
        {
            return;
        }

        model.Elevations = MapElevationsFromDrawings(drawings);
    }

    public static ProjectModel PrepareForSerialization(ProjectTechnicalDocumentationDetails details)
    {
        ProjectModel model = details.ProjectModel ?? new ProjectModel();
        MapSlabFromCeilings(model);
        SyncWarnings(model);

        if (model.ExtractionMetadata.PipelineVersion is null)
        {
            PopulateExtractionMetadata(model, details, thematicGroups: null);
        }

        return model;
    }

    private static void MapSlabFromCeilings(ProjectModel model)
    {
        if (model.Slab is not null || model.Ceilings.Count == 0)
        {
            return;
        }

        ProjectModelCeiling ceiling = model.Ceilings[0];
        model.Slab = new ProjectModelSlab
        {
            CoverageDescription = ceiling.CoverageDescription,
            ThicknessCm = ceiling.ThicknessCm,
            Concrete = ceiling.Concrete,
            SteelBottomKg = ceiling.SteelBottomKg,
            SteelTopKg = ceiling.SteelTopKg,
            SteelDiameterMm = ceiling.SteelDiameterMm
        };
    }

    private static void SyncWarnings(ProjectModel model)
    {
        if (model.Warnings.Count > 0)
        {
            return;
        }

        foreach (ProjectModelConflict conflict in model.Conflicts)
        {
            string message = string.IsNullOrWhiteSpace(conflict.ValueB)
                ? $"{conflict.FieldPath}: {conflict.ValueA}"
                : $"{conflict.FieldPath}: {conflict.ValueA} vs {conflict.ValueB}";

            model.Warnings.Add(new ProjectModelWarning
            {
                Code = "conflict",
                Message = message,
                Severity = "warning"
            });
        }

        foreach (string missing in model.MissingData)
        {
            if (string.IsNullOrWhiteSpace(missing))
            {
                continue;
            }

            model.Warnings.Add(new ProjectModelWarning
            {
                Code = "missing_data",
                Message = missing,
                Severity = "warning"
            });
        }
    }

    private static void PopulateExtractionMetadata(
        ProjectModel model,
        ProjectTechnicalDocumentationDetails details,
        IReadOnlyList<string>? thematicGroups)
    {
        List<string> groups = thematicGroups is { Count: > 0 }
            ? thematicGroups.ToList()
            :
            [
                TechnicalDocumentationOptions.ThematicGroups.Reinforcement,
                TechnicalDocumentationOptions.ThematicGroups.RoofStructure,
                TechnicalDocumentationOptions.ThematicGroups.FloorPlans,
                TechnicalDocumentationOptions.ThematicGroups.Sections,
                TechnicalDocumentationOptions.ThematicGroups.Elevations,
                TechnicalDocumentationOptions.ThematicGroups.Foundations,
                TechnicalDocumentationOptions.ThematicGroups.Site,
                TechnicalDocumentationOptions.ThematicGroups.Other
            ];

        model.ExtractionMetadata = new ProjectModelExtractionMetadata
        {
            PipelineVersion = PipelineVersion,
            ThematicGroups = groups,
            TokenUsage = details.TokenUsage > 0 ? details.TokenUsage : null,
            ProcessedAt = details.ProcessedAt
        };
    }

    private static List<ProjectModelElevation> MapElevationsFromDrawings(
        IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<ProjectModelElevation> elevations = new();

        foreach (FloorPlanDrawing drawing in drawings)
        {
            if (drawing.Elevation is null)
            {
                continue;
            }

            string orientation = drawing.Elevation.Title
                ?? drawing.Classification.Title
                ?? drawing.Classification.DrawingType
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(orientation)
                && drawing.Elevation.Finishes.Count == 0
                && drawing.Openings.Count == 0)
            {
                continue;
            }

            ProjectModelElevation elevation = new()
            {
                Orientation = orientation,
                SourceDrawing = drawing.Classification.SheetNumber,
                Finishes = drawing.Elevation.Finishes
                    .Select(finish => new ProjectModelElevationFinish
                    {
                        Zone = finish.Zone,
                        Material = finish.Material,
                        Color = finish.Color
                    })
                    .ToList(),
                Openings = drawing.Openings
                    .Select(opening => new ProjectModelElevationOpening
                    {
                        Type = opening.Type,
                        Count = opening.Count > 0 ? opening.Count : 1,
                        WidthCm = opening.WidthCm > 0 ? opening.WidthCm : null,
                        HeightCm = opening.HeightCm > 0 ? opening.HeightCm : null,
                        Location = opening.Location ?? orientation
                    })
                    .ToList()
            };

            elevations.Add(elevation);
        }

        return elevations;
    }
}
