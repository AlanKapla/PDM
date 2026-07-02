using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class DrawingInstallationListJsonConverter : JsonConverter<List<DrawingInstallation>>
{
    public override List<DrawingInstallation> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new List<DrawingInstallation>();
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            return MapFromInstallationsObject(root);
        }

        return JsonParsingHelpers.ReadList(root, ParseInstallation);
    }

    public override void Write(Utf8JsonWriter writer, List<DrawingInstallation> value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<List<DrawingInstallation>, DrawingInstallationListJsonConverter>(
            writer,
            value,
            options);
    }

    private static List<DrawingInstallation> MapFromInstallationsObject(JsonElement root)
    {
        List<DrawingInstallation> installations = new();

        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            DrawingInstallation installation = new()
            {
                Type = NormalizeInstallationType(property.Name),
                IsPresent = true
            };

            ApplyInstallationFields(installation, property.Value);
            installations.Add(installation);
        }

        if (installations.Count > 0)
        {
            return installations;
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "type", out JsonElement _))
        {
            DrawingInstallation? single = ParseInstallation(root);
            if (single is not null)
            {
                installations.Add(single);
            }
        }

        return installations;
    }

    private static void ApplyInstallationFields(DrawingInstallation installation, JsonElement element)
    {
        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "type", out JsonElement typeElement))
        {
            string type = JsonParsingHelpers.ReadString(typeElement);
            if (!string.IsNullOrWhiteSpace(type))
            {
                installation.Type = type;
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "isPresent", out JsonElement isPresentElement))
        {
            installation.IsPresent = JsonParsingHelpers.ReadBool(isPresentElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "notes", out JsonElement notesElement))
        {
            installation.Notes = JsonParsingHelpers.ReadString(notesElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "sourceDrawing", out JsonElement sourceDrawingElement))
        {
            installation.SourceDrawing = JsonParsingHelpers.ReadString(sourceDrawingElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "sourceDrawings", out JsonElement sourceDrawingsElement))
        {
            installation.SourceDrawings = JsonParsingHelpers.ReadStringList(sourceDrawingsElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "floors", out JsonElement floorsElement))
        {
            installation.Floors = JsonParsingHelpers.ReadStringList(floorsElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "sewage", out JsonElement sewageElement)
            && sewageElement.ValueKind == JsonValueKind.Object)
        {
            if (JsonParsingHelpers.TryGetPropertyIgnoreCase(sewageElement, "type", out JsonElement sewageTypeElement))
            {
                installation.SewageType = JsonParsingHelpers.ReadString(sewageTypeElement);
            }

            if (JsonParsingHelpers.TryGetPropertyIgnoreCase(sewageElement, "sourceDrawing", out JsonElement sewageSourceElement))
            {
                installation.SourceDrawing ??= JsonParsingHelpers.ReadString(sewageSourceElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "waterSupply", out JsonElement waterElement)
            && waterElement.ValueKind == JsonValueKind.Object)
        {
            if (JsonParsingHelpers.TryGetPropertyIgnoreCase(waterElement, "type", out JsonElement waterTypeElement))
            {
                installation.WaterSupplyType = JsonParsingHelpers.ReadString(waterTypeElement);
            }

            if (JsonParsingHelpers.TryGetPropertyIgnoreCase(waterElement, "notes", out JsonElement waterNotesElement))
            {
                installation.Notes ??= JsonParsingHelpers.ReadString(waterNotesElement);
            }

            if (JsonParsingHelpers.TryGetPropertyIgnoreCase(waterElement, "sourceDrawing", out JsonElement waterSourceElement))
            {
                installation.SourceDrawing ??= JsonParsingHelpers.ReadString(waterSourceElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "roomNumber", out JsonElement roomNumberElement))
        {
            installation.RoomNumber = JsonParsingHelpers.ReadString(roomNumberElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "areaM2", out JsonElement areaElement))
        {
            installation.AreaM2 = JsonParsingHelpers.ReadNullableDouble(areaElement);
        }
    }

    private static DrawingInstallation? ParseInstallation(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        DrawingInstallation installation = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "type", out JsonElement typeElement))
        {
            installation.Type = JsonParsingHelpers.ReadString(typeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "isPresent", out JsonElement isPresentElement))
        {
            installation.IsPresent = JsonParsingHelpers.ReadBool(isPresentElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "notes", out JsonElement notesElement))
        {
            installation.Notes = JsonParsingHelpers.ReadString(notesElement);
        }

        return installation;
    }

    private static string NormalizeInstallationType(string type)
    {
        string normalized = type.Trim().ToLowerInvariant()
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty);

        if (normalized.Contains("wentyl") || normalized.Contains("rekuper") || normalized.Contains("ventilation"))
        {
            return "wentylacja";
        }

        if (normalized.Contains("wod") || normalized.Contains("kan") || normalized.Contains("sewer") || normalized.Contains("plumbing"))
        {
            return "wod-kan";
        }

        if (normalized.Contains("elektr") || normalized.Contains("electric"))
        {
            return "elektryczna";
        }

        if (normalized.Contains("ogrzew") || normalized.Contains("heat"))
        {
            return "ogrzewanie";
        }

        if (normalized.Contains("klimat") || normalized.Contains("aircondition"))
        {
            return "klimatyzacja";
        }

        return type;
    }
}
