using Microsoft.AspNetCore.Http;

namespace Business.Interfaces.Helpers
{
    /// <summary>
    /// Helper class for document validation
    /// </summary>
    public static class DocumentValidationHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".pdf", ".png" };
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/jpg", "image/png", "application/pdf" };
        private const long MaxDocumentSize = 50L * 1024 * 1024; // 50MB

        /// <summary>
        /// Validates if document has allowed file type (JPEG, JPG, PDF)
        /// </summary>
        public static bool IsValidDocumentType(IFormFile? document)
        {
            if (document == null)
            {
                return true;
            }

            string extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            string contentType = document.ContentType.ToLowerInvariant();

            return AllowedExtensions.Contains(extension) && AllowedContentTypes.Contains(contentType);
        }

        /// <summary>
        /// Validates if document size does not exceed maximum allowed size (50MB)
        /// </summary>
        public static bool IsValidDocumentSize(IFormFile? document)
        {
            if (document == null) return true;

            return document.Length <= MaxDocumentSize;
        }

        /// <summary>
        /// Gets the maximum allowed document size in bytes
        /// </summary>
        public static long GetMaxDocumentSize() => MaxDocumentSize;

        /// <summary>
        /// Gets allowed file extensions
        /// </summary>
        public static IReadOnlyList<string> GetAllowedExtensions() => AllowedExtensions;

        /// <summary>
        /// Gets allowed content types
        /// </summary>
        public static IReadOnlyList<string> GetAllowedContentTypes() => AllowedContentTypes;
    }
}
