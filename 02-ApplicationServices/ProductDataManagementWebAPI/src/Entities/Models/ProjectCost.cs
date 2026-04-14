using Entities.Models.Base;

namespace Entities.Models
{
    /// <summary>
    /// Reprezentuje koszt poniesiony przez członka projektu
    /// </summary>
    public class ProjectCost : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// ID użytkownika, który dodał koszt (członek projektu)
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// Nazwa kosztu (wymagane)
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Miejsce poniesienia kosztu (opcjonalne)
        /// </summary>
        public string? Place { get; set; }
        
        /// <summary>
        /// Data poniesienia kosztu (wymagane)
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Opis kosztu (opcjonalne)
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Kwota netto (opcjonalne jeśli dostarczone brutto)
        /// </summary>
        public decimal? NetAmount { get; set; }
        
        /// <summary>
        /// Kwota brutto - wyliczana z netto + VAT lub wprowadzana ręcznie
        /// </summary>
        public decimal GrossAmount { get; set; }
        
        /// <summary>
        /// Czy koszt został zamknięty/zapłacony
        /// </summary>
        public bool IsClosed { get; set; } = false;
        
        /// <summary>
        /// Czy załączono dokument poświadczający
        /// </summary>
        public bool HasDocument { get; set; } = false;
        
        /// <summary>
        /// Nazwa pliku dokumentu w blob storage (jeśli załączony)
        /// </summary>
        public string? DocumentFileName { get; set; }
        
        /// <summary>
        /// Ścieżka do dokumentu w blob storage: tenantId/projectId/userId/costId/filename
        /// </summary>
        public string? DocumentBlobPath { get; set; }
        
        /// <summary>
        /// Typ MIME dokumentu
        /// </summary>
        public string? DocumentContentType { get; set; }
        
        /// <summary>
        /// Rozmiar dokumentu w bajtach
        /// </summary>
        public long? DocumentSizeBytes { get; set; }
        
        /// <summary>
        /// Data utworzenia kosztu
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Data ostatniej modyfikacji
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Czy koszt został usunięty (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Project Project { get; set; } = default!;
        public TenantMember TenantMember { get; set; } = default!;
        public ICollection<SharedProjectCost> SharedWith { get; set; } = new List<SharedProjectCost>();
    }
}
