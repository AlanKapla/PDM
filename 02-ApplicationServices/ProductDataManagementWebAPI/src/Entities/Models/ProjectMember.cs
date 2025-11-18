using Entities.Enums;

namespace Entities.Models
{
    public class ProjectMember
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }

        public ProjectRole Role { get; set; } = ProjectRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public Project Project { get; set; } = default!;
        public TenantMember TenantMember { get; set; } = default!;
        public ICollection<ProjectGroupMember> ProjectGroupMembers { get; set; } = new List<ProjectGroupMember>();
    }
}