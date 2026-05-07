using Entities.Models.Base;
using Entities.Models.Users;

namespace Entities.Models.Files
{
    /// <summary>
    /// Reprezentuje konkretną wersję pliku projektu
    /// </summary>
    public class ProjectFileVersion : DeletableEntity
    {
        public Guid ProjectFileId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// Numer wersji (1, 2, 3, ...)
        /// </summary>
        public int VersionNumber { get; set; }
        
        /// <summary>
        /// ID użytkownika, który stworzył tę wersję
        /// </summary>
        public Guid CreatedByUserId { get; set; }
        
        /// <summary>
        /// Nazwa fizycznego pliku na blob storage (GUID + rozszerzenie)
        /// </summary>
        public string BlobFileName { get; set; } = default!;
        
        /// <summary>
        /// Pełna ścieżka do pliku na blob storage: tenantId/projectId/fileId/versionNumber/blobFileName
        /// </summary>
        public string BlobPath { get; set; } = default!;
        
        /// <summary>
        /// Typ pliku (MIME type)
        /// </summary>
        public string ContentType { get; set; } = default!;
        
        /// <summary>
        /// Rozmiar pliku w bajtach
        /// </summary>
        public long FileSizeBytes { get; set; }
        
        /// <summary>
        /// Data utworzenia wersji
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public ProjectFile ProjectFile { get; set; } = default!;
        public User CreatedByUser { get; set; } = default!;
        public ICollection<ProjectFileVersionComment> Comments { get; set; } = new List<ProjectFileVersionComment>();
    }
}
