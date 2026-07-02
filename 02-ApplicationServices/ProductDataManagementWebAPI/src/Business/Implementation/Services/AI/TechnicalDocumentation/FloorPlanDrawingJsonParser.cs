using System.Text;
using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class FloorPlanDrawingJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();
    private static readonly JsonSerializerOptions CompactJsonOptions = TechnicalDocumentationJsonHelper.CreateCompactSerializerOptions();

    public static FloorPlanDrawing Parse(
        string response,
        DrawingClassification classification,
        ILogger? logger = null)
    {
        FloorPlanDrawing drawing = TechnicalDocumentationJsonHelper.DeserializeAgentResponse(
            response,
            JsonOptions,
            new FloorPlanDrawing(),
            logger,
            "FloorPlanDrawing");

        drawing.Classification = classification;
        MergeTextSourcesFromClassification(drawing, classification);
        DrawingExtractionNormalizer.Normalize(drawing, classification);
        return MaterialUnitNormalizer.NormalizeDrawing(drawing);
    }

    public static string SerializeDrawing(FloorPlanDrawing drawing)
    {
        return JsonSerializer.Serialize(drawing, CompactJsonOptions);
    }

    public static string BuildExtractionUserText(
        DrawingClassification classification,
        TechnicalDocumentationExtractionContext? extractionContext = null,
        string? focusPrompt = null,
        bool includeFullTextSources = false)
    {
        StringBuilder builder = new();
        builder.AppendLine(TechnicalDocumentationDomainRules.ExtractionUserPrefix.TrimEnd());
        builder.AppendLine(BuildClassificationContext(classification));

        if (includeFullTextSources)
        {
            AppendTextSourcesBlock(builder, classification);
        }
        else if (classification.HasMaterialTable)
        {
            AppendMaterialTableOnly(builder, classification);
        }

        if (!string.IsNullOrWhiteSpace(focusPrompt))
        {
            builder.AppendLine();
            builder.AppendLine("=== FOCUS EKSTRAKCJI ===");
            builder.AppendLine(focusPrompt.Trim());
        }

        string catalogText = TechnicalDocumentationDrawingCatalog.BuildCatalogUserText(extractionContext);
        if (!string.IsNullOrWhiteSpace(catalogText))
        {
            builder.AppendLine();
            builder.Append(catalogText);
        }

        return builder.ToString().TrimEnd();
    }

    public static string BuildClassificationContext(DrawingClassification classification)
    {
        StringBuilder builder = new();
        builder.Append("typ:");
        builder.Append(classification.DrawingType);

        if (classification.Scale.HasValue)
        {
            builder.Append(";skala:1:");
            builder.Append(classification.Scale.Value);
        }

        AppendContextField(builder, "arkusz", classification.SheetNumber);
        AppendContextField(builder, "tytul", classification.Title);
        AppendContextField(builder, "autor", classification.Author);
        AppendContextField(builder, "data", classification.Date);
        AppendContextField(builder, "inwestor", classification.Investor);
        AppendContextField(builder, "lokalizacja", classification.Location);
        AppendContextField(builder, "typBudynku", classification.BuildingType);
        AppendContextField(builder, "rewizja", classification.Revision);
        AppendContextField(builder, "kondygnacja", classification.FloorLevel);

        if (classification.FloorOrder.HasValue)
        {
            builder.Append(";floorOrder:");
            builder.Append(classification.FloorOrder.Value);
        }

        AppendContextField(builder, "tabele", ResolveTableContent(classification));
        AppendContextField(builder, "opis", classification.DescriptiveText);
        AppendContextField(builder, "parametry", classification.TechnicalParameters);
        AppendContextField(builder, "etykiety", classification.ElementAnnotations);
        AppendContextField(builder, "legenda", classification.Legend);
        AppendContextField(builder, "uwagi", classification.Notes);

        if (classification.HasMaterialTable)
        {
            builder.Append(";hasMaterialTable:true");
        }

        return builder.ToString();
    }

    private static void AppendMaterialTableOnly(StringBuilder builder, DrawingClassification classification)
    {
        string? tableContent = ResolveTableContent(classification);
        if (string.IsNullOrWhiteSpace(tableContent))
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("=== TABELE MATERIAŁOWE (kontekst tekstowy) ===");
        AppendSourceSection(builder, "TABELE MATERIAŁOWE", tableContent);
    }

    private static void AppendTextSourcesBlock(StringBuilder builder, DrawingClassification classification)
    {
        string? tableContent = ResolveTableContent(classification);
        bool hasContent = !string.IsNullOrWhiteSpace(tableContent)
            || !string.IsNullOrWhiteSpace(classification.DescriptiveText)
            || !string.IsNullOrWhiteSpace(classification.TechnicalParameters)
            || !string.IsNullOrWhiteSpace(classification.ElementAnnotations)
            || !string.IsNullOrWhiteSpace(classification.Legend)
            || !string.IsNullOrWhiteSpace(classification.Notes);

        if (!hasContent)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("=== 6 ŹRÓDEŁ TEKSTU Z KLASYFIKACJI (OBOWIĄZKOWE) ===");

        AppendSourceSection(builder, "TABLICZKA", BuildTitleBlock(classification));
        AppendSourceSection(builder, "TABELE MATERIAŁOWE", tableContent);
        AppendSourceSection(builder, "BLOKI OPISOWE", classification.DescriptiveText);
        AppendSourceSection(builder, "PARAMETRY TECHNICZNE", classification.TechnicalParameters);
        AppendSourceSection(builder, "ETYKIETY ELEMENTÓW", classification.ElementAnnotations);
        AppendSourceSection(builder, "LEGENDA", classification.Legend);
        AppendSourceSection(builder, "UWAGI", classification.Notes);

        builder.AppendLine("Dla każdego symbolu z tabeli (Z1, O1, D1, ...) utwórz odpowiedni wpis w walls[] lub openings[].");
    }

    private static string? ResolveTableContent(DrawingClassification classification)
    {
        if (!string.IsNullOrWhiteSpace(classification.TableContent))
        {
            return classification.TableContent.Trim();
        }

        if (!string.IsNullOrWhiteSpace(classification.DrawingTable))
        {
            return classification.DrawingTable.Trim();
        }

        return null;
    }

    private static string BuildTitleBlock(DrawingClassification classification)
    {
        StringBuilder block = new();

        if (!string.IsNullOrWhiteSpace(classification.SheetNumber))
        {
            block.Append("arkusz:");
            block.Append(classification.SheetNumber.Trim());
        }

        if (!string.IsNullOrWhiteSpace(classification.Title))
        {
            if (block.Length > 0)
            {
                block.Append("; ");
            }

            block.Append("tytuł:");
            block.Append(classification.Title.Trim());
        }

        if (!string.IsNullOrWhiteSpace(classification.Author))
        {
            AppendTitleBlockField(block, "autor", classification.Author);
        }

        if (!string.IsNullOrWhiteSpace(classification.Date))
        {
            AppendTitleBlockField(block, "data", classification.Date);
        }

        if (!string.IsNullOrWhiteSpace(classification.Investor))
        {
            AppendTitleBlockField(block, "inwestor", classification.Investor);
        }

        if (!string.IsNullOrWhiteSpace(classification.Location))
        {
            AppendTitleBlockField(block, "lokalizacja", classification.Location);
        }

        if (!string.IsNullOrWhiteSpace(classification.BuildingType))
        {
            AppendTitleBlockField(block, "typ budynku", classification.BuildingType);
        }

        if (classification.Scale.HasValue)
        {
            if (block.Length > 0)
            {
                block.Append("; ");
            }

            block.Append("skala:1:");
            block.Append(classification.Scale.Value);
        }

        return block.ToString();
    }

    private static void AppendTitleBlockField(StringBuilder block, string label, string value)
    {
        if (block.Length > 0)
        {
            block.Append("; ");
        }

        block.Append(label);
        block.Append(':');
        block.Append(value.Trim());
    }

    private static void AppendSourceSection(StringBuilder builder, string label, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        builder.Append('[');
        builder.Append(label);
        builder.AppendLine("]");
        builder.AppendLine(content.Trim());
    }

    private static void AppendContextField(StringBuilder builder, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(';');
        builder.Append(key);
        builder.Append(':');
        builder.Append(value.Trim());
    }

    private static void MergeTextSourcesFromClassification(
        FloorPlanDrawing drawing,
        DrawingClassification classification)
    {
        drawing.TextSources ??= new DrawingTextSources();

        if (string.IsNullOrWhiteSpace(drawing.TextSources.DescriptiveText)
            && !string.IsNullOrWhiteSpace(classification.DescriptiveText))
        {
            drawing.TextSources.DescriptiveText = classification.DescriptiveText;
        }

        string? tableContent = ResolveTableContent(classification);
        if (string.IsNullOrWhiteSpace(drawing.TextSources.DrawingTable)
            && !string.IsNullOrWhiteSpace(tableContent))
        {
            drawing.TextSources.DrawingTable = tableContent;
        }
    }
}
