using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation.Validation;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class ValidationReportJsonConverter : JsonConverter<ValidationReport>
{
    public override ValidationReport Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new ValidationReport();
        }

        JsonElement root;
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
            root = document.RootElement.Clone();
        }

        ValidationReport report = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "totalFields", out JsonElement totalElement))
        {
            report.TotalFields = JsonParsingHelpers.ReadInt(totalElement);
        }

        report.HighConfidence = ReadCount(root, "highConfidence", "agreeingFields");
        report.MediumConfidence = ReadCount(root, "mediumConfidence", "disagreementsResolved");
        report.LowConfidence = ReadCount(root, "lowConfidence", "lowConfidenceFields");

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "disagreements", out JsonElement disagreementsElement)
            && disagreementsElement.ValueKind == JsonValueKind.Array)
        {
            report.Disagreements = JsonParsingHelpers.ReadList(disagreementsElement, ParseDisagreement);
        }

        return report;
    }

    public override void Write(Utf8JsonWriter writer, ValidationReport value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<ValidationReport, ValidationReportJsonConverter>(writer, value, options);
    }

    private static int ReadCount(JsonElement root, string primaryName, string legacyName)
    {
        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, primaryName, out JsonElement element))
        {
            return JsonParsingHelpers.ReadInt(element);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, legacyName, out JsonElement legacyElement))
        {
            return JsonParsingHelpers.ReadInt(legacyElement);
        }

        return 0;
    }

    private static FieldDisagreement ParseDisagreement(JsonElement element)
    {
        FieldDisagreement disagreement = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "fieldPath", out JsonElement fieldPathElement))
        {
            disagreement.FieldPath = JsonParsingHelpers.ReadString(fieldPathElement);
        }

        disagreement.ValueA = ReadFlexibleString(element, "valueA", "valueFromAgentA");
        disagreement.ValueB = ReadFlexibleString(element, "valueB", "valueFromAgentB");
        disagreement.Resolved = ReadFlexibleString(element, "resolved", "resolvedValue");

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "resolutionNote", out JsonElement noteElement))
        {
            disagreement.ResolutionNote = JsonParsingHelpers.ReadString(noteElement);
        }

        return disagreement;
    }

    private static string? ReadFlexibleString(JsonElement element, string primaryName, string legacyName)
    {
        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, primaryName, out JsonElement valueElement))
        {
            return JsonParsingHelpers.ReadString(valueElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, legacyName, out JsonElement legacyElement))
        {
            return JsonParsingHelpers.ReadString(legacyElement);
        }

        return null;
    }
}
