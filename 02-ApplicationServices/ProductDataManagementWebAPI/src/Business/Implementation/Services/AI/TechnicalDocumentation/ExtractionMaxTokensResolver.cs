namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ExtractionMaxTokensResolver
{
    private const int DefaultMaxTokens = 4096;
    private const int RoofTimberTableMaxTokens = 12000;
    private const int SlabReinforcementMaxTokens = 6000;

    public static int Resolve(string? drawingType)
    {
        string normalized = ExtractionFocusRouter.NormalizeDrawingType(drawingType ?? string.Empty);

        if (normalized.Contains("rzut wiezby dachowej", StringComparison.Ordinal)
            || normalized.Contains("material calculation", StringComparison.Ordinal)
            || normalized.Contains("material_calculation", StringComparison.Ordinal))
        {
            return RoofTimberTableMaxTokens;
        }

        if (normalized.Contains("zbrojenie stropu dolne", StringComparison.Ordinal)
            || normalized.Contains("zbrojenie stropu gorne", StringComparison.Ordinal))
        {
            return SlabReinforcementMaxTokens;
        }

        return DefaultMaxTokens;
    }
}
