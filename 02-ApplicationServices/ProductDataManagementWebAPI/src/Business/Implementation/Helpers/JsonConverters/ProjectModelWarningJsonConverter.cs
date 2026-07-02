using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class ProjectModelWarningJsonConverter : JsonConverter<ProjectModelWarning>
{
    public override ProjectModelWarning Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new ProjectModelWarning
            {
                Message = reader.GetString() ?? string.Empty,
                Severity = "warning",
            };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return new ProjectModelWarning();
        }

        JsonElement root;
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
            root = document.RootElement.Clone();
        }

        ProjectModelWarning warning = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "code", out JsonElement codeElement))
        {
            warning.Code = JsonParsingHelpers.ReadString(codeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "message", out JsonElement messageElement))
        {
            warning.Message = JsonParsingHelpers.ReadString(messageElement) ?? string.Empty;
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "severity", out JsonElement severityElement))
        {
            warning.Severity = JsonParsingHelpers.ReadString(severityElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "sourceGroup", out JsonElement sourceGroupElement))
        {
            warning.SourceGroup = JsonParsingHelpers.ReadString(sourceGroupElement);
        }

        if (string.IsNullOrWhiteSpace(warning.Message)
            && JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "text", out JsonElement textElement))
        {
            warning.Message = JsonParsingHelpers.ReadString(textElement) ?? string.Empty;
        }

        return warning;
    }

    public override void Write(Utf8JsonWriter writer, ProjectModelWarning value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<ProjectModelWarning, ProjectModelWarningJsonConverter>(
            writer,
            value,
            options);
    }
}
