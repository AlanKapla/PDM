using Entities.Models.Base;

namespace Entities.Models
{
    public class WorkScheduleStageWorkPeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsClosed { get; set; } = false;
    }
}
