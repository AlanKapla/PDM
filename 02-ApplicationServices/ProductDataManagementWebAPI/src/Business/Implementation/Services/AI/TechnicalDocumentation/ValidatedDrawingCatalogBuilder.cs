using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ValidatedDrawingCatalogBuilder
{
    public static List<ValidatedDrawingEntry> Build(
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        IReadOnlyList<string> failedPages)
    {
        HashSet<string> failedKeys = failedPages
            .Select(ParseFailedPageKey)
            .Where(key => key.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, ValidatedDrawingEntry> entries = new(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlanDrawing drawing in drawings)
        {
            string key = BuildImageKey(drawing.Source.FileName, drawing.Source.PageNumber);
            entries[key] = new ValidatedDrawingEntry
            {
                SheetNumber = drawing.Classification.SheetNumber
                    ?? DrawingSheetNumberInferrer.InferFromFileName(drawing.Source.FileName),
                DrawingType = drawing.Classification.DrawingType ?? "nieznany",
                Title = drawing.Classification.Title,
                Scale = drawing.Classification.Scale,
                Validated = !failedKeys.Contains(key),
                HasMaterialTable = drawing.Classification.HasMaterialTable
            };
        }

        foreach (TechnicalDocumentationImageInput image in images)
        {
            string key = BuildImageKey(image.FileName, image.PageNumber);
            if (entries.ContainsKey(key))
            {
                continue;
            }

            entries[key] = new ValidatedDrawingEntry
            {
                SheetNumber = DrawingSheetNumberInferrer.InferFromFileName(image.FileName),
                DrawingType = "nieprzetworzony",
                Title = image.FileName,
                Validated = false,
                HasMaterialTable = false
            };
        }

        return entries.Values
            .OrderBy(entry => entry.SheetNumber ?? entry.Title ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildImageKey(string fileName, int pageNumber)
    {
        return $"{fileName.Trim().ToLowerInvariant()}|{pageNumber}";
    }

    private static string ParseFailedPageKey(string failedPage)
    {
        int pageStart = failedPage.LastIndexOf("(str.", StringComparison.OrdinalIgnoreCase);
        if (pageStart < 0)
        {
            return string.Empty;
        }

        string filePart = failedPage[..pageStart].Trim();
        string pagePart = failedPage[(pageStart + 5)..].Trim().TrimEnd(')');
        if (!int.TryParse(pagePart, out int pageNumber))
        {
            return string.Empty;
        }

        return BuildImageKey(filePart, pageNumber);
    }
}
