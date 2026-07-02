using System.Reflection;
using System.Text.Json;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class DetailsSchemaReferenceLoader
{
    private const string ResourceSuffix = "details_schema_reference.json";

    private static readonly Lazy<(JsonElement Element, string Text)> CachedSchema =
        new(LoadSchemaReferenceCore);

    public static JsonElement LoadSchemaReference()
    {
        return CachedSchema.Value.Element.Clone();
    }

    public static string LoadSchemaReferenceText()
    {
        return CachedSchema.Value.Text;
    }

    private static (JsonElement Element, string Text) LoadSchemaReferenceCore()
    {
        string json = ReadEmbeddedJson();
        using JsonDocument document = JsonDocument.Parse(json);
        return (document.RootElement.Clone(), json);
    }

    private static string ReadEmbeddedJson()
    {
        Assembly assembly = typeof(Business.AIAgent.Core.AgentDefinitionLoader).Assembly;
        string? resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' not found.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not open resource stream: {resourceName}");

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
