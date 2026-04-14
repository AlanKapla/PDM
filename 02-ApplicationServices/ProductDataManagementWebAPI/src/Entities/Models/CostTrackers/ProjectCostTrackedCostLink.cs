using Entities.Models.Base;

namespace Entities.Models.CostTrackers
{
    public class ProjectCostTrackedCostLink : BaseEntity
    {
        public Guid ProjectCostId { get; set; }
        public Guid TrackedCostId { get; set; }
        public DateTime LinkedAt { get; set; }

        public ProjectCost ProjectCost { get; set; } = default!;
        public TrackedCost TrackedCost { get; set; } = default!;
    }
}
