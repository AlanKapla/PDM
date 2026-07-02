using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Business.AIAgent.Core;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ExtractionFocusPromptLoader
{
    private static readonly Lazy<Dictionary<string, (string FocusA, string FocusB)>> PromptsCache =
        new(LoadPrompts);

    private static readonly Dictionary<string, string> DrawingTypeAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["rzut fundamentów"] = "rzut fundamentow",
            ["przekrój"] = "przekroj",
            ["rzut więźby dachowej"] = "rzut wiezby dachowej",
            ["rzut pietra"] = "rzut piętra"
        };

    private static readonly Regex FocusHeaderRegex = new(
        @"^##\s+FOCUS(?:_B)?:\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static (string FocusA, string FocusB) GetPrompts(string normalizedDrawingType)
    {
        Dictionary<string, (string FocusA, string FocusB)> prompts = PromptsCache.Value;
        string lookupKey = ResolveLookupKey(normalizedDrawingType, prompts);

        if (prompts.TryGetValue(lookupKey, out (string FocusA, string FocusB) found))
        {
            string focusB = string.IsNullOrWhiteSpace(found.FocusB) ? found.FocusA : found.FocusB;
            return (found.FocusA, focusB);
        }

        (string FocusA, string FocusB) fallback = prompts["default"];
        string fallbackB = string.IsNullOrWhiteSpace(fallback.FocusB) ? fallback.FocusA : fallback.FocusB;
        return (fallback.FocusA, fallbackB);
    }

    private static string ResolveLookupKey(
        string normalizedDrawingType,
        Dictionary<string, (string FocusA, string FocusB)> prompts)
    {
        if (prompts.ContainsKey(normalizedDrawingType))
        {
            return normalizedDrawingType;
        }

        if (DrawingTypeAliases.TryGetValue(normalizedDrawingType, out string? alias)
            && prompts.ContainsKey(alias))
        {
            return alias;
        }

        return normalizedDrawingType;
    }

    private static Dictionary<string, (string FocusA, string FocusB)> LoadPrompts()
    {
        Dictionary<string, (string FocusA, string FocusB)> result = new(StringComparer.OrdinalIgnoreCase);
        string content = ReadEmbeddedResource();
        string[] blocks = content.Split("---", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (string block in blocks)
        {
            ParseBlock(block, result);
        }

        RegisterAliases(result);

        if (!result.ContainsKey("default"))
        {
            result["default"] = (
                "Odczytaj wszystkie widoczne wymiary, materiały i tabele.",
                "Zweryfikuj tabele i opisy z geometrią rysunku.");
        }

        return result;
    }

    private static void ParseBlock(
        string block,
        Dictionary<string, (string FocusA, string FocusB)> result)
    {
        List<string> types = new();
        bool isFocusB = false;
        StringBuilder body = new();

        foreach (string rawLine in block.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            string trimmed = line.Trim();

            if (trimmed.Length == 0 && types.Count == 0)
            {
                continue;
            }

            Match headerMatch = FocusHeaderRegex.Match(trimmed);
            if (headerMatch.Success)
            {
                isFocusB = trimmed.StartsWith("## FOCUS_B:", StringComparison.OrdinalIgnoreCase);
                types.Add(headerMatch.Groups[1].Value.Trim());
                continue;
            }

            if (trimmed.StartsWith('#') && types.Count == 0)
            {
                continue;
            }

            body.AppendLine(line);
        }

        if (types.Count == 0)
        {
            return;
        }

        string prompt = body.ToString().Trim();
        foreach (string drawingType in types)
        {
            string key = ExtractionFocusRouter.NormalizeDrawingType(drawingType);
            AssignPrompt(result, key, prompt, isFocusB);
        }
    }

    private static void AssignPrompt(
        Dictionary<string, (string FocusA, string FocusB)> result,
        string key,
        string prompt,
        bool isFocusB)
    {
        if (!result.TryGetValue(key, out (string FocusA, string FocusB) existing))
        {
            existing = (string.Empty, string.Empty);
        }

        if (isFocusB)
        {
            result[key] = (existing.FocusA, prompt);
        }
        else
        {
            result[key] = (prompt, existing.FocusB);
        }
    }

    private static void RegisterAliases(Dictionary<string, (string FocusA, string FocusB)> result)
    {
        foreach (KeyValuePair<string, string> alias in DrawingTypeAliases)
        {
            if (result.TryGetValue(alias.Value, out (string FocusA, string FocusB) canonical)
                && !result.ContainsKey(alias.Key))
            {
                result[alias.Key] = canonical;
            }
        }
    }

    private static string ReadEmbeddedResource()
    {
        Assembly assembly = typeof(AgentDefinitionLoader).Assembly;
        string? resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.Contains("extraction_focus_prompts", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return string.Empty;
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not open resource: {resourceName}");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
