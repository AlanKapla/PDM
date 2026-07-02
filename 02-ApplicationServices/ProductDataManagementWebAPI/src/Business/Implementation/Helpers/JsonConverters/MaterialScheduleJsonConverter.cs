using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Implementation.Helpers.JsonConverters;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class MaterialScheduleJsonConverter : JsonConverter<MaterialSchedule>
{
    public override MaterialSchedule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new MaterialSchedule();
        }

        JsonElement root;
        try
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return new MaterialSchedule();
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return new MaterialSchedule();
        }

        JsonSerializerOptions nestedOptions = JsonConverterWriteHelper.WithoutConverter<MaterialScheduleJsonConverter>(options);
        MaterialSchedule schedule = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "projectId", out JsonElement projectIdElement)
            && projectIdElement.ValueKind == JsonValueKind.String)
        {
            schedule.ProjectId = projectIdElement.GetString() ?? string.Empty;
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "calculatedAt", out JsonElement calculatedAtElement)
            && calculatedAtElement.ValueKind == JsonValueKind.String
            && DateTime.TryParse(calculatedAtElement.GetString(), out DateTime calculatedAt))
        {
            schedule.CalculatedAt = calculatedAt;
        }

        schedule.DrawingsUsed = ReadStringList(root, "drawingsUsed");
        schedule.MissingDrawings = ReadStringList(root, "missingDrawings");
        schedule.MissingDimensions = ReadStringList(root, "missingDimensions");
        schedule.Assumptions = ReadStringList(root, "assumptions");
        schedule.Warnings = ReadStringList(root, "warnings");
        schedule.Summary = ReadList(root, "summary", nestedOptions, new List<MaterialSummaryItem>());
        schedule.Openings = ReadList(root, "openings", nestedOptions, new List<OpeningScheduleItem>());
        schedule.Masonry = ReadMaterialItems(root, "masonry", nestedOptions);
        schedule.Insulation = ReadMaterialItems(root, "insulation", nestedOptions);
        schedule.Concrete = ReadMaterialItems(root, "concrete", nestedOptions);
        schedule.Steel = ReadMaterialItems(root, "steel", nestedOptions);
        schedule.Timber = ReadMaterialItems(root, "timber", nestedOptions);
        schedule.Roofing = ReadMaterialItems(root, "roofing", nestedOptions);
        schedule.Finishes = ReadMaterialItems(root, "finishes", nestedOptions);

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "foundations", out JsonElement foundationsElement))
        {
            schedule.Foundations = ReadSection(foundationsElement, nestedOptions, new FoundationMaterials());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "walls", out JsonElement wallsElement))
        {
            schedule.Walls = ReadSection(wallsElement, nestedOptions, new WallMaterials());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "ceilings", out JsonElement ceilingsElement))
        {
            schedule.Ceilings = ReadSection(ceilingsElement, nestedOptions, new CeilingMaterials());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "columns", out JsonElement columnsElement))
        {
            schedule.Columns = ReadSection(columnsElement, nestedOptions, new ColumnMaterials());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "roof", out JsonElement roofElement))
        {
            schedule.Roof = ReadSection(roofElement, nestedOptions, new RoofMaterials());
        }

        return schedule;
    }

    public override void Write(Utf8JsonWriter writer, MaterialSchedule value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<MaterialSchedule, MaterialScheduleJsonConverter>(writer, value, options);
    }

    private static List<string> ReadStringList(JsonElement root, string propertyName)
    {
        if (!JsonParsingHelpers.TryGetPropertyIgnoreCase(root, propertyName, out JsonElement element)
            || element.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        List<string> values = new();
        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                string? value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
        }

        return values;
    }

    private static List<MaterialItem> ReadMaterialItems(
        JsonElement root,
        string propertyName,
        JsonSerializerOptions options)
    {
        return ReadList(root, propertyName, options, new List<MaterialItem>());
    }

    private static List<T> ReadList<T>(
        JsonElement root,
        string propertyName,
        JsonSerializerOptions options,
        List<T> fallback)
    {
        if (!JsonParsingHelpers.TryGetPropertyIgnoreCase(root, propertyName, out JsonElement element))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(element.GetRawText(), options) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static T ReadSection<T>(JsonElement element, JsonSerializerOptions options, T fallback)
        where T : class
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText(), options) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
