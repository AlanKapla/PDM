using Entities.Models.Base;

namespace Entities.Models
{
    public class WorkScheduleStageWorkComment : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WorkScheduleStageWorkId { get; set; }
        public string Content { get; set; } = default!;
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public WorkScheduleStageWork Work { get; set; } = default!;
        public User CreatedBy { get; set; } = default!;
    }
}
