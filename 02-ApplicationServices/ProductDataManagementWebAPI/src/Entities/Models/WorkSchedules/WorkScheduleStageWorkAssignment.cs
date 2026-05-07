using Entities.Models.Projects;
using Entities.Models.Tenants;

namespace Entities.Models.WorkSchedules
{
    /// <summary>
    /// Composite PK: (WorkScheduleStageWorkId, UserId) — wymaga konfiguracji w OnModelCreating.
    /// </summary>
    public class WorkScheduleStageWorkAssignment
    {
        public Guid WorkScheduleStageWorkId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }

        public WorkScheduleStageWork Work { get; set; } = default!;
        public ProjectMember ProjectMember { get; set; } = default!;
        public Tenant Tenant { get; set; } = default!;
        public Project Project { get; set; } = default!;
        public TenantMember TenantMember { get; set; } = default!;
    }
}
