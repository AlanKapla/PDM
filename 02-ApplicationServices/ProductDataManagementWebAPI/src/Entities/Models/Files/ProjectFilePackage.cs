using Entities.Models.Base;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;

namespace Entities.Models.Files
{
    /// <summary>
    /// Represents a package (folder/group) for organizing project files
    /// </summary>
    public class ProjectFilePackage : DeletableEntity
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
        
        // Navigation
        public Project Project { get; set; } = default!;
        public User Owner { get; set; } = default!;
        public User CreatedByUser { get; set; } = default!;
        public TenantMember OwnerTenantMember { get; set; } = default!;
        public TenantMember CreatedByTenantMember { get; set; } = default!;
        
        /// <summary>
        /// Parent package id (null = root directory)
        /// </summary>
        public Guid? ParentId { get; set; }

        // Navigation
        public ProjectFilePackage? Parent { get; set; }
        public ICollection<ProjectFilePackage> Children { get; set; } = new List<ProjectFilePackage>();

        /// <summary>
        /// Files belonging to this package
        /// </summary>
        public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
    }
}
