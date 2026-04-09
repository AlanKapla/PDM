using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models.CostTrackers
{
    public class CostTracker : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }

        public virtual CostEstimate? CostEstimate { get; set; }
        public virtual ICollection<TrackedCost> TrackedCosts { get; set; } = new List<TrackedCost>();
    }
}
