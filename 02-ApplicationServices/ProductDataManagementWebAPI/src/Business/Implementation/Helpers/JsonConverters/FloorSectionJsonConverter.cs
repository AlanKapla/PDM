using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class FloorSectionJsonConverter : JsonConverter<FloorSection?>
{
    public override FloorSection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind == JsonValueKind.String || root.ValueKind == JsonValueKind.Number)
        {
            return null;
        }

        if (JsonParsingHelpers.IsEmptyObject(root))
        {
            return null;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            return new FloorSection
            {
                Slabs = JsonParsingHelpers.ReadList(root, ParseSlab)
            };
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        FloorSection section = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "coverageDescription", out JsonElement coverageElement))
        {
            section.CoverageDescription = JsonParsingHelpers.ReadString(coverageElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "basicGrid", out JsonElement gridElement))
        {
            section.BasicGrid = JsonParsingHelpers.ReadString(gridElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "totalMassKg", out JsonElement totalMassElement))
        {
            section.TotalMassKg = JsonParsingHelpers.ReadNullableDouble(totalMassElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "slabs", out JsonElement slabsElement))
        {
            section.Slabs = JsonParsingHelpers.ReadList(slabsElement, ParseSlab);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "bars", out JsonElement barsElement))
        {
            section.Bars = JsonParsingHelpers.ReadList(barsElement, ParseBar);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "concrete", out JsonElement concreteElement))
        {
            section.Concrete = ReadMaterialList(concreteElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "steel", out JsonElement steelElement))
        {
            section.Steel = ReadMaterialList(steelElement);
        }

        return HasAnyData(section) ? section : null;
    }

    public override void Write(Utf8JsonWriter writer, FloorSection? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonConverterWriteHelper.SerializeWithoutConverter<FloorSection, FloorSectionJsonConverter>(writer, value, options);
    }

    private static SlabDetail? ParseSlab(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        SlabDetail slab = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "id", out JsonElement idElement))
        {
            slab.Id = JsonParsingHelpers.ReadString(idElement, Guid.NewGuid().ToString());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "areaM2", out JsonElement areaElement))
        {
            slab.AreaM2 = JsonParsingHelpers.ReadDouble(areaElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "thicknessCm", out JsonElement thicknessElement))
        {
            slab.ThicknessCm = JsonParsingHelpers.ReadDouble(thicknessElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "concreteClass", out JsonElement concreteClassElement))
        {
            slab.ConcreteClass = JsonParsingHelpers.ReadString(concreteClassElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "reinforcement", out JsonElement reinforcementElement))
        {
            slab.Reinforcement = JsonParsingHelpers.ReadFlexibleString(reinforcementElement);
        }

        return slab;
    }

    private static RebarBar? ParseBar(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        RebarBar bar = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "pos", out JsonElement posElement))
        {
            bar.Pos = JsonParsingHelpers.ReadInt(posElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "count", out JsonElement countElement))
        {
            bar.Count = JsonParsingHelpers.ReadNullableInt(countElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "diameterMm", out JsonElement diameterElement))
        {
            bar.DiameterMm = JsonParsingHelpers.ReadNullableDouble(diameterElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "lengthM", out JsonElement lengthElement))
        {
            bar.LengthM = JsonParsingHelpers.ReadNullableDouble(lengthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "totalLengthM", out JsonElement totalLengthElement))
        {
            bar.TotalLengthM = JsonParsingHelpers.ReadNullableDouble(totalLengthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "massKg", out JsonElement massElement))
        {
            bar.MassKg = JsonParsingHelpers.ReadNullableDouble(massElement);
        }

        return bar;
    }

    private static List<MaterialQuantity> ReadMaterialList(JsonElement element)
    {
        return JsonParsingHelpers.ReadList(element, child =>
        {
            if (child.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return MaterialQuantityJsonConverter.ParseMaterialQuantity(child);
        });
    }

    private static bool HasAnyData(FloorSection section)
    {
        return section.Slabs.Count > 0
            || section.Bars.Count > 0
            || section.TotalMassKg is > 0
            || section.Concrete.Count > 0
            || section.Steel.Count > 0
            || !string.IsNullOrWhiteSpace(section.CoverageDescription);
    }
}
