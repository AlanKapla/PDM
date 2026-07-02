using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class RoofSectionJsonConverter : JsonConverter<RoofSection?>
{
    public override RoofSection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind == JsonValueKind.String || root.ValueKind == JsonValueKind.Number)
        {
            return null;
        }

        if (JsonParsingHelpers.IsEmptyObject(root))
        {
            return null;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            return new RoofSection
            {
                Timber = JsonParsingHelpers.ReadList(root, ParseTimber)
            };
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        RoofSection section = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "areaM2", out JsonElement areaElement))
        {
            section.AreaM2 = JsonParsingHelpers.ReadDouble(areaElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "pitchDegrees", out JsonElement pitchElement))
        {
            section.PitchDegrees = JsonParsingHelpers.ReadDouble(pitchElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "coveringType", out JsonElement coveringElement))
        {
            section.CoveringType = JsonParsingHelpers.ReadString(coveringElement);
        }
        else if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "covering", out JsonElement coveringAltElement))
        {
            section.CoveringType = JsonParsingHelpers.ReadString(coveringAltElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "woodClass", out JsonElement woodClassElement))
        {
            section.WoodClass = JsonParsingHelpers.ReadString(woodClassElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "notes", out JsonElement notesElement))
        {
            section.Notes = JsonParsingHelpers.ReadString(notesElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "totalVolumeM3", out JsonElement totalVolumeElement))
        {
            section.TotalVolumeM3 = JsonParsingHelpers.ReadNullableDouble(totalVolumeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "timberGroups", out JsonElement timberGroupsElement))
        {
            section.TimberGroups = JsonParsingHelpers.ReadList(timberGroupsElement, child => ParseTimberGroup(child, options));
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "timber", out JsonElement timberElement))
        {
            section.Timber = JsonParsingHelpers.ReadList(timberElement, ParseTimber);
        }

        return HasAnyData(section) ? section : null;
    }

    public override void Write(Utf8JsonWriter writer, RoofSection? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonConverterWriteHelper.SerializeWithoutConverter<RoofSection, RoofSectionJsonConverter>(writer, value, options);
    }

    private static TimberElement? ParseTimber(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        TimberElement timber = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "element", out JsonElement elementName))
        {
            timber.Element = JsonParsingHelpers.ReadString(elementName);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "section", out JsonElement sectionElement))
        {
            timber.Section = JsonParsingHelpers.ReadString(sectionElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "lengthM", out JsonElement lengthElement))
        {
            timber.LengthM = JsonParsingHelpers.ReadDouble(lengthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "count", out JsonElement countElement))
        {
            timber.Count = JsonParsingHelpers.ReadInt(countElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "woodType", out JsonElement woodTypeElement))
        {
            timber.WoodType = JsonParsingHelpers.ReadString(woodTypeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "rowSumMb", out JsonElement rowSumElement))
        {
            timber.RowSumMb = JsonParsingHelpers.ReadNullableDouble(rowSumElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "volumeM3", out JsonElement volumeElement))
        {
            timber.VolumeM3 = JsonParsingHelpers.ReadNullableDouble(volumeElement);
        }

        return timber;
    }

    private static TimberGroup? ParseTimberGroup(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return JsonSerializer.Deserialize<TimberGroup>(element.GetRawText(), options);
    }

    private static bool HasAnyData(RoofSection section)
    {
        return section.AreaM2 > 0
            || section.PitchDegrees > 0
            || !string.IsNullOrWhiteSpace(section.CoveringType)
            || section.Timber.Count > 0
            || section.TimberGroups.Count > 0
            || section.TotalVolumeM3 is > 0;
    }
}
