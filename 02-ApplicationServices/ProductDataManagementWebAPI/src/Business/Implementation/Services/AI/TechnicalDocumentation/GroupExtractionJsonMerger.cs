using System.Text.Json;
using System.Text.Json.Nodes;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class GroupExtractionJsonMerger
{
    public static string Merge(IReadOnlyList<string> jsonFragments)
    {
        if (jsonFragments.Count == 0)
        {
            return "{}";
        }

        if (jsonFragments.Count == 1)
        {
            return jsonFragments[0];
        }

        JsonObject merged = new();

        foreach (string fragment in jsonFragments)
        {
            if (string.IsNullOrWhiteSpace(fragment))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(fragment);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                merged[property.Name] = JsonNode.Parse(property.Value.GetRawText());
            }
        }

        return merged.ToJsonString();
    }
}
