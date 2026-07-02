using System.Text;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class TechnicalDocumentationDrawingCatalog
{
    public static IReadOnlyList<DrawingCatalogEntry> Build(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        IReadOnlyList<DrawingClassification> classifications)
    {
        List<DrawingCatalogEntry> entries = new();

        for (int index = 0; index < images.Count; index++)
        {
            TechnicalDocumentationImageInput image = images[index];
            DrawingClassification classification = classifications[index];

            entries.Add(new DrawingCatalogEntry
            {
                FileName = image.FileName,
                PageNumber = image.PageNumber,
                Classification = classification
            });
        }

        return entries;
    }

    public static TechnicalDocumentationExtractionContext BuildExtractionContext(
        TechnicalDocumentationImageInput image,
        DrawingClassification classification,
        IReadOnlyList<DrawingCatalogEntry> catalog)
    {
        return new TechnicalDocumentationExtractionContext
        {
            Catalog = catalog,
            RelatedDrawings = classification.RelatedDrawings
        };
    }

    public static string BuildCatalogUserText(TechnicalDocumentationExtractionContext? context)
    {
        if (context is null || context.Catalog.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        builder.AppendLine("KATALOG RYSUNKÓW W DOKUMENCIE:");

        for (int index = 0; index < context.Catalog.Count; index++)
        {
            DrawingCatalogEntry entry = context.Catalog[index];
            DrawingClassification classification = entry.Classification;

            builder.Append('[');
            builder.Append(index + 1);
            builder.Append("] ");
            builder.Append(entry.FileName);
            builder.Append(" s.");
            builder.Append(entry.PageNumber);

            if (!string.IsNullOrWhiteSpace(classification.SheetNumber))
            {
                builder.Append(" | ark.");
                builder.Append(classification.SheetNumber);
            }

            if (!string.IsNullOrWhiteSpace(classification.DrawingType))
            {
                builder.Append(" | ");
                builder.Append(classification.DrawingType);
            }

            if (!string.IsNullOrWhiteSpace(classification.Title))
            {
                builder.Append(" | ");
                builder.Append(classification.Title);
            }

            if (classification.Scale.HasValue)
            {
                builder.Append(" | skala 1:");
                builder.Append(classification.Scale.Value);
            }

            builder.AppendLine();

            AppendTextBlock(builder, "opis", classification.DescriptiveText);
            AppendTextBlock(builder, "tabela", classification.DrawingTable);
        }

        if (context.RelatedDrawings.Count > 0)
        {
            builder.AppendLine("POWIĄZANIA TEGO RYSUNKU (odczytaj i użyj katalogu):");

            foreach (RelatedDrawingRef reference in context.RelatedDrawings)
            {
                builder.Append("- ");
                builder.Append(reference.ReferenceLabel);

                if (!string.IsNullOrWhiteSpace(reference.TargetSheetNumber))
                {
                    builder.Append(" → ark.");
                    builder.Append(reference.TargetSheetNumber);
                }

                if (!string.IsNullOrWhiteSpace(reference.TargetTitle))
                {
                    builder.Append(" (");
                    builder.Append(reference.TargetTitle);
                    builder.Append(')');
                }

                if (!string.IsNullOrWhiteSpace(reference.DetailType))
                {
                    builder.Append(" [");
                    builder.Append(reference.DetailType);
                    builder.Append(']');
                }

                builder.AppendLine();
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendTextBlock(StringBuilder builder, string label, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        builder.Append("    ");
        builder.Append(label);
        builder.Append(": ");
        builder.AppendLine(text.Trim());
    }
}
