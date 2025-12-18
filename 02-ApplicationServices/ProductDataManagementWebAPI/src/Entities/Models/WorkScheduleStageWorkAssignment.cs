namespace Entities.Models
{
    public class WorkScheduleStageWorkAssignment
    {
        public Guid WorkScheduleStageWorkId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }

        public WorkScheduleStageWork Work { get; set; } = default!;
        public ProjectMember ProjectMember { get; set; } = default!;
    }
}
