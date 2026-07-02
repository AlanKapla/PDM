using System.Globalization;
using System.Text.RegularExpressions;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static partial class DrawingFloorNotesParser
{
    public static string? TryParseAreaNotes(string? tableContent)
    {
        if (string.IsNullOrWhiteSpace(tableContent))
        {
            return null;
        }

        Match match = AreaNotesRegex().Match(tableContent);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups[1].Value.Trim();
    }

    [GeneratedRegex(
        @"(Powierzchnia\s+użytkowa[^:;]*:\s*[\d.,]+\s*m2[^;]*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AreaNotesRegex();
}
