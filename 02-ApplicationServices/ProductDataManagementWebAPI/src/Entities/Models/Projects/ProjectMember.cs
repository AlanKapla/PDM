using Entities.Models.Tenants;

namespace Entities.Models.Projects;

public class ProjectMember
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public Project Project { get; set; } = default!;
    public TenantMember TenantMember { get; set; } = default!;
    public ICollection<ProjectMemberModulePermission> ModulePermissions { get; set; } = new List<ProjectMemberModulePermission>();
}
