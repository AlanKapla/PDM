using Entities.Models.Projects;
using Entities.Models.Users;

namespace Entities.Models.Tenants
{
    public class TenantMember
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Tenant Tenant { get; set; } = default!;
        public User User { get; set; } = default!;
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }
}
