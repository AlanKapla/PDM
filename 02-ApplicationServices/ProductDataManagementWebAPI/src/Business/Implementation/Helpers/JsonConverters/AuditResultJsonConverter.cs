using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class AuditResultJsonConverter : JsonConverter<AuditResult>
{
    public override AuditResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new AuditResult();
        }

        JsonElement root;
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
            root = document.RootElement.Clone();
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return new AuditResult();
        }

        AuditResult result = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "warnings", out JsonElement warningsElement))
        {
            result.Warnings = ReadStringList(warningsElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "missingMaterials", out JsonElement missingMaterialsElement))
        {
            result.MissingMaterials = ReadStringList(missingMaterialsElement);
        }
        else if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "missingData", out JsonElement missingElement))
        {
            result.MissingMaterials = ReadStringList(missingElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "assumptions", out JsonElement assumptionsElement))
        {
            result.Assumptions = ReadStringList(assumptionsElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "crossReferenceErrors", out JsonElement crossRefElement))
        {
            result.CrossReferenceErrors = ReadStringList(crossRefElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "unitErrors", out JsonElement unitErrorsElement)
            && unitErrorsElement.ValueKind == JsonValueKind.Array)
        {
            result.UnitErrors = ReadUnitErrors(unitErrorsElement);
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, AuditResult value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<AuditResult, AuditResultJsonConverter>(writer, value, options);
    }

    private static List<string> ReadStringList(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
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

    private static List<AuditUnitError> ReadUnitErrors(JsonElement element)
    {
        List<AuditUnitError> errors = new();

        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                string? message = item.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    errors.Add(new AuditUnitError { Found = message });
                }

                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            AuditUnitError error = new();

            if (JsonParsingHelpers.TryGetPropertyIgnoreCase(item, "field", out JsonElement fieldElement))
            {
                error.Field = JsonParsingHelpers.ReadString(fieldElement);
            }

            if (JsonParsingHelpers.TryGetPropertyIgnoreCase(item, "found", out JsonElement foundElement))
            {
                error.Found = JsonParsingHelpers.ReadString(foundElement);
            }

            if (JsonParsingHelpers.TryGetPropertyIgnoreCase(item, "expected", out JsonElement expectedElement))
            {
                error.Expected = JsonParsingHelpers.ReadString(expectedElement);
            }

            errors.Add(error);
        }

        return errors;
    }
}
