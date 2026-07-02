using System.Text.RegularExpressions;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static partial class DrawingSheetNumberInferrer
{
    public static string? InferFromFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        string name = Path.GetFileNameWithoutExtension(fileName.Trim());
        Match match = SheetNumberRegex().Match(name);
        if (!match.Success)
        {
            return null;
        }

        string letter = match.Groups[1].Value.ToUpperInvariant();
        string number = match.Groups[2].Value.PadLeft(2, '0');
        return $"{letter}-{number}";
    }

    [GeneratedRegex(@"(?:^|[^a-z0-9])([a-z])[\s_-]?0*(\d{1,2})(?:[^a-z0-9]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex SheetNumberRegex();
}
