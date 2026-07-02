using System.Globalization;
using System.Text.RegularExpressions;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static partial class DrawingTableMassParser
{
    public static double? TryParseTotalMassKg(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        Match match = TotalMassRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }

        string valueText = match.Groups[1].Value.Replace(',', '.');
        if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out double massKg))
        {
            return null;
        }

        return Math.Round(massKg, 2);
    }

    public static double? TryParseTimberTotalVolumeM3(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        MatchCollection matches = TimberVolumeRegex().Matches(text);
        if (matches.Count == 0)
        {
            return null;
        }

        double total = 0;
        foreach (Match match in matches)
        {
            string valueText = match.Groups[1].Value.Replace(',', '.');
            if (double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out double volume))
            {
                total += volume;
            }
        }

        return total > 0 ? Math.Round(total, 3) : null;
    }

    [GeneratedRegex(@"masa\s+całkowita\s*:\s*([\d.,]+)\s*kg", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TotalMassRegex();

    [GeneratedRegex(@"([\d.,]+)\s*m3", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TimberVolumeRegex();
}
