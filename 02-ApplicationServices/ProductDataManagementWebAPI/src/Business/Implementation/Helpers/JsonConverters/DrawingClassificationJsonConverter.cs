using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Helpers.JsonConverters;

internal static class ScaleParsingHelper
{
    public static int? ReadScale(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out int value) => value,
            JsonValueKind.Number => (int)element.GetDouble(),
            JsonValueKind.String => ParseScaleString(element.GetString()),
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    public static int? ParseScaleString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int direct))
        {
            return direct;
        }

        int colonIndex = trimmed.IndexOf(':');
        if (colonIndex >= 0)
        {
            string denominator = trimmed[(colonIndex + 1)..].Trim();
            if (int.TryParse(denominator, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }
        }

        int slashIndex = trimmed.IndexOf('/');
        if (slashIndex >= 0)
        {
            string denominator = trimmed[(slashIndex + 1)..].Trim();
            if (int.TryParse(denominator, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

public sealed class DrawingClassificationJsonConverter : JsonConverter<DrawingClassification>
{
    public override DrawingClassification Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new DrawingClassification();
        }

        JsonElement root;
        try
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return new DrawingClassification();
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return new DrawingClassification();
        }

        DrawingClassification classification = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "drawingType", out JsonElement drawingTypeElement))
        {
            classification.DrawingType = JsonParsingHelpers.ReadString(drawingTypeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "scale", out JsonElement scaleElement))
        {
            classification.Scale = ScaleParsingHelper.ReadScale(scaleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "sheetNumber", out JsonElement sheetNumberElement))
        {
            classification.SheetNumber = JsonParsingHelpers.ReadString(sheetNumberElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "title", out JsonElement titleElement))
        {
            classification.Title = JsonParsingHelpers.ReadString(titleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "author", out JsonElement authorElement))
        {
            classification.Author = JsonParsingHelpers.ReadString(authorElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "date", out JsonElement dateElement))
        {
            classification.Date = JsonParsingHelpers.ReadString(dateElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "investor", out JsonElement investorElement))
        {
            classification.Investor = JsonParsingHelpers.ReadString(investorElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "address", out JsonElement addressElement))
        {
            classification.Address = JsonParsingHelpers.ReadString(addressElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "location", out JsonElement locationElement))
        {
            classification.Location = JsonParsingHelpers.ReadString(locationElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "collaborator", out JsonElement collaboratorElement))
        {
            classification.Collaborator = JsonParsingHelpers.ReadString(collaboratorElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "phase", out JsonElement phaseElement))
        {
            classification.Phase = JsonParsingHelpers.ReadString(phaseElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "projectName", out JsonElement projectNameElement))
        {
            classification.ProjectName = JsonParsingHelpers.ReadString(projectNameElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "buildingType", out JsonElement buildingTypeElement))
        {
            classification.BuildingType = JsonParsingHelpers.ReadString(buildingTypeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "revision", out JsonElement revisionElement))
        {
            classification.Revision = JsonParsingHelpers.ReadString(revisionElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "descriptiveText", out JsonElement descriptiveElement))
        {
            classification.DescriptiveText = JsonParsingHelpers.ReadString(descriptiveElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "drawingTable", out JsonElement tableElement))
        {
            classification.DrawingTable = JsonParsingHelpers.ReadString(tableElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "tableContent", out JsonElement tableContentElement))
        {
            classification.TableContent = JsonParsingHelpers.ReadString(tableContentElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "elementAnnotations", out JsonElement annotationsElement))
        {
            classification.ElementAnnotations = JsonParsingHelpers.ReadString(annotationsElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "technicalParameters", out JsonElement parametersElement))
        {
            string? parameters = JsonParsingHelpers.ReadTechnicalParameters(parametersElement);
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                classification.TechnicalParameters = parameters;
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "legend", out JsonElement legendElement))
        {
            classification.Legend = JsonParsingHelpers.ReadString(legendElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "notes", out JsonElement notesElement))
        {
            classification.Notes = JsonParsingHelpers.ReadString(notesElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "floorLevel", out JsonElement floorLevelElement))
        {
            classification.FloorLevel = JsonParsingHelpers.ReadString(floorLevelElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "floorOrder", out JsonElement floorOrderElement)
            && floorOrderElement.ValueKind != JsonValueKind.Null)
        {
            classification.FloorOrder = JsonParsingHelpers.ReadInt(floorOrderElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "hasMaterialTable", out JsonElement hasMaterialTableElement))
        {
            classification.HasMaterialTable = JsonParsingHelpers.ReadBool(hasMaterialTableElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "tableTitle", out JsonElement tableTitleElement))
        {
            classification.TableTitle = JsonParsingHelpers.ReadString(tableTitleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "relatedDrawings", out JsonElement relatedElement))
        {
            classification.RelatedDrawings = JsonParsingHelpers.ReadList(relatedElement, ParseRelatedDrawing);
        }

        return classification;
    }

    private static RelatedDrawingRef ParseRelatedDrawing(JsonElement element)
    {
        RelatedDrawingRef reference = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "referenceLabel", out JsonElement labelElement)
            || JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "label", out labelElement))
        {
            reference.ReferenceLabel = JsonParsingHelpers.ReadString(labelElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "targetSheetNumber", out JsonElement sheetElement)
            || JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "sheetNumber", out sheetElement))
        {
            reference.TargetSheetNumber = JsonParsingHelpers.ReadString(sheetElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "targetTitle", out JsonElement titleElement))
        {
            reference.TargetTitle = JsonParsingHelpers.ReadString(titleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "detailType", out JsonElement detailElement))
        {
            reference.DetailType = JsonParsingHelpers.ReadString(detailElement);
        }

        return reference;
    }

    public override void Write(Utf8JsonWriter writer, DrawingClassification value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<DrawingClassification, DrawingClassificationJsonConverter>(writer, value, options);
    }
}
