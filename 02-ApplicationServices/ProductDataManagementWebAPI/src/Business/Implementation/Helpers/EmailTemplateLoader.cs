using System.Reflection;

namespace Business.Implementation.Helpers
{
    public static class EmailTemplateLoader
    {
        private static readonly Assembly ResourceAssembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// Ładuje surowy szablon HTML z zasobów osadzonych (bez podmiany placeholderów).
        /// </summary>
        public static string LoadRaw(string templateName)
        {
            string resourceName = ResourceAssembly
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith($".{templateName}", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Email template '{templateName}' not found in embedded resources.");

            using Stream stream = ResourceAssembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not open embedded resource stream for '{resourceName}'.");

            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Ładuje szablon HTML z zasobów osadzonych i podmienia placeholdery podane w słowniku.
        /// Placeholdery w pliku mają format {klucz}.
        /// </summary>
        public static string Load(string templateName, IReadOnlyDictionary<string, string> placeholders)
        {
            string html = LoadRaw(templateName);

            foreach (KeyValuePair<string, string> pair in placeholders)
            {
                html = html.Replace($"{{{pair.Key}}}", pair.Value, StringComparison.Ordinal);
            }

            return html;
        }
    }
}
