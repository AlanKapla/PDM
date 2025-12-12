using Entities.Models.Base;

namespace Entities.Models
{
    /// <summary>
    /// Represents a package (folder/group) for organizing project files
    /// </summary>
    public class ProjectFilePackage : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// Owner of the package (user who created it)
        /// </summary>
        public Guid OwnerId { get; set; }
        
        /// <summary>
        /// Package name - unique per tenant + project + owner
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// User who created the package
        /// </summary>
        public Guid CreatedByUserId { get; set; }
        
        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Project Project { get; set; } = default!;
        public User Owner { get; set; } = default!;
        public User CreatedByUser { get; set; } = default!;
        public TenantMember OwnerTenantMember { get; set; } = default!;
        public TenantMember CreatedByTenantMember { get; set; } = default!;
        
        /// <summary>
        /// Files belonging to this package
        /// </summary>
        public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
    }
}
