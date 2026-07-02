using System.Text.Json;
using Business.Interfaces.Services.TechnicalDocumentation;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class ExtractionDiffEngine
{
    private const double CriticalNumericToleranceRatio = 0.01;

    private static readonly string[] CriticalFieldTokens =
    [
        "totalmass",
        "totalvolume",
        "aream2",
        "concreteclass",
        "reinforcement",
        "total_mass",
        "total_volume",
        "match",
    ];

    public static ExtractionDiffResult Compare(string jsonA, string jsonB)
    {
        ExtractionDiffResult result = new();

        if (string.IsNullOrWhiteSpace(jsonA) && string.IsNullOrWhiteSpace(jsonB))
        {
            return result;
        }

        using JsonDocument documentA = JsonDocument.Parse(string.IsNullOrWhiteSpace(jsonA) ? "{}" : jsonA);
        using JsonDocument documentB = JsonDocument.Parse(string.IsNullOrWhiteSpace(jsonB) ? "{}" : jsonB);

        CompareElements(documentA.RootElement, documentB.RootElement, string.Empty, result);
        result.HasCriticalDifferences = result.Differences.Any(diff => diff.IsCritical);
        result.HasMinorDifferences = result.Differences.Any(diff => !diff.IsCritical);
        return result;
    }

    private static void CompareElements(
        JsonElement elementA,
        JsonElement elementB,
        string path,
        ExtractionDiffResult result)
    {
        if (elementA.ValueKind != elementB.ValueKind)
        {
            AddDifference(path, elementA, elementB, result);
            return;
        }

        switch (elementA.ValueKind)
        {
            case JsonValueKind.Object:
                HashSet<string> propertyNames = new(StringComparer.Ordinal);
                foreach (JsonProperty property in elementA.EnumerateObject())
                {
                    propertyNames.Add(property.Name);
                }

                foreach (JsonProperty property in elementB.EnumerateObject())
                {
                    propertyNames.Add(property.Name);
                }

                foreach (string propertyName in propertyNames)
                {
                    string childPath = string.IsNullOrEmpty(path) ? propertyName : $"{path}.{propertyName}";
                    bool hasA = elementA.TryGetProperty(propertyName, out JsonElement childA);
                    bool hasB = elementB.TryGetProperty(propertyName, out JsonElement childB);

                    if (!hasA || !hasB)
                    {
                        AddDifference(childPath, hasA ? childA : default, hasB ? childB : default, result);
                        continue;
                    }

                    CompareElements(childA, childB, childPath, result);
                }

                break;

            case JsonValueKind.Array:
                int maxLength = Math.Max(elementA.GetArrayLength(), elementB.GetArrayLength());
                for (int index = 0; index < maxLength; index++)
                {
                    string childPath = $"{path}[{index}]";
                    bool hasA = index < elementA.GetArrayLength();
                    bool hasB = index < elementB.GetArrayLength();

                    if (!hasA || !hasB)
                    {
                        AddDifference(childPath, hasA ? elementA[index] : default, hasB ? elementB[index] : default, result);
                        continue;
                    }

                    CompareElements(elementA[index], elementB[index], childPath, result);
                }

                break;

            default:
                if (!ValuesEqual(elementA, elementB))
                {
                    AddDifference(path, elementA, elementB, result);
                }

                break;
        }
    }

    private static void AddDifference(
        string path,
        JsonElement valueA,
        JsonElement valueB,
        ExtractionDiffResult result)
    {
        result.Differences.Add(new ExtractionFieldDiff
        {
            FieldPath = path,
            ValueA = FormatValue(valueA),
            ValueB = FormatValue(valueB),
            IsCritical = IsCriticalDifference(path, valueA, valueB),
        });
    }

    private static bool IsCriticalDifference(string path, JsonElement valueA, JsonElement valueB)
    {
        string normalizedPath = path.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        foreach (string token in CriticalFieldTokens)
        {
            if (normalizedPath.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (valueA.ValueKind == JsonValueKind.Number && valueB.ValueKind == JsonValueKind.Number)
        {
            double numberA = valueA.GetDouble();
            double numberB = valueB.GetDouble();

            if (numberA == 0 && numberB == 0)
            {
                return false;
            }

            double baseline = Math.Max(Math.Abs(numberA), Math.Abs(numberB));
            if (baseline <= 0)
            {
                return true;
            }

            return Math.Abs(numberA - numberB) / baseline > CriticalNumericToleranceRatio;
        }

        return valueA.ValueKind != valueB.ValueKind;
    }

    private static bool ValuesEqual(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        return left.ValueKind switch
        {
            JsonValueKind.String => string.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal),
            JsonValueKind.Number => left.GetDouble().Equals(right.GetDouble()),
            JsonValueKind.True or JsonValueKind.False => left.GetBoolean() == right.GetBoolean(),
            JsonValueKind.Null or JsonValueKind.Undefined => true,
            _ => left.GetRawText() == right.GetRawText(),
        };
    }

    private static string? FormatValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Undefined => null,
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => value.GetRawText(),
        };
    }
}
