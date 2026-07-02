using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Implementation.Helpers.JsonConverters;

/// <summary>
/// Akceptuje string, number, bool lub obiekt JSON i konwertuje do string (dla pól z odpowiedzi LLM).
/// </summary>
public sealed class FlexibleStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long longValue))
                {
                    return longValue.ToString(CultureInfo.InvariantCulture);
                }

                return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
                using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.GetRawText();
                }
            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }
}
