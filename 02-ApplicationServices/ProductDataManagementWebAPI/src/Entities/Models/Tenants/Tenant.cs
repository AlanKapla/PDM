using Entities.Models.Base;
using Entities.Models.Projects;

namespace Entities.Models.Tenants
{
    public class Tenant : BaseEntity
    {
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TenantMember> Members { get; set; } = new List<TenantMember>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
