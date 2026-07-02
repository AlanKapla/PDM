using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class MaterialQuantityJsonConverter : JsonConverter<MaterialQuantity>
{
    public override MaterialQuantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new MaterialQuantity();
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return ParseMaterialQuantity(document.RootElement);
    }

    public override void Write(Utf8JsonWriter writer, MaterialQuantity value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<MaterialQuantity, MaterialQuantityJsonConverter>(writer, value, options);
    }

    internal static MaterialQuantity ParseMaterialQuantity(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new MaterialQuantity
            {
                MaterialType = JsonParsingHelpers.ReadString(element)
            };
        }

        string materialType = string.Empty;
        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "materialType", out JsonElement materialTypeElement))
        {
            materialType = JsonParsingHelpers.ReadString(materialTypeElement);
        }
        else if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "type", out JsonElement typeElement))
        {
            materialType = JsonParsingHelpers.ReadString(typeElement);
        }
        else if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "material", out JsonElement materialElement))
        {
            materialType = JsonParsingHelpers.ReadString(materialElement);
        }

        double quantity = 0;
        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "quantity", out JsonElement quantityElement))
        {
            quantity = JsonParsingHelpers.ReadDouble(quantityElement);
        }
        else if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "amount", out JsonElement amountElement))
        {
            quantity = JsonParsingHelpers.ReadDouble(amountElement);
        }

        string unit = "szt";
        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "unit", out JsonElement unitElement))
        {
            unit = JsonParsingHelpers.ReadString(unitElement, "szt");
        }

        return new MaterialQuantity
        {
            MaterialType = materialType,
            Quantity = quantity,
            Unit = unit
        };
    }
}

public sealed class MaterialQuantityListJsonConverter : JsonConverter<List<MaterialQuantity>>
{
    public override List<MaterialQuantity> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new List<MaterialQuantity>();
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        return JsonParsingHelpers.ReadList(root, element =>
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return MaterialQuantityJsonConverter.ParseMaterialQuantity(element);
        });
    }

    public override void Write(Utf8JsonWriter writer, List<MaterialQuantity> value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<List<MaterialQuantity>, MaterialQuantityListJsonConverter>(
            writer,
            value,
            options);
    }
}
