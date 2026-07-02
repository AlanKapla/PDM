using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class ExtractionFocusRouter : IExtractionFocusRouter
{
    private static readonly HashSet<string> CrossValidationDrawingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "rzut parteru",
        "rzut piętra",
        "rzut pietra",
        "rzut poddasza",
        "rzut piwnicy",
        "rzut fundamentów",
        "rzut fundamentow"
    };

    public ExtractionFocusRoute Resolve(DrawingClassification classification)
    {
        string normalizedType = NormalizeDrawingType(classification.DrawingType);
        (string focusA, string focusB) = ExtractionFocusPromptLoader.GetPrompts(normalizedType);

        bool requiresCv = classification.HasMaterialTable
            || CrossValidationDrawingTypes.Contains(normalizedType);

        return new ExtractionFocusRoute
        {
            FocusA = focusA,
            FocusB = focusB,
            RequiresCrossValidation = requiresCv
        };
    }

    public static string NormalizeDrawingType(string drawingType)
    {
        if (string.IsNullOrWhiteSpace(drawingType))
        {
            return "nieznany";
        }

        string normalized = drawingType.Trim().ToLowerInvariant()
            .Replace('_', ' ');

        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalized;
    }
}
