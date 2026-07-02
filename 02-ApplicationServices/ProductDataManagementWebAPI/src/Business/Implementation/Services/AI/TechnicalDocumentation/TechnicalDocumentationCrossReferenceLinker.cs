using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class TechnicalDocumentationCrossReferenceLinker
{
    public static List<DrawingDependencyLink> LinkDrawings(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<DrawingDependencyLink> dependencies = new();

        foreach (FloorPlanDrawing drawing in drawings)
        {
            foreach (DrawingCrossReference reference in drawing.CrossReferences)
            {
                AddCrossReferenceDependency(drawings, drawing, reference, dependencies);
            }

            foreach (RelatedDrawingRef related in drawing.Classification.RelatedDrawings)
            {
                AddRelatedDrawingDependency(drawing, related, dependencies);
            }

            foreach (DeferredDetailNote deferred in drawing.DeferredDetails)
            {
                FloorPlanDrawing? target = ResolveDeferredTarget(drawings, deferred);

                dependencies.Add(new DrawingDependencyLink
                {
                    SourceFileName = drawing.Source.FileName,
                    SourcePageNumber = drawing.Source.PageNumber,
                    SourceSheetNumber = drawing.Classification.SheetNumber,
                    ReferenceLabel = deferred.TargetReference,
                    DetailType = deferred.Topic,
                    TargetFileName = target?.Source.FileName,
                    TargetPageNumber = target?.Source.PageNumber,
                    TargetSheetNumber = target?.Classification.SheetNumber,
                    TargetTitle = target?.Classification.Title,
                    Notes = deferred.Notes
                });
            }
        }

        return dependencies;
    }

    private static void AddCrossReferenceDependency(
        IReadOnlyList<FloorPlanDrawing> drawings,
        FloorPlanDrawing drawing,
        DrawingCrossReference reference,
        List<DrawingDependencyLink> dependencies)
    {
        FloorPlanDrawing? target = ResolveTarget(drawings, reference);

        if (target is not null)
        {
            reference.ResolvedFileName = target.Source.FileName;
            reference.ResolvedPageNumber = target.Source.PageNumber;
        }

        dependencies.Add(new DrawingDependencyLink
        {
            SourceFileName = drawing.Source.FileName,
            SourcePageNumber = drawing.Source.PageNumber,
            SourceSheetNumber = drawing.Classification.SheetNumber,
            ReferenceLabel = reference.ReferenceLabel,
            DetailType = reference.DetailType,
            TargetFileName = reference.ResolvedFileName,
            TargetPageNumber = reference.ResolvedPageNumber,
            TargetSheetNumber = reference.TargetSheetNumber ?? target?.Classification.SheetNumber,
            TargetTitle = reference.TargetTitle ?? target?.Classification.Title,
            Notes = reference.Notes
        });
    }

    private static void AddRelatedDrawingDependency(
        FloorPlanDrawing drawing,
        RelatedDrawingRef related,
        List<DrawingDependencyLink> dependencies)
    {
        if (string.IsNullOrWhiteSpace(related.TargetSheetNumber)
            && string.IsNullOrWhiteSpace(related.ReferenceLabel)
            && string.IsNullOrWhiteSpace(related.TargetTitle))
        {
            return;
        }

        dependencies.Add(new DrawingDependencyLink
        {
            SourceFileName = drawing.Source.FileName,
            SourcePageNumber = drawing.Source.PageNumber,
            SourceSheetNumber = drawing.Classification.SheetNumber,
            ReferenceLabel = related.ReferenceLabel,
            DetailType = related.DetailType,
            TargetSheetNumber = related.TargetSheetNumber,
            TargetTitle = related.TargetTitle
        });
    }

    private static FloorPlanDrawing? ResolveTarget(
        IReadOnlyList<FloorPlanDrawing> drawings,
        DrawingCrossReference reference)
    {
        return FindDrawing(
            drawings,
            reference.TargetSheetNumber,
            reference.TargetTitle,
            reference.ReferenceLabel);
    }

    private static FloorPlanDrawing? ResolveDeferredTarget(
        IReadOnlyList<FloorPlanDrawing> drawings,
        DeferredDetailNote deferred)
    {
        return FindDrawing(
            drawings,
            ExtractSheetNumber(deferred.TargetReference),
            null,
            deferred.TargetReference);
    }

    private static FloorPlanDrawing? FindDrawing(
        IReadOnlyList<FloorPlanDrawing> drawings,
        string? sheetNumber,
        string? title,
        string? referenceLabel)
    {
        if (!string.IsNullOrWhiteSpace(sheetNumber))
        {
            FloorPlanDrawing? bySheet = drawings.FirstOrDefault(drawing =>
                string.Equals(
                    NormalizeSheet(drawing.Classification.SheetNumber),
                    NormalizeSheet(sheetNumber),
                    StringComparison.OrdinalIgnoreCase));

            if (bySheet is not null)
            {
                return bySheet;
            }
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            FloorPlanDrawing? byTitle = drawings.FirstOrDefault(drawing =>
                drawing.Classification.Title is not null
                && drawing.Classification.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            if (byTitle is not null)
            {
                return byTitle;
            }
        }

        if (!string.IsNullOrWhiteSpace(referenceLabel))
        {
            string normalizedReference = referenceLabel.Trim().ToLowerInvariant();

            return drawings.FirstOrDefault(drawing =>
            {
                string drawingTitle = drawing.Classification.Title?.ToLowerInvariant() ?? string.Empty;
                string drawingType = drawing.Classification.DrawingType.ToLowerInvariant();

                return drawingTitle.Contains(normalizedReference, StringComparison.Ordinal)
                    || normalizedReference.Contains(drawingType, StringComparison.Ordinal)
                    || drawing.Classification.RelatedDrawings.Any(related =>
                        string.Equals(
                            related.ReferenceLabel,
                            referenceLabel,
                            StringComparison.OrdinalIgnoreCase));
            });
        }

        return null;
    }

    private static string? ExtractSheetNumber(string targetReference)
    {
        string normalized = targetReference.ToLowerInvariant();

        if (normalized.Contains("arkusz"))
        {
            string[] parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < parts.Length - 1; index++)
            {
                if (parts[index] == "arkusz" || parts[index] == "ark.")
                {
                    return parts[index + 1].Trim('.', ',', ';');
                }
            }
        }

        if (normalized.Contains("ark."))
        {
            int start = normalized.IndexOf("ark.", StringComparison.Ordinal) + 4;
            if (start < normalized.Length)
            {
                return normalized[start..].Split(' ', '/', ',')[0];
            }
        }

        return null;
    }

    private static string NormalizeSheet(string? sheetNumber)
    {
        return sheetNumber?.Trim().TrimStart('0') ?? string.Empty;
    }
}
