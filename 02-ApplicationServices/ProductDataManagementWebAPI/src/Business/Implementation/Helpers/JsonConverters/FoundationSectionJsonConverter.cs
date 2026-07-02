using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class FoundationSectionJsonConverter : JsonConverter<FoundationSection?>
{
    public override FoundationSection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
            return new FoundationSection
            {
                Footings = JsonParsingHelpers.ReadList(root, ParseFooting)
            };
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        FoundationSection section = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "concreteClass", out JsonElement concreteClassElement))
        {
            section.ConcreteClass = JsonParsingHelpers.ReadString(concreteClassElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "steelSpecification", out JsonElement steelSpecElement))
        {
            section.SteelSpecification = JsonParsingHelpers.ReadString(steelSpecElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "coverageMm", out JsonElement coverageElement))
        {
            section.CoverageMm = JsonParsingHelpers.ReadNullableInt(coverageElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "foundationLevelM", out JsonElement foundationLevelElement))
        {
            section.FoundationLevelM = JsonParsingHelpers.ReadNullableDouble(foundationLevelElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "footings", out JsonElement footingsElement))
        {
            section.Footings = JsonParsingHelpers.ReadList(footingsElement, ParseFooting);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "pads", out JsonElement padsElement))
        {
            section.Pads = JsonParsingHelpers.ReadList(padsElement, ParsePad);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "foundationWall", out JsonElement foundationWallElement)
            && foundationWallElement.ValueKind == JsonValueKind.Object)
        {
            section.FoundationWall = JsonSerializer.Deserialize<FoundationWallDetail>(
                foundationWallElement.GetRawText(),
                options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "foundationWalls", out JsonElement foundationWallsElement)
            && foundationWallsElement.ValueKind == JsonValueKind.Object
            && section.FoundationWall is null)
        {
            section.FoundationWall = JsonSerializer.Deserialize<FoundationWallDetail>(
                foundationWallsElement.GetRawText(),
                options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "blocks", out JsonElement blocksElement))
        {
            section.Blocks = ReadMaterialList(blocksElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "concrete", out JsonElement concreteElement))
        {
            if (concreteElement.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(section.ConcreteClass))
            {
                section.ConcreteClass = JsonParsingHelpers.ReadString(concreteElement);
            }
            else
            {
                section.Concrete = ReadMaterialList(concreteElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "steel", out JsonElement steelElement))
        {
            if (steelElement.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(section.SteelSpecification))
            {
                section.SteelSpecification = JsonParsingHelpers.ReadString(steelElement);
            }
            else
            {
                section.Steel = ReadMaterialList(steelElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "insulation", out JsonElement insulationElement))
        {
            section.Insulation = ReadMaterialList(insulationElement);
        }

        return HasAnyData(section) ? section : null;
    }

    public override void Write(Utf8JsonWriter writer, FoundationSection? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonConverterWriteHelper.SerializeWithoutConverter<FoundationSection, FoundationSectionJsonConverter>(writer, value, options);
    }

    private static FootingDetail? ParseFooting(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        FootingDetail footing = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "id", out JsonElement idElement))
        {
            footing.Id = JsonParsingHelpers.ReadString(idElement, Guid.NewGuid().ToString());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "symbol", out JsonElement symbolElement))
        {
            footing.Symbol = JsonParsingHelpers.ReadString(symbolElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "lengthM", out JsonElement lengthElement))
        {
            footing.LengthM = JsonParsingHelpers.ReadDouble(lengthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "widthM", out JsonElement widthElement))
        {
            footing.WidthM = JsonParsingHelpers.ReadDouble(widthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "heightM", out JsonElement heightElement))
        {
            footing.HeightM = JsonParsingHelpers.ReadDouble(heightElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "concreteClass", out JsonElement concreteClassElement))
        {
            footing.ConcreteClass = JsonParsingHelpers.ReadString(concreteClassElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "reinforcement", out JsonElement reinforcementElement))
        {
            footing.Reinforcement = JsonParsingHelpers.ReadFlexibleString(reinforcementElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "segments", out JsonElement segmentsElement))
        {
            footing.Segments = JsonParsingHelpers.ReadList(segmentsElement, ParseFootingSegment);
        }

        if (footing.Segments.Count > 0)
        {
            footing.LengthM = footing.Segments.Sum(segment => segment.LengthM);
        }

        return footing;
    }

    private static FootingSegmentDetail? ParseFootingSegment(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        FootingSegmentDetail segment = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "id", out JsonElement idElement))
        {
            segment.Id = JsonParsingHelpers.ReadString(idElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "lengthM", out JsonElement lengthElement))
        {
            segment.LengthM = JsonParsingHelpers.ReadDouble(lengthElement);
        }

        return segment.LengthM > 0 ? segment : null;
    }

    private static PadDetail? ParsePad(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        PadDetail pad = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "symbol", out JsonElement symbolElement))
        {
            pad.Symbol = JsonParsingHelpers.ReadString(symbolElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "bM", out JsonElement bElement))
        {
            pad.BM = JsonParsingHelpers.ReadDouble(bElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "lM", out JsonElement lElement))
        {
            pad.LM = JsonParsingHelpers.ReadDouble(lElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "heightM", out JsonElement heightElement))
        {
            pad.HeightM = JsonParsingHelpers.ReadDouble(heightElement);
        }

        return pad;
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

    private static bool HasAnyData(FoundationSection section)
    {
        return section.Footings.Count > 0
            || section.Pads.Count > 0
            || section.FoundationWall is not null
            || !string.IsNullOrWhiteSpace(section.ConcreteClass)
            || !string.IsNullOrWhiteSpace(section.SteelSpecification)
            || section.Blocks.Count > 0
            || section.Concrete.Count > 0
            || section.Steel.Count > 0
            || section.Insulation.Count > 0;
    }
}
