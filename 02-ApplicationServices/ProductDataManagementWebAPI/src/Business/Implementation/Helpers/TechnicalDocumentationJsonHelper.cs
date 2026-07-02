using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Implementation.Helpers.JsonConverters;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Helpers;

public static class TechnicalDocumentationJsonHelper
{
    public static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            AllowTrailingCommas = true
        };

        options.Converters.Add(new FlexibleDoubleJsonConverter());
        options.Converters.Add(new FlexibleIntJsonConverter());
        options.Converters.Add(new MaterialQuantityJsonConverter());
        options.Converters.Add(new MaterialQuantityListJsonConverter());
        options.Converters.Add(new FoundationSectionJsonConverter());
        options.Converters.Add(new FloorSectionJsonConverter());
        options.Converters.Add(new RoofSectionJsonConverter());
        options.Converters.Add(new DrawingInstallationListJsonConverter());
        options.Converters.Add(new DrawingClassificationJsonConverter());
        options.Converters.Add(new FloorPlanDrawingJsonConverter());
        options.Converters.Add(new InstallationsSummaryJsonConverter());
        options.Converters.Add(new MaterialScheduleJsonConverter());
        options.Converters.Add(new AuditResultJsonConverter());
        options.Converters.Add(new ValidationReportJsonConverter());
        options.Converters.Add(new ProjectModelWarningJsonConverter());

        return options;
    }

    public static JsonSerializerOptions CreateCompactSerializerOptions()
    {
        JsonSerializerOptions options = CreateSerializerOptions();
        options.WriteIndented = false;
        return options;
    }

    public static string ExtractJson(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return "{}";
        }

        string text = StripMarkdownWrappers(response.Trim());
        string? payload = TryExtractJsonPayload(text);

        if (payload is null)
        {
            return "{}";
        }

        return AiGeneratedJsonSanitizer.Sanitize(payload);
    }

    public static T DeserializeAgentResponse<T>(
        string response,
        JsonSerializerOptions options,
        T fallback,
        ILogger? logger = null,
        string? context = null)
        where T : class
    {
        string json = ExtractJson(response);

        if (json == "{}")
        {
            return fallback;
        }

        try
        {
            T? result = JsonSerializer.Deserialize<T>(json, options);
            return result ?? fallback;
        }
        catch (JsonException ex)
        {
            string preview = json.Length > 120 ? json[..120] : json;
            logger?.LogWarning(
                ex,
                "Failed to deserialize {Context}. JSON length: {Length}, preview: {Preview}",
                context ?? typeof(T).Name,
                json.Length,
                preview);

            return fallback;
        }
    }

    private static string StripMarkdownWrappers(string text)
    {
        string result = text;

        if (result.Length > 0 && result[0] == '\uFEFF')
        {
            result = result[1..];
        }

        if (result.Contains("```", StringComparison.Ordinal))
        {
            result = ExtractFromCodeFenceBlock(result);
        }

        result = result.Trim();

        if (result.Length >= 2
            && result[0] == '`'
            && result[^1] == '`'
            && !result.StartsWith("``", StringComparison.Ordinal))
        {
            result = result[1..^1].Trim();
        }

        return result.Trim();
    }

    private static string ExtractFromCodeFenceBlock(string text)
    {
        int fenceStart = text.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart < 0)
        {
            return text;
        }

        int contentStart = fenceStart + 3;
        int lineEnd = text.IndexOf('\n', contentStart);

        if (lineEnd < 0)
        {
            string afterFence = text[contentStart..].TrimStart();
            if (afterFence.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                contentStart += 4;
            }
        }
        else
        {
            contentStart = lineEnd + 1;
        }

        int fenceEnd = text.IndexOf("```", contentStart, StringComparison.Ordinal);
        if (fenceEnd > contentStart)
        {
            return text[contentStart..fenceEnd].Trim();
        }

        string remainder = text[contentStart..].Trim();
        if (remainder.EndsWith("```", StringComparison.Ordinal))
        {
            remainder = remainder[..^3].TrimEnd();
        }

        return remainder;
    }

    private static string? TryExtractJsonPayload(string text)
    {
        int objectStart = text.IndexOf('{');
        int arrayStart = text.IndexOf('[');

        int start = ResolveJsonStart(objectStart, arrayStart);
        if (start < 0)
        {
            return null;
        }

        int end = FindMatchingJsonEnd(text, start);
        if (end < start)
        {
            char close = text[start] == '{' ? '}' : ']';
            end = text.LastIndexOf(close);
        }

        if (end < start)
        {
            return null;
        }

        return text[start..(end + 1)];
    }

    private static int ResolveJsonStart(int objectStart, int arrayStart)
    {
        if (objectStart >= 0 && arrayStart >= 0)
        {
            return Math.Min(objectStart, arrayStart);
        }

        if (objectStart >= 0)
        {
            return objectStart;
        }

        return arrayStart;
    }

    private static int FindMatchingJsonEnd(string text, int start)
    {
        char open = text[start];
        char close = open == '{' ? '}' : ']';
        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = start; i < text.Length; i++)
        {
            char current = text[i];

            if (inString)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (current == '\\')
                {
                    escape = true;
                    continue;
                }

                if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                continue;
            }

            if (current == open)
            {
                depth++;
                continue;
            }

            if (current == close)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
