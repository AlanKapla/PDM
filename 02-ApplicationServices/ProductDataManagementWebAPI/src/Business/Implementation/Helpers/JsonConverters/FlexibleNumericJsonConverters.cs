using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class FlexibleDoubleJsonConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when double.TryParse(
                reader.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed) => parsed,
            JsonTokenType.String when double.TryParse(
                reader.GetString(),
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out double parsedLocal) => parsedLocal,
            JsonTokenType.True => 1,
            JsonTokenType.False => 0,
            JsonTokenType.Null => 0,
            _ => 0
        };
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public sealed class FlexibleNullableDoubleJsonConverter : JsonConverter<double?>
{
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return new FlexibleDoubleJsonConverter().Read(ref reader, typeof(double), options);
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public sealed class FlexibleIntJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetInt32(out int value) ? value : (int)reader.GetDouble(),
            JsonTokenType.String when int.TryParse(
                reader.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed) => parsed,
            JsonTokenType.True => 1,
            JsonTokenType.False => 0,
            JsonTokenType.Null => 0,
            _ => 0
        };
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
