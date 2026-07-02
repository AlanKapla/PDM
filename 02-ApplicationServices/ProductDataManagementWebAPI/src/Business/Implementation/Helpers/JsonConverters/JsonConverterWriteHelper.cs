using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Business.Implementation.Helpers.JsonConverters;

internal static class JsonConverterWriteHelper
{
    private static readonly FieldInfo? ConverterBackingField = typeof(JsonTypeInfo).GetField(
        "<Converter>k__BackingField",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static JsonSerializerOptions WithoutConverter<TConverter>(JsonSerializerOptions options)
        where TConverter : JsonConverter
    {
        return WithoutConverter(options, typeof(TConverter));
    }

    public static JsonSerializerOptions WithoutConverter(JsonSerializerOptions options, Type converterType)
    {
        JsonSerializerOptions fallback = new(options);

        for (int index = fallback.Converters.Count - 1; index >= 0; index--)
        {
            if (converterType.IsInstanceOfType(fallback.Converters[index]))
            {
                fallback.Converters.RemoveAt(index);
            }
        }

        return fallback;
    }

    public static void SerializeWithoutConverter<T>(
        Utf8JsonWriter writer,
        T value,
        JsonSerializerOptions options,
        Type converterType)
    {
        JsonSerializerOptions writeOptions = WithoutConverter(options, converterType);
        writeOptions.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();

        DefaultJsonTypeInfoResolver resolver = (DefaultJsonTypeInfoResolver)writeOptions.TypeInfoResolver;
        JsonTypeInfo typeInfo = resolver.GetTypeInfo(typeof(T), writeOptions)
            ?? throw new InvalidOperationException($"Missing JSON contract for type {typeof(T).Name}.");

        if (converterType.IsInstanceOfType(typeInfo.Converter))
        {
            JsonTypeInfo attributeFreeTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(typeof(T), writeOptions);
            ReplaceConverter(typeInfo, attributeFreeTypeInfo.Converter);
        }

        JsonSerializer.Serialize(writer, value, typeInfo);
    }

    public static void SerializeWithoutConverter<T, TConverter>(
        Utf8JsonWriter writer,
        T value,
        JsonSerializerOptions options)
        where TConverter : JsonConverter
    {
        SerializeWithoutConverter<T>(writer, value, options, typeof(TConverter));
    }

    private static void ReplaceConverter(JsonTypeInfo typeInfo, JsonConverter replacement)
    {
        if (ConverterBackingField is null)
        {
            throw new InvalidOperationException("Unable to replace JsonTypeInfo converter.");
        }

        ConverterBackingField.SetValue(typeInfo, replacement);
    }
}
