using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public enum DrawingViewBucket
{
    Unknown,
    Plan,
    Section,
    Detail,
    Elevation,
    Foundation,
    Roof
}

public static class DrawingViewClassifier
{
    public static DrawingViewBucket Classify(DrawingClassification classification)
    {
        string type = classification.DrawingType.Trim().ToLowerInvariant();
        string title = classification.Title?.Trim().ToLowerInvariant() ?? string.Empty;
        string combined = $"{type} {title}";

        if (ContainsAny(combined, "fundament"))
        {
            return DrawingViewBucket.Foundation;
        }

        if (ContainsAny(combined, "dach", "wiezba", "więźba", "wiéźba"))
        {
            return DrawingViewBucket.Roof;
        }

        if (ContainsAny(combined, "rzut", "plan", "poziom"))
        {
            return DrawingViewBucket.Plan;
        }

        if (ContainsAny(combined, "przekrój", "przekroj", "secion", "section"))
        {
            return DrawingViewBucket.Section;
        }

        if (ContainsAny(combined, "detal", "szczegół", "szczegol", "zbrojen"))
        {
            return DrawingViewBucket.Detail;
        }

        if (ContainsAny(combined, "elewacja", "fasada"))
        {
            return DrawingViewBucket.Elevation;
        }

        return DrawingViewBucket.Unknown;
    }

    public static string BuildFloorLabel(DrawingClassification classification)
    {
        if (!string.IsNullOrWhiteSpace(classification.FloorLevel))
        {
            return classification.FloorLevel.Trim();
        }

        string key = BuildFloorKey(classification);
        return key switch
        {
            "parter" => "Parter",
            "pietro" => "Piętro",
            "poddasze" => "Poddasze",
            "piwnica" => "Piwnica",
            _ => key
        };
    }

    public static int BuildFloorOrder(DrawingClassification classification)
    {
        if (classification.FloorOrder.HasValue)
        {
            return classification.FloorOrder.Value;
        }

        string key = BuildFloorKey(classification);
        return key switch
        {
            "parter" => 0,
            "pietro" => 1,
            "poddasze" => 99,
            "piwnica" => -1,
            _ => 50
        };
    }

    public static string BuildFloorKey(DrawingClassification classification)
    {
        if (!string.IsNullOrWhiteSpace(classification.FloorLevel))
        {
            return classification.FloorLevel.Trim().ToLowerInvariant();
        }

        if (classification.FloorOrder.HasValue)
        {
            return $"floor-{classification.FloorOrder.Value}";
        }

        string title = classification.Title?.Trim().ToLowerInvariant() ?? string.Empty;
        string type = classification.DrawingType.Trim().ToLowerInvariant();
        string combined = $"{type} {title}";

        if (combined.Contains("piętro", StringComparison.Ordinal) || combined.Contains("pietro", StringComparison.Ordinal))
        {
            return "pietro";
        }

        if (combined.Contains("parter", StringComparison.Ordinal) || combined.Contains("poziom 0", StringComparison.Ordinal))
        {
            return "parter";
        }

        if (combined.Contains("poddasze", StringComparison.Ordinal))
        {
            return "poddasze";
        }

        return "budowlane";
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
