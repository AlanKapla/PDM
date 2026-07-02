using System.Text.RegularExpressions;

namespace Business.Implementation.Helpers;

internal static partial class AiGeneratedJsonSanitizer
{
    public static string Sanitize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        string result = json;

        result = DecimalWithoutLeadingZeroRegex().Replace(result, "$1 0.$2");
        result = NegativeDecimalWithoutLeadingZeroRegex().Replace(result, "$1 -0.$2");
        result = EllipsisValueRegex().Replace(result, "$1 null");
        result = LoneDotValueRegex().Replace(result, "$1 null$2");
        result = InfinityRegex().Replace(result, "null");
        result = NanRegex().Replace(result, "null");
        result = UndefinedRegex().Replace(result, "null");

        return result;
    }

    // : .5  , .5  [ .5  -> add leading zero (invalid in strict JSON)
    [GeneratedRegex(@"([:,\[])\s*\.(\d)", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalWithoutLeadingZeroRegex();

    // : -.5 -> -0.5
    [GeneratedRegex(@"([:,\[])\s*-\.(\d)", RegexOptions.CultureInvariant)]
    private static partial Regex NegativeDecimalWithoutLeadingZeroRegex();

    // : ... -> null
    [GeneratedRegex(@"([:,\[])\s*\.\.\.", RegexOptions.CultureInvariant)]
    private static partial Regex EllipsisValueRegex();

    // : .  or , .  when not followed by a digit
    [GeneratedRegex(@"([:,\[])\s*\.(\s*[,}\]])", RegexOptions.CultureInvariant)]
    private static partial Regex LoneDotValueRegex();

    [GeneratedRegex(@"\b-?Infinity\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InfinityRegex();

    [GeneratedRegex(@"\bNaN\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NanRegex();

    [GeneratedRegex(@"\bundefined\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UndefinedRegex();
}
