using System.Text.Json.Serialization;
using Business.Implementation.Helpers.JsonConverters;

namespace Business.Interfaces.WebModels.TechnicalDocumentation.Validation;

public sealed class FieldDisagreement
{
    public string FieldPath { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? ValueA { get; set; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? ValueB { get; set; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Resolved { get; set; }

    public string? ResolutionNote { get; set; }
}
