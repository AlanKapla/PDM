using Entities.Models.Base;
using Entities.Models.CostTrackers;

namespace Entities.Models
{
    public class Project : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }
        public Guid CostTrackerId { get; set; }

        public Tenant Tenant { get; set; } = default!;
        public TenantMember CreatedBy { get; set; } = default!;
        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<ProjectGroup> Groups { get; set; } = new List<ProjectGroup>();
        public virtual CostTracker CostTracker { get; set; } = default!;
    }
}
