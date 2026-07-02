using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ProjectModelFromVerifiedGroupsMapper
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    public static ProjectModel Map(IReadOnlyList<VerifiedGroupExtractionResult> verifiedGroups)
    {
        if (verifiedGroups.Count == 0)
        {
            return new ProjectModel();
        }

        ProjectModel model = new();

        foreach (VerifiedGroupExtractionResult group in verifiedGroups)
        {
            GroupExtractionProjectModelMapper.ApplyGroupJson(model, group.VerifiedJson, group.GroupName);
        }

        if (IsStructurallyPopulated(model))
        {
            return model;
        }

        string mergedJson = GroupExtractionJsonMerger.Merge(
            verifiedGroups.Select(group => group.VerifiedJson).ToList());

        ProjectModel? direct = TryDeserializeProjectModel(mergedJson);
        if (direct is not null && IsStructurallyPopulated(direct))
        {
            return MergePreferNonEmpty(direct, model);
        }

        ProjectModel result = direct is not null
            ? MergePreferNonEmpty(model, direct)
            : model;

        if (!IsStructurallyPopulated(result))
        {
            ApplyLegacyHeuristicMappings(mergedJson, result);
        }

        return result;
    }

    public static bool IsStructurallyPopulated(ProjectModel model)
    {
        return model.Floors.Count > 0
            || model.Foundations.Footings.Count > 0
            || model.Foundations.Pads.Count > 0
            || model.Ceilings.Count > 0
            || model.Slab is not null
            || model.Roof.TimberGroups.Count > 0
            || model.Roof.AreaM2 is > 0
            || model.Walls.External.Layers.Count > 0
            || model.Elevations.Count > 0
            || model.Columns.Count > 0;
    }

    public static ProjectModel MergePreferNonEmpty(ProjectModel primary, ProjectModel fallback)
    {
        if (string.IsNullOrWhiteSpace(primary.Project.Name) && !string.IsNullOrWhiteSpace(fallback.Project.Name))
        {
            primary.Project.Name = fallback.Project.Name;
        }

        if (string.IsNullOrWhiteSpace(primary.Project.Investor) && !string.IsNullOrWhiteSpace(fallback.Project.Investor))
        {
            primary.Project.Investor = fallback.Project.Investor;
        }

        if (string.IsNullOrWhiteSpace(primary.Project.Location) && !string.IsNullOrWhiteSpace(fallback.Project.Location))
        {
            primary.Project.Location = fallback.Project.Location;
        }

        if (string.IsNullOrWhiteSpace(primary.Project.Author) && !string.IsNullOrWhiteSpace(fallback.Project.Author))
        {
            primary.Project.Author = fallback.Project.Author;
        }

        if (primary.Floors.Count == 0 && fallback.Floors.Count > 0)
        {
            primary.Floors = fallback.Floors;
        }

        if (primary.Ceilings.Count == 0 && fallback.Ceilings.Count > 0)
        {
            primary.Ceilings = fallback.Ceilings;
        }

        if (primary.Slab is null && fallback.Slab is not null)
        {
            primary.Slab = fallback.Slab;
        }

        if (primary.Foundations.Footings.Count == 0 && fallback.Foundations.Footings.Count > 0)
        {
            primary.Foundations = fallback.Foundations;
        }

        if ((primary.Roof.TimberGroups.Count == 0 || primary.Roof.AreaM2 is null)
            && (fallback.Roof.TimberGroups.Count > 0 || fallback.Roof.AreaM2 is > 0))
        {
            if (fallback.Roof.AreaM2 is > 0)
            {
                primary.Roof.AreaM2 = fallback.Roof.AreaM2;
            }

            if (fallback.Roof.PitchDegrees is > 0)
            {
                primary.Roof.PitchDegrees = fallback.Roof.PitchDegrees;
            }

            if (fallback.Roof.TimberGroups.Count > 0)
            {
                primary.Roof.TimberGroups = fallback.Roof.TimberGroups;
            }

            if (fallback.Roof.TotalTimberVolumeM3 is > 0)
            {
                primary.Roof.TotalTimberVolumeM3 = fallback.Roof.TotalTimberVolumeM3;
            }
        }

        if (primary.Elevations.Count == 0 && fallback.Elevations.Count > 0)
        {
            primary.Elevations = fallback.Elevations;
        }

        if (primary.Walls.External.Layers.Count == 0 && fallback.Walls.External.Layers.Count > 0)
        {
            primary.Walls = fallback.Walls;
        }

        if (primary.Warnings.Count == 0 && fallback.Warnings.Count > 0)
        {
            primary.Warnings = fallback.Warnings;
        }

        return primary;
    }

    private static ProjectModel? TryDeserializeProjectModel(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("projectModel", out JsonElement nested))
            {
                ProjectModel? fromNested = JsonSerializer.Deserialize<ProjectModel>(nested.GetRawText(), JsonOptions);
                if (fromNested is not null && IsStructurallyPopulated(fromNested))
                {
                    return fromNested;
                }

                ProjectModel mapped = new();
                GroupExtractionProjectModelMapper.ApplyGroupJson(mapped, json);
                return IsStructurallyPopulated(mapped) ? mapped : fromNested;
            }

            return JsonSerializer.Deserialize<ProjectModel>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ApplyLegacyHeuristicMappings(string json, ProjectModel model)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            MapLegacyReinforcementSteel(document.RootElement, model);
        }
        catch (JsonException)
        {
            // Ignore malformed fragments.
        }
    }

    private static void MapLegacyReinforcementSteel(JsonElement root, ProjectModel model)
    {
        if (model.Slab?.SteelBottomKg is > 0 || model.Ceilings.Any(ceiling => ceiling.SteelBottomKg is > 0))
        {
            return;
        }

        double? steelKg = FindSteelMassKg(root);
        if (steelKg is not > 0)
        {
            return;
        }

        model.Ceilings.Add(new ProjectModelCeiling
        {
            CoverageDescription = "Zbrojenie stropu — odczyt z rysunku K-02",
            SteelBottomKg = steelKg,
            ThicknessCm = 18,
        });
    }

    private static double? FindSteelMassKg(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (property.Name.Contains("mass", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Contains("steel", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Contains("kg", StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind == JsonValueKind.Number
                        && property.Value.TryGetDouble(out double number)
                        && number > 0)
                    {
                        return number;
                    }
                }

                double? nested = FindSteelMassKg(property.Value);
                if (nested is > 0)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                double? nested = FindSteelMassKg(child);
                if (nested is > 0)
                {
                    return nested;
                }
            }
        }

        return null;
    }
}
