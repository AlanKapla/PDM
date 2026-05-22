using Entities.Enums;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Users;

namespace Entities.Models.Tenants
{
    public class TenantMember
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public Guid? RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Tenant Tenant { get; set; } = default!;
        public User User { get; set; } = default!;
        public Role? MemberRole { get; set; }
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }
}
