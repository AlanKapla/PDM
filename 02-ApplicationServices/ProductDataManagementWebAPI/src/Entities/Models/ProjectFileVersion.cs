using Entities.Models.Base;

namespace Entities.Models
{
    /// <summary>
    /// Reprezentuje konkretną wersję pliku projektu
    /// </summary>
    public class ProjectFileVersion : BaseEntity
    {
        public Guid ProjectFileId { get; set; }
        
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
        
        /// <summary>
        /// Czy wersja została usunięta (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ProjectFile ProjectFile { get; set; } = default!;
        public User CreatedByUser { get; set; } = default!;
        public ICollection<ProjectFileVersionComment> Comments { get; set; } = new List<ProjectFileVersionComment>();
    }
}
