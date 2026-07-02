using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class InstallationsSummaryJsonConverter : JsonConverter<InstallationsSummary>
{
    public override InstallationsSummary Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new InstallationsSummary();
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return new InstallationsSummary();
        }

        InstallationsSummary summary = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "ventilation", out JsonElement ventilationElement)
            && ventilationElement.ValueKind == JsonValueKind.Object)
        {
            summary.Ventilation = JsonSerializer.Deserialize<InstallationVentilationSummary>(
                ventilationElement.GetRawText(),
                options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "plumbing", out JsonElement plumbingElement)
            && plumbingElement.ValueKind == JsonValueKind.Object)
        {
            summary.Plumbing = JsonSerializer.Deserialize<InstallationPlumbingSummary>(
                plumbingElement.GetRawText(),
                options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "electrical", out JsonElement electricalElement)
            && electricalElement.ValueKind == JsonValueKind.Object)
        {
            summary.Electrical = JsonSerializer.Deserialize<InstallationElectricalSummary>(
                electricalElement.GetRawText(),
                options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "heating", out JsonElement heatingElement)
            && heatingElement.ValueKind == JsonValueKind.Object)
        {
            summary.Heating = JsonSerializer.Deserialize<InstallationHeatingSummary>(
                heatingElement.GetRawText(),
                options);
        }

        return summary;
    }

    public override void Write(
        Utf8JsonWriter writer,
        InstallationsSummary value,
        JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<InstallationsSummary, InstallationsSummaryJsonConverter>(
            writer,
            value,
            options);
    }
}
