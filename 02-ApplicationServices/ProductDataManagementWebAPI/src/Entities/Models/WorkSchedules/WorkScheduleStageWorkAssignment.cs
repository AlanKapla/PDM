using Entities.Models.Base;
using Entities.Models.Projects;
using Entities.Models.Tenants;

namespace Entities.Models.WorkSchedules
{
    /// <summary>
    /// PK: Id. Exactly one of UserId / ContractorId must be set (XOR).
    /// </summary>
    public class WorkScheduleStageWorkAssignment : BaseEntity
    {
        public Guid WorkScheduleStageWorkId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ContractorId { get; set; }

        public WorkScheduleStageWork Work { get; set; } = default!;
        public ProjectMember? ProjectMember { get; set; }
        public Tenant Tenant { get; set; } = default!;
        public Project Project { get; set; } = default!;
        public TenantMember? TenantMember { get; set; }
        public Contractor? Contractor { get; set; }
    }
}
