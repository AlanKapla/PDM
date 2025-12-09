using System.Globalization;
using System.Text;

namespace Business.Interfaces.Helpers
{
    /// <summary>
    /// Helper class for file-related operations (blob storage paths, normalization)
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Normalizes package name for safe use in Azure Blob Storage path.
        /// Removes diacritics (Polish characters, accents), replaces spaces with underscores,
        /// and removes invalid characters.
        /// </summary>
        /// <param name="packageName">Package name to normalize</param>
        /// <returns>Normalized package name safe for blob storage path, or "default" if empty</returns>
        public static string NormalizePackageNameForBlobPath(string? packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return "default";
            }

            // Transliterate diacritics (Polish characters, accents) to ASCII
            string normalized = RemoveDiacritics(packageName);

            // Replace spaces with underscores
            normalized = normalized.Replace(' ', '_');

            // Remove invalid characters for Azure Blob Storage path
            // Allowed: letters, digits, - _ . /
            char[] invalidChars = Path.GetInvalidFileNameChars()
                .Concat(new[] { '<', '>', ':', '"', '|', '?', '*', '\\' })
                .Distinct()
                .ToArray();

            foreach (char c in invalidChars)
            {
                normalized = normalized.Replace(c, '_');
            }

            // Remove multiple consecutive underscores
            while (normalized.Contains("__"))
            {
                normalized = normalized.Replace("__", "_");
            }

            // Trim underscores from start and end
            normalized = normalized.Trim('_');

            return string.IsNullOrWhiteSpace(normalized) ? "default" : normalized;
        }

        /// <summary>
        /// Normalizes file name for safe use in Content-Disposition header (SAS URI).
        /// Removes diacritics (Polish characters, accents) and replaces problematic characters
        /// that may cause issues in HTTP headers. Preserves spaces as they are safe within
        /// quoted filename values and provide better readability in browsers/PDF viewers.
        /// </summary>
        /// <param name="fileName">Original file name (with or without extension)</param>
        /// <returns>Normalized file name safe for Content-Disposition header</returns>
        public static string NormalizeFileNameForContentDisposition(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "file";
            }

            // Preserve extension
            string? extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                return "file" + extension;
            }

            // Transliterate diacritics (Polish characters, accents) to ASCII
            string normalized = RemoveDiacritics(nameWithoutExtension);

            // Remove characters that are problematic in Content-Disposition header
            // Problematic: quotes ("), backslash (\), control characters, semicolon (;)
            // Safe: spaces, letters, digits, dash (-), underscore (_), dot (.), parentheses, brackets
            char[] allowedChars = normalized
                .Where(c => 
                    char.IsLetterOrDigit(c) || 
                    c == ' ' ||  // Preserve spaces - safe within quotes and more readable
                    c == '-' || 
                    c == '_' || 
                    c == '.' || 
                    c == '(' || 
                    c == ')' || 
                    c == '[' || 
                    c == ']')
                .ToArray();

            normalized = new string(allowedChars);

            // Remove multiple consecutive spaces
            while (normalized.Contains("  "))
            {
                normalized = normalized.Replace("  ", " ");
            }

            // Remove multiple consecutive underscores or dots
            while (normalized.Contains("__"))
            {
                normalized = normalized.Replace("__", "_");
            }
            while (normalized.Contains(".."))
            {
                normalized = normalized.Replace("..", ".");
            }

            // Trim spaces, underscores and dots from start and end
            normalized = normalized.Trim(' ', '_', '.');

            // If after normalization the name is empty, use default
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "file";
            }

            // Reattach extension (if present)
            return normalized + extension;
        }

        /// <summary>
        /// Removes diacritics (Polish characters, accents, etc.) by replacing them with ASCII equivalents.
        /// Handles special characters not covered by standard Unicode normalization.
        /// </summary>
        /// <param name="text">Text with diacritics</param>
        /// <returns>Text with diacritics removed</returns>
        private static string RemoveDiacritics(string text)
        {
            // Special handling for characters not handled by standard Unicode normalization
            // (these are separate characters, not base_character + diacritic composition)
            var specialCharsMap = new Dictionary<char, string>
            {
                {'ł', "l"}, {'Ł', "L"},     // Polish
                {'ø', "o"}, {'Ø', "O"},     // Scandinavian
                {'å', "a"}, {'Å', "A"},     // Scandinavian
                {'æ', "ae"}, {'Æ', "AE"},   // Scandinavian
                {'œ', "oe"}, {'Œ', "OE"},   // French
                {'ß', "ss"},                 // German
                {'ð', "d"}, {'Ð', "D"},     // Icelandic
                {'þ', "th"}, {'Þ', "TH"}    // Icelandic
            };

            foreach (var mapping in specialCharsMap)
            {
                text = text.Replace(mapping.Key.ToString(), mapping.Value);
            }

            // Unicode FormD normalization separates base characters from diacritics
            string normalized = text.Normalize(NormalizationForm.FormD);

            // Remove all diacritic characters (accents, hooks)
            var stringBuilder = new StringBuilder();
            foreach (char c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
