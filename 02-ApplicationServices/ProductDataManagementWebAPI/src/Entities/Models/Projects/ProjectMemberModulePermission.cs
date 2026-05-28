using Entities.Enums;

namespace Entities.Models.Projects;

public class ProjectMemberModulePermission
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectModule Module { get; set; }

    public ProjectMember ProjectMember { get; set; } = default!;
}
