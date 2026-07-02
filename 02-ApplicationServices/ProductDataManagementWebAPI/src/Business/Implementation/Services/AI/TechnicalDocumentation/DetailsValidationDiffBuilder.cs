using System.Globalization;
using System.Text.Json;
using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class DetailsValidationDiffBuilder
{
    private const double RelativeTolerance = 0.01;
    private const double AbsoluteTolerance = 0.1;

    public static List<DetailsValidationDifference> Compare(string expectedJson, string actualJson)
    {
        List<DetailsValidationDifference> differences = new();

        using JsonDocument expectedDoc = JsonDocument.Parse(expectedJson);
        using JsonDocument actualDoc = JsonDocument.Parse(actualJson);

        CompareElements(
            expectedDoc.RootElement,
            actualDoc.RootElement,
            string.Empty,
            differences);

        return differences;
    }

    private static void CompareElements(
        JsonElement expected,
        JsonElement actual,
        string path,
        List<DetailsValidationDifference> differences)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            AddDifference(differences, path, "Różny typ wartości", FormatValue(expected), FormatValue(actual), "high");
            return;
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                CompareObjects(expected, actual, path, differences);
                break;
            case JsonValueKind.Array:
                CompareArrays(expected, actual, path, differences);
                break;
            case JsonValueKind.Number:
                if (!NumbersEqual(expected.GetDouble(), actual.GetDouble()))
                {
                    AddDifference(
                        differences,
                        path,
                        "Różna wartość liczbowa",
                        FormatNumber(expected.GetDouble()),
                        FormatNumber(actual.GetDouble()),
                        "medium");
                }

                break;
            case JsonValueKind.String:
                string expectedText = expected.GetString() ?? string.Empty;
                string actualText = actual.GetString() ?? string.Empty;
                if (!string.Equals(expectedText, actualText, StringComparison.Ordinal))
                {
                    AddDifference(differences, path, "Różna wartość tekstowa", expectedText, actualText, "medium");
                }

                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                if (expected.GetBoolean() != actual.GetBoolean())
                {
                    AddDifference(
                        differences,
                        path,
                        "Różna wartość logiczna",
                        expected.GetBoolean().ToString(),
                        actual.GetBoolean().ToString(),
                        "low");
                }

                break;
            case JsonValueKind.Null:
                break;
        }
    }

    private static void CompareObjects(
        JsonElement expected,
        JsonElement actual,
        string path,
        List<DetailsValidationDifference> differences)
    {
        HashSet<string> propertyNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty property in expected.EnumerateObject())
        {
            propertyNames.Add(property.Name);
        }

        foreach (JsonProperty property in actual.EnumerateObject())
        {
            propertyNames.Add(property.Name);
        }

        foreach (string propertyName in propertyNames.OrderBy(name => name, StringComparer.Ordinal))
        {
            string childPath = string.IsNullOrEmpty(path) ? propertyName : $"{path}.{propertyName}";
            bool expectedHas = TryGetPropertyIgnoreCase(expected, propertyName, out JsonElement expectedChild);
            bool actualHas = TryGetPropertyIgnoreCase(actual, propertyName, out JsonElement actualChild);

            if (!expectedHas && actualHas)
            {
                AddDifference(differences, childPath, "Pole dodatkowe w wyniku", null, FormatValue(actualChild), "low");
                continue;
            }

            if (expectedHas && !actualHas)
            {
                AddDifference(differences, childPath, "Brakujące pole", FormatValue(expectedChild), null, "high");
                continue;
            }

            if (!expectedHas)
            {
                continue;
            }

            if (expectedChild.ValueKind == JsonValueKind.Null && actualChild.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            if (expectedChild.ValueKind == JsonValueKind.Null)
            {
                AddDifference(differences, childPath, "Brakujące pole", "null", FormatValue(actualChild), "high");
                continue;
            }

            if (actualChild.ValueKind == JsonValueKind.Null)
            {
                AddDifference(differences, childPath, "Puste pole", FormatValue(expectedChild), "null", "high");
                continue;
            }

            CompareElements(expectedChild, actualChild, childPath, differences);
        }
    }

    private static void CompareArrays(
        JsonElement expected,
        JsonElement actual,
        string path,
        List<DetailsValidationDifference> differences)
    {
        int expectedCount = expected.GetArrayLength();
        int actualCount = actual.GetArrayLength();

        if (expectedCount != actualCount)
        {
            AddDifference(
                differences,
                path,
                "Różna liczba elementów tablicy",
                expectedCount.ToString(CultureInfo.InvariantCulture),
                actualCount.ToString(CultureInfo.InvariantCulture),
                "high");
        }

        int compareCount = Math.Min(expectedCount, actualCount);
        for (int index = 0; index < compareCount; index++)
        {
            CompareElements(
                expected[index],
                actual[index],
                $"{path}[{index}]",
                differences);
        }

        for (int index = compareCount; index < expectedCount; index++)
        {
            AddDifference(
                differences,
                $"{path}[{index}]",
                "Brakujący element tablicy",
                FormatValue(expected[index]),
                null,
                "high");
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool IsNullOrEmpty(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => true,
            JsonValueKind.Undefined => true,
            JsonValueKind.String => string.IsNullOrWhiteSpace(element.GetString()),
            JsonValueKind.Array => element.GetArrayLength() == 0,
            JsonValueKind.Object => !element.EnumerateObject().Any(),
            _ => false
        };
    }

    private static bool NumbersEqual(double left, double right)
    {
        if (double.IsNaN(left) && double.IsNaN(right))
        {
            return true;
        }

        double delta = Math.Abs(left - right);
        if (delta <= AbsoluteTolerance)
        {
            return true;
        }

        double scale = Math.Max(Math.Abs(left), Math.Abs(right));
        if (scale <= 0)
        {
            return true;
        }

        return delta / scale <= RelativeTolerance;
    }

    private static void AddDifference(
        List<DetailsValidationDifference> differences,
        string path,
        string issue,
        string? expected,
        string? actual,
        string severity)
    {
        differences.Add(new DetailsValidationDifference
        {
            Path = path,
            Issue = issue,
            Expected = expected,
            Actual = actual,
            Severity = severity
        });
    }

    private static string FormatValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => FormatNumber(element.GetDouble()),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
            JsonValueKind.Object => "{...}",
            _ => element.GetRawText()
        };
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
