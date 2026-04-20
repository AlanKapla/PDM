using System.Reflection;

namespace Business.Implementation.Helpers
{
    public static class EmailTemplateLoader
    {
        private static readonly Assembly ResourceAssembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// Ładuje szablon HTML z zasobów osadzonych i podmienia placeholdery podane w słowniku.
        /// Placeholdery w pliku mają format {klucz}.
        /// </summary>
        public static string Load(string templateName, IReadOnlyDictionary<string, string> placeholders)
        {
            string resourceName = ResourceAssembly
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith($".{templateName}", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Email template '{templateName}' not found in embedded resources.");

            using var stream = ResourceAssembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not open embedded resource stream for '{resourceName}'.");

            using var reader = new StreamReader(stream);
            string html = reader.ReadToEnd();

            foreach (var (key, value) in placeholders)
            {
                html = html.Replace($"{{{key}}}", value, StringComparison.Ordinal);
            }

            return html;
        }
    }
}
