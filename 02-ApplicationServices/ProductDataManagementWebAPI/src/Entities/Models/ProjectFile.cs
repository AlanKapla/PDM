using Entities.Models.Base;

namespace Entities.Models
{
    /// <summary>
    /// Reprezentuje plik dodany do projektu
    /// </summary>
    public class ProjectFile : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UploadedByUserId { get; set; }
        
        /// <summary>
        /// Nazwa pliku z rozszerzeniem
        /// </summary>
        public string FileName { get; set; } = default!;
        
        /// <summary>
        /// Nazwa paczki (katalogu) w której znajduje się plik
        /// </summary>
        public string PackageName { get; set; } = default!;
        
        /// <summary>
        /// Nazwa wyświetlana na UI
        /// </summary>
        public string DisplayName { get; set; } = default!;
        
        /// <summary>
        /// Typ pliku (MIME type)
        /// </summary>
        public string ContentType { get; set; } = default!;
        
        /// <summary>
        /// Rozmiar pliku w bajtach
        /// </summary>
        public long FileSizeBytes { get; set; }
        
        /// <summary>
        /// Pełna ścieżka do pliku na blob storage: tenantId/projectId/userId/packageName/fileName
        /// </summary>
        public string BlobPath { get; set; } = default!;
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Project Project { get; set; } = default!;
        public User UploadedByUser { get; set; } = default!;
        public TenantMember UploadedByTenantMember { get; set; } = default!;
    }
}
