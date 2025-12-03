using Entities.Models.Base;

namespace Entities.Models
{
    /// <summary>
    /// Reprezentuje plik udostępniony innemu członkowi projektu
    /// </summary>
    public class SharedProjectFile : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid ProjectFileId { get; set; }
        
        /// <summary>
        /// ID użytkownika, który udostępnia plik
        /// </summary>
        public Guid SharedByUserId { get; set; }
        
        /// <summary>
        /// ID użytkownika, któremu udostępniono plik
        /// </summary>
        public Guid SharedWithUserId { get; set; }
        
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Project Project { get; set; } = default!;
        public ProjectFile ProjectFile { get; set; } = default!;
        public User SharedByUser { get; set; } = default!;
        public User SharedWithUser { get; set; } = default!;
        public TenantMember SharedByTenantMember { get; set; } = default!;
        public TenantMember SharedWithTenantMember { get; set; } = default!;
    }
}
