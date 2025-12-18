using Entities.Models.Base;

namespace Entities.Models
{
    /// <summary>
    /// Reprezentuje plik dodany do projektu (metadane i właściciel)
    /// </summary>
    public class ProjectFile : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// ID paczki do której należy plik
        /// </summary>
        public Guid ProjectFilePackageId { get; set; }
        
        /// <summary>
        /// ID właściciela pliku (użytkownik, który pierwotnie przesłał plik)
        /// </summary>
        public Guid OwnerId { get; set; }
        
        /// <summary>
        /// Nazwa pliku źródłowego z rozszerzeniem
        /// </summary>
        public string FileName { get; set; } = default!;
        
        /// <summary>
        /// Nazwa wyświetlana na UI
        /// </summary>
        public string DisplayName { get; set; } = default!;
        
        /// <summary>
        /// Data utworzenia pliku
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// ID aktualnie aktywnej wersji
        /// </summary>
        public Guid? CurrentVersionId { get; set; }
        
        /// <summary>
        /// Czy plik został usunięty (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Project Project { get; set; } = default!;
        public ProjectFilePackage Package { get; set; } = default!;
        public User Owner { get; set; } = default!;
        public TenantMember OwnerTenantMember { get; set; } = default!;
        
        /// <summary>
        /// Aktualnie aktywna wersja pliku
        /// </summary>
        public ProjectFileVersion? CurrentVersion { get; set; }
        
        /// <summary>
        /// Wszystkie wersje pliku
        /// </summary>
        public ICollection<ProjectFileVersion> Versions { get; set; } = new List<ProjectFileVersion>();
        
        /// <summary>
        /// Użytkownicy, którym udostępniono ten plik
        /// </summary>
        public ICollection<SharedProjectFile> SharedWith { get; set; } = new List<SharedProjectFile>();
    }
}
