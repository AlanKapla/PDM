namespace Entities.Models
{
    public class ProjectGroupMember
    {
        public Guid ProjectGroupId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }

        public ProjectGroup ProjectGroup { get; set; } = default!;
        public ProjectMember ProjectMember { get; set; } = default!;
    }
}
