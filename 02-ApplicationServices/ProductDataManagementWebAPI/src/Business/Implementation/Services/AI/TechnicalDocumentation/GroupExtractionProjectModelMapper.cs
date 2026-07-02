using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class GroupExtractionProjectModelMapper
{
    public static void ApplyGroupJson(ProjectModel model, string json, string? groupName = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonElement payload = ResolvePayload(root, groupName);

            MapFloorPlans(model, payload);
            MapElevations(model, payload);
            MapSections(model, payload);
            MapFoundations(model, payload);
            MapReinforcement(model, payload);
            MapRoofStructure(model, payload);
            MapRootWarnings(model, root);
        }
        catch (JsonException)
        {
            // Partial / invalid JSON from a single group — other groups may still contribute.
        }
    }

    private static JsonElement ResolvePayload(JsonElement root, string? groupName)
    {
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("projectModel", out JsonElement projectModel))
        {
            return projectModel;
        }

        return root;
    }

    private static void MapFloorPlans(ProjectModel model, JsonElement payload)
    {
        if (!payload.TryGetProperty("floorPlans", out JsonElement floorPlans)
            || floorPlans.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement floorPlan in floorPlans.EnumerateArray())
        {
            string drawingType = GetString(floorPlan, "type") ?? string.Empty;
            if (drawingType.Contains("rzut_dachu", StringComparison.OrdinalIgnoreCase))
            {
                MapRoofFromFloorPlan(model, floorPlan);
                continue;
            }

            (string level, int order) = ResolveFloorLevel(drawingType, floorPlan);
            ProjectModelFloor floor = new()
            {
                Level = level,
                Order = order,
            };

            if (floorPlan.TryGetProperty("tables", out JsonElement tables)
                && tables.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement table in tables.EnumerateArray())
                {
                    double? totalArea = GetDouble(table, "totalArea", "total_area");
                    if (totalArea is > 0 && floor.TotalAreaM2 is null)
                    {
                        floor.TotalAreaM2 = totalArea;
                    }

                    if (!table.TryGetProperty("rows", out JsonElement rows)
                        || rows.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (JsonElement row in rows.EnumerateArray())
                    {
                        string? roomName = GetString(row, "Nazwa", "name", "nazwa");
                        if (string.IsNullOrWhiteSpace(roomName))
                        {
                            continue;
                        }

                        floor.Rooms.Add(new ProjectModelRoom
                        {
                            Name = roomName,
                            Symbol = GetString(row, "Nr", "nr", "number", "symbol"),
                            AreaM2 = GetDouble(row, "Pow. [m2]", "areaM2", "area_m2", "pow"),
                        });
                    }
                }
            }

            if (floor.TotalAreaM2 is null or <= 0 && floor.Rooms.Count > 0)
            {
                floor.TotalAreaM2 = Math.Round(floor.Rooms.Sum(room => room.AreaM2 ?? 0), 2);
            }

            if (floor.Rooms.Count > 0 || floor.TotalAreaM2 is > 0)
            {
                model.Floors.Add(floor);
            }
        }
    }

    private static void MapRoofFromFloorPlan(ProjectModel model, JsonElement floorPlan)
    {
        double? roofArea = GetDouble(floorPlan, "roofArea", "roof_area");
        if (roofArea is > 0)
        {
            model.Roof.AreaM2 = roofArea;
        }
    }

    private static (string Level, int Order) ResolveFloorLevel(string drawingType, JsonElement floorPlan)
    {
        string? name = GetString(floorPlan, "name", "title");
        string combined = $"{drawingType} {name}".ToLowerInvariant();

        if (combined.Contains("poddasz", StringComparison.Ordinal))
        {
            return ("Poddasze", 1);
        }

        if (combined.Contains("piętro", StringComparison.Ordinal) || combined.Contains("pietro", StringComparison.Ordinal))
        {
            return ("Piętro", 2);
        }

        if (combined.Contains("parter", StringComparison.Ordinal))
        {
            return ("Parter", 0);
        }

        return (name ?? drawingType, 0);
    }

    private static void MapElevations(ProjectModel model, JsonElement payload)
    {
        if (!payload.TryGetProperty("elevations", out JsonElement elevations)
            || elevations.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement elevation in elevations.EnumerateArray())
        {
            string orientation = GetString(elevation, "title", "name", "orientation") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(orientation))
            {
                continue;
            }

            ProjectModelElevation mapped = new()
            {
                Orientation = orientation,
                SourceDrawing = GetString(elevation, "drawingNumber", "drawing_number", "id"),
            };

            if (elevation.TryGetProperty("materials", out JsonElement materials)
                && materials.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement material in materials.EnumerateArray())
                {
                    string? type = GetString(material, "type", "material");
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    mapped.Finishes.Add(new ProjectModelElevationFinish
                    {
                        Zone = $"Strefa {++index}",
                        Material = type,
                        Color = GetString(material, "color", "colour"),
                    });
                }
            }

            model.Elevations.Add(mapped);
        }
    }

    private static void MapSections(ProjectModel model, JsonElement payload)
    {
        if (!payload.TryGetProperty("sections", out JsonElement sections)
            || sections.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement section in sections.EnumerateArray())
        {
            if (!section.TryGetProperty("materials", out JsonElement materials)
                || materials.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (JsonProperty zone in materials.EnumerateObject())
            {
                if (zone.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                string description = zone.Value.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                foreach (string layerMaterial in description.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    model.Walls.External.Layers.Add(new ProjectModelWallLayer
                    {
                        Material = layerMaterial,
                    });
                }
            }
        }
    }

    private static void MapFoundations(ProjectModel model, JsonElement payload)
    {
        if (!payload.TryGetProperty("foundations", out JsonElement foundations))
        {
            return;
        }

        if (foundations.TryGetProperty("drawings", out JsonElement drawings)
            && drawings.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement drawing in drawings.EnumerateArray())
            {
                if (!drawing.TryGetProperty("notes", out JsonElement notes)
                    || notes.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                string sheet = GetString(drawing, "drawingNumber", "drawing_number", "id") ?? "fundamenty";
                foreach (JsonElement note in notes.EnumerateArray())
                {
                    string? text = note.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(model.Foundations.Concrete)
                        && text.Contains("B25", StringComparison.OrdinalIgnoreCase))
                    {
                        model.Foundations.Concrete = "B25";
                    }

                    model.Warnings.Add(new ProjectModelWarning
                    {
                        Code = "foundation_note",
                        Message = $"{sheet}: {text}",
                        Severity = "info",
                        SourceGroup = "foundations",
                    });
                }
            }
        }
    }

    private static void MapReinforcement(ProjectModel model, JsonElement payload)
    {
        JsonElement reinforcement = payload;
        if (payload.TryGetProperty("reinforcement", out JsonElement nested))
        {
            reinforcement = nested;
        }

        if (!reinforcement.TryGetProperty("drawings", out JsonElement drawings)
            || drawings.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement drawing in drawings.EnumerateArray())
        {
            double? totalMass = ExtractTableTotalMass(drawing);
            if (totalMass is not > 0)
            {
                continue;
            }

            string id = GetString(drawing, "id", "drawingNumber", "drawing_number") ?? string.Empty;
            string type = GetString(drawing, "type", "name") ?? string.Empty;
            string name = GetString(drawing, "name", "title") ?? id;

            ProjectModelCeiling ceiling = new()
            {
                CoverageDescription = name,
                ThicknessCm = 18,
            };

            if (IsTopReinforcement(type, id, name))
            {
                ceiling.SteelTopKg = totalMass;
                model.Ceilings.Add(ceiling);
                continue;
            }

            ceiling.SteelBottomKg = totalMass;
            model.Ceilings.Add(ceiling);

            if (model.Slab is null)
            {
                model.Slab = new ProjectModelSlab
                {
                    CoverageDescription = name,
                    ThicknessCm = 18,
                    SteelBottomKg = totalMass,
                };
            }
        }
    }

    private static void MapRoofStructure(ProjectModel model, JsonElement payload)
    {
        JsonElement roofStructure = payload;
        if (payload.TryGetProperty("roofStructure", out JsonElement nested))
        {
            roofStructure = nested;
        }

        if (!roofStructure.TryGetProperty("drawings", out JsonElement drawings)
            || drawings.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement drawing in drawings.EnumerateArray())
        {
            if (!drawing.TryGetProperty("details", out JsonElement details))
            {
                continue;
            }

            double? roofArea = ParseAreaValue(GetString(details, "roofArea", "roof_area"));
            if (roofArea is > 0)
            {
                model.Roof.AreaM2 = roofArea;
            }

            double? pitch = ParsePitchDegrees(GetString(details, "roofSlope", "roof_slope", "pitchDegrees"));
            if (pitch is > 0)
            {
                model.Roof.PitchDegrees = pitch;
            }

            if (details.TryGetProperty("woodList", out JsonElement woodList)
                && woodList.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement wood in woodList.EnumerateArray())
                {
                    model.Roof.TimberGroups.Add(new ProjectModelTimberGroup
                    {
                        Element = GetString(wood, "type", "element") ?? "Element drewniany",
                        Section = GetString(wood, "dimensions", "section"),
                        Count = (int?)GetDouble(wood, "quantity", "count"),
                        LengthM = GetDouble(wood, "length", "lengthM"),
                        VolumeM3 = ParseVolumeM3(GetString(wood, "volume", "volumeM3")),
                    });
                }
            }

            double? totalVolume = ParseVolumeM3(GetString(details, "totalVolume", "total_volume"));
            if (totalVolume is > 0)
            {
                model.Roof.TotalTimberVolumeM3 = totalVolume;
            }
        }
    }

    private static void MapRootWarnings(ProjectModel model, JsonElement root)
    {
        if (!root.TryGetProperty("warnings", out JsonElement warnings)
            || warnings.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement warning in warnings.EnumerateArray())
        {
            string? message = warning.ValueKind == JsonValueKind.String
                ? warning.GetString()
                : GetString(warning, "message");

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            model.Warnings.Add(new ProjectModelWarning
            {
                Message = message,
                Severity = "warning",
            });
        }
    }

    private static double? ExtractTableTotalMass(JsonElement drawing)
    {
        if (!drawing.TryGetProperty("tables", out JsonElement tables)
            || tables.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement table in tables.EnumerateArray())
        {
            double? totalMass = GetDouble(table, "totalMass", "total_mass", "total_mass_printed_kg");
            if (totalMass is > 0)
            {
                return totalMass;
            }
        }

        return null;
    }

    private static bool IsTopReinforcement(string type, string id, string name)
    {
        string combined = $"{type} {id} {name}".ToLowerInvariant();
        return combined.Contains("gorn", StringComparison.Ordinal)
            || combined.Contains("górn", StringComparison.Ordinal)
            || combined.Contains("k-03", StringComparison.Ordinal)
            || combined.Contains("k03", StringComparison.Ordinal);
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (string propertyName in propertyNames)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    return property.Value.GetRawText();
                }
            }
        }

        return null;
    }

    private static double? GetDouble(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (string propertyName in propertyNames)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.Number
                    && property.Value.TryGetDouble(out double number))
                {
                    return number;
                }

                if (property.Value.ValueKind == JsonValueKind.String
                    && double.TryParse(property.Value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }

    private static double? ParseAreaValue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        Match match = Regex.Match(raw, @"\d+(?:[.,]\d+)?");
        if (!match.Success)
        {
            return null;
        }

        string digits = match.Value.Replace(',', '.');
        return double.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out double value)
            ? value
            : null;
    }

    private static double? ParsePitchDegrees(string? raw)
    {
        return ParseAreaValue(raw);
    }

    private static double? ParseVolumeM3(string? raw)
    {
        return ParseAreaValue(raw);
    }
}
