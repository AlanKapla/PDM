using System.Globalization;
using System.Text.Json;

namespace Business.Implementation.Helpers.JsonConverters;

internal static class JsonParsingHelpers
{
    public static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static double ReadDouble(JsonElement element, double defaultValue = 0)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String when double.TryParse(
                element.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed) => parsed,
            JsonValueKind.String when double.TryParse(
                element.GetString(),
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out double parsedLocal) => parsedLocal,
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => defaultValue
        };
    }

    public static double? ReadNullableDouble(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => ReadDouble(element)
        };
    }

    public static int ReadInt(JsonElement element, int defaultValue = 0)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out int value) ? value : (int)element.GetDouble(),
            JsonValueKind.String when int.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed) => parsed,
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => defaultValue
        };
    }

    public static int? ReadNullableInt(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => ReadInt(element)
        };
    }

    public static string ReadString(JsonElement element, string defaultValue = "")
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? defaultValue,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => defaultValue,
            _ => defaultValue
        };
    }

    public static string? ReadFlexibleString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => JoinFlexibleArray(element),
            JsonValueKind.Object => FormatTechnicalParametersObject(element),
            _ => null
        };
    }

    private static string? JoinFlexibleArray(JsonElement element)
    {
        List<string> parts = new();

        foreach (JsonElement child in element.EnumerateArray())
        {
            string? part = ReadFlexibleString(child);
            if (!string.IsNullOrWhiteSpace(part))
            {
                parts.Add(part);
            }
        }

        return parts.Count > 0 ? string.Join("; ", parts) : null;
    }

    public static string? ReadTechnicalParameters(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Object when IsEmptyObject(element) => null,
            JsonValueKind.Object => FormatTechnicalParametersObject(element),
            _ => ReadString(element)
        };
    }

    private static string FormatTechnicalParametersObject(JsonElement element)
    {
        System.Text.StringBuilder builder = new();

        foreach (JsonProperty property in element.EnumerateObject())
        {
            string value = ReadString(property.Value);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append(property.Name);
            builder.Append('=');
            builder.Append(value.Trim());
        }

        return builder.Length > 0 ? builder.ToString() : string.Empty;
    }

    public static bool ReadBool(JsonElement element, bool defaultValue = false)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.GetString(), out bool parsed) => parsed,
            JsonValueKind.Number => element.GetDouble() != 0,
            _ => defaultValue
        };
    }

    public static List<T> ReadList<T>(
        JsonElement element,
        Func<JsonElement, T?> itemFactory)
        where T : class
    {
        List<T> items = new();

        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return items;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                T? item = itemFactory(child);
                if (item is not null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            T? item = itemFactory(element);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public static bool IsEmptyObject(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object && !element.EnumerateObject().Any();
    }

    public static List<string> ReadStringList(JsonElement element)
    {
        List<string> values = new();

        if (element.ValueKind != JsonValueKind.Array)
        {
            return values;
        }

        foreach (JsonElement child in element.EnumerateArray())
        {
            string value = ReadString(child);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }
}
