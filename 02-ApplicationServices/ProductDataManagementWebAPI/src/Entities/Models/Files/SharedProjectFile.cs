using Entities.Models.Base;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;

namespace Entities.Models.Files
{
    /// <summary>
    /// Reprezentuje udostępnienie paczki lub pliku innemu członkowi projektu
    /// Może udostępniać całą paczkę (PackageId, FileId=null)
    /// Może udostępniać pojedynczy plik (PackageId + FileId z Access=Allow)
    /// Może wykluczać plik z udostępnionej paczki (PackageId + FileId z Access=Deny)
    /// </summary>
    public class SharedProjectFile : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// ID paczki której dotyczy udostępnienie
        /// Zawsze wypełnione
        /// </summary>
        public Guid ProjectFilePackageId { get; set; }
        
        /// <summary>
        /// ID pliku (opcjonalne)
        /// NULL - udostępniona cała paczka
        /// NOT NULL - udostępniony konkretny plik lub wykluczenie pliku z paczki (zależy od Access)
        /// </summary>
        public Guid? ProjectFileId { get; set; }
        
        /// <summary>
        /// Typ dostępu do pliku
        /// Allow - jawne udostępnienie pliku
        /// Deny - wykluczenie pliku z udostępnionej paczki
        /// </summary>
        public ProjectFileAccess Access { get; set; }
        
        /// <summary>
        /// ID użytkownika, który udostępnia
        /// </summary>
        public Guid SharedByUserId { get; set; }
        
        /// <summary>
        /// ID użytkownika, któremu udostępniono
        /// </summary>
        public Guid SharedWithUserId { get; set; }
        
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Project Project { get; set; } = default!;
        public ProjectFilePackage ProjectFilePackage { get; set; } = default!;
        public ProjectFile? ProjectFile { get; set; }  // Nullable bo FileId może być null
        public User SharedByUser { get; set; } = default!;
        public User SharedWithUser { get; set; } = default!;
        public TenantMember SharedByTenantMember { get; set; } = default!;
        public TenantMember SharedWithTenantMember { get; set; } = default!;
    }
}
