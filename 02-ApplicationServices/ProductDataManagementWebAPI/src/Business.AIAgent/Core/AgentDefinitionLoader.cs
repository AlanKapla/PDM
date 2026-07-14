using System.Reflection;

namespace Business.AIAgent.Core;

/// <summary>
/// Loads agent definitions from embedded .md resources.
/// Parses YAML-like frontmatter (--- block) without external libraries.
/// </summary>
public sealed class AgentDefinitionLoader
{
    private readonly Dictionary<string, AgentDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public AgentDefinition Load(string agentName)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(agentName, out AgentDefinition? cached))
            {
                return cached;
            }

            AgentDefinition definition = LoadFromEmbeddedResource(agentName);
            _cache[agentName] = definition;
            return definition;
        }
    }

    public IReadOnlyList<string> ListAvailableAgents()
    {
        Assembly assembly = typeof(AgentDefinitionLoader).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.Contains("Resources.Agents") && n.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(n => ExtractAgentNameFromResourceName(n))
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();
    }

    private AgentDefinition LoadFromEmbeddedResource(string agentName)
    {
        Assembly assembly = typeof(AgentDefinitionLoader).Assembly;
        string[] allResources = assembly.GetManifestResourceNames();

        string? resourceName = allResources.FirstOrDefault(r =>
            r.Contains("Resources.Agents") &&
            r.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
            ExtractAgentNameFromResourceName(r)?.Equals(agentName, StringComparison.OrdinalIgnoreCase) == true);

        if (resourceName is null)
        {
            throw new InvalidOperationException($"Agent definition not found: '{agentName}'. Available: {string.Join(", ", allResources.Where(r => r.Contains("Resources.Agents")))}");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not open resource stream for: {resourceName}");

        using StreamReader reader = new(stream);
        string content = reader.ReadToEnd();

        return ParseMarkdown(agentName, content);
    }

    private static string? ExtractAgentNameFromResourceName(string resourceName)
    {
        // e.g. Business.AIAgent.Resources.Agents.sub_agents.cost_estimate_agent.md
        // returns "cost-estimate-agent" (converts _ to -)
        int agentsIndex = resourceName.IndexOf("Agents.", StringComparison.Ordinal);
        if (agentsIndex < 0) { return null; }

        string rest = resourceName[(agentsIndex + "Agents.".Length)..];
        // Take last segment (file name without .md)
        string fileName = rest.Split('.')[^2]; // second to last before extension
        return fileName.Replace('_', '-');
    }

    private static AgentDefinition ParseMarkdown(string agentName, string content)
    {
        content = content.TrimStart('\uFEFF'); // strip BOM
        if (!content.StartsWith("---", StringComparison.Ordinal))
        {
            return new AgentDefinition
            {
                Name = agentName,
                SystemPrompt = content.Trim()
            };
        }

        int endFrontmatter = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (endFrontmatter < 0)
        {
            return new AgentDefinition
            {
                Name = agentName,
                SystemPrompt = content.Trim()
            };
        }

        string frontmatter = content[4..endFrontmatter].Trim();
        string systemPrompt = content[(endFrontmatter + 4)..].TrimStart('\r', '\n').Trim();

        Dictionary<string, string> scalar = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<string>> lists = new(StringComparer.OrdinalIgnoreCase);

        string? currentListKey = null;
        foreach (string rawLine in frontmatter.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');

            if (line.TrimStart().StartsWith("- ", StringComparison.Ordinal) && currentListKey is not null)
            {
                string item = line.TrimStart().Substring(2).Trim();
                if (!lists.ContainsKey(currentListKey))
                {
                    lists[currentListKey] = [];
                }
                lists[currentListKey].Add(item);
                continue;
            }

            int colonIndex = line.IndexOf(':');
            if (colonIndex <= 0) { continue; }

            string key = line[..colonIndex].Trim();
            string value = line[(colonIndex + 1)..].Trim();

            if (string.IsNullOrEmpty(value))
            {
                currentListKey = key;
            }
            else
            {
                currentListKey = null;
                scalar[key] = value;
            }
        }

        return new AgentDefinition
        {
            Name = scalar.GetValueOrDefault("name", agentName),
            Description = scalar.GetValueOrDefault("description"),
            Model = scalar.GetValueOrDefault("model", "gpt-4o"),
            Temperature = float.TryParse(scalar.GetValueOrDefault("temperature", "0.7"), System.Globalization.CultureInfo.InvariantCulture, out float temp) ? temp : 0.7f,
            MaxTokens = int.TryParse(scalar.GetValueOrDefault("max_tokens", "4096"), out int maxTok) ? maxTok : 4096,
            MaxIterations = int.TryParse(scalar.GetValueOrDefault("max_iterations", "10"), out int maxIter) ? maxIter : 10,
            Tools = lists.GetValueOrDefault("tools") ?? [],
            SubAgents = lists.GetValueOrDefault("sub_agents") ?? [],
            SystemPrompt = systemPrompt
        };
    }
}
