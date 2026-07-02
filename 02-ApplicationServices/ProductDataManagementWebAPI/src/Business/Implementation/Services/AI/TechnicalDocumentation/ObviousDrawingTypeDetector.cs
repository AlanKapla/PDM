using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ObviousDrawingTypeDetector
{
    public static DrawingClassification? TryDetect(string? fileName, string? hintText = null)
    {
        string combined = Normalize($"{fileName} {hintText}");
        if (string.IsNullOrWhiteSpace(combined))
        {
            return null;
        }

        string? sheetNumber = DrawingSheetNumberInferrer.InferFromFileName(fileName);
        string normalizedSheet = sheetNumber?.ToLowerInvariant() ?? string.Empty;

        if (normalizedSheet.StartsWith("a-01", StringComparison.Ordinal)
            || ContainsAny(
                combined,
                "zagospodarowanie terenu",
                "zagospodarowanie_terenu",
                "zagospodarowanie dzialki",
                "zagospodarowanie dzialka",
                "dzialk"))
        {
            return BuildClassification("zagospodarowanie_terenu", hasMaterialTable: false, "Zagospodarowanie terenu", sheetNumber);
        }

        if (normalizedSheet.StartsWith("k-05", StringComparison.Ordinal)
            || ContainsAny(combined, "detale konstrukcyjne", "detale_konstrukcyjne"))
        {
            return BuildClassification("detale_konstrukcyjne", hasMaterialTable: false, "Detale konstrukcyjne", sheetNumber);
        }

        if (ContainsAny(combined, "lista pretow", "lista prętów", "lista pretów"))
        {
            return BuildSteelClassification(combined, fileName);
        }

        if (ContainsAny(combined, "lista drewna")
            || normalizedSheet.StartsWith("k-04", StringComparison.Ordinal))
        {
            return BuildClassification("rzut_wiezby_dachowej", hasMaterialTable: true, "Lista drewna", sheetNumber);
        }

        if (ContainsAny(combined, "zestawienie pomieszczen", "zestawienie pomieszczeń"))
        {
            return BuildFloorPlanClassification(combined);
        }

        if (ContainsAny(combined, "rzut fundamentow", "rzut fundamentów", "fundamentow", "fundamentów"))
        {
            return BuildClassification("rzut_fundamentow", hasMaterialTable: false, "Rzut fundamentów");
        }

        return null;
    }

    private static DrawingClassification BuildSteelClassification(string combined, string? fileName)
    {
        bool isUpper = ContainsAny(combined, "gorn", "gór", "gorne", "górne", "k-03", "k03");
        string drawingType = isUpper ? "zbrojenie_stropu_gorne" : "zbrojenie_stropu_dolne";
        string title = isUpper ? "Zbrojenie górne stropu" : "Zbrojenie dolne stropu";
        string? sheetNumber = DrawingSheetNumberInferrer.InferFromFileName(fileName);

        return BuildClassification(drawingType, hasMaterialTable: true, title, sheetNumber);
    }

    private static DrawingClassification BuildFloorPlanClassification(string combined)
    {
        if (ContainsAny(combined, "poddasz"))
        {
            return BuildClassification("rzut_poddasza", hasMaterialTable: true, "Rzut poddasza");
        }

        if (ContainsAny(combined, "piwnic"))
        {
            return BuildClassification("rzut_piwnicy", hasMaterialTable: true, "Rzut piwnicy");
        }

        if (ContainsAny(combined, "pietr", "piętr", "pietra", "piętra")
            && !ContainsAny(combined, "parter"))
        {
            return BuildClassification("rzut_piętra", hasMaterialTable: true, "Rzut piętra");
        }

        return BuildClassification("rzut_parteru", hasMaterialTable: true, "Rzut parteru");
    }

    private static DrawingClassification BuildClassification(
        string drawingType,
        bool hasMaterialTable,
        string title,
        string? sheetNumber = null)
    {
        return new DrawingClassification
        {
            DrawingType = drawingType,
            Title = title,
            SheetNumber = sheetNumber,
            HasMaterialTable = hasMaterialTable,
            TableTitle = hasMaterialTable ? title : null
        };
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant()
            .Replace('_', ' ')
            .Replace('ł', 'l')
            .Replace('ó', 'o')
            .Replace('ą', 'a')
            .Replace('ę', 'e')
            .Replace('ś', 's')
            .Replace('ć', 'c')
            .Replace('ń', 'n')
            .Replace('ź', 'z')
            .Replace('ż', 'z');
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        foreach (string token in tokens)
        {
            if (text.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
