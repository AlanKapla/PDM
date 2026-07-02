using Business.AIAgent.Core;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TechnicalDocumentationSystemPromptBuilder
{
    internal const string SchemaReferencePlaceholder = "{SCHEMA_REFERENCE_PLACEHOLDER}";

    public static string ApplySchemaReference(string systemPrompt)
    {
        if (!systemPrompt.Contains(SchemaReferencePlaceholder, StringComparison.Ordinal))
        {
            return systemPrompt;
        }

        string schemaText = DetailsSchemaReferenceLoader.LoadSchemaReferenceText();
        return systemPrompt.Replace(SchemaReferencePlaceholder, schemaText, StringComparison.Ordinal);
    }

    public static string ResolveSystemPrompt(AgentDefinitionLoader agentDefinitionLoader, string agentName)
    {
        AgentDefinition definition = agentDefinitionLoader.Load(agentName);
        return ApplySchemaReference(definition.SystemPrompt);
    }
}
