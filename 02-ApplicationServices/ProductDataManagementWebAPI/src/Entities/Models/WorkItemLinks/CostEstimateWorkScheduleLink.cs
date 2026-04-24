using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models.WorkItemLinks
{
    /// <summary>Łącznik główny między kosztorysem a harmonogramem.</summary>
    public class CostEstimateWorkScheduleLink : BaseEntity
    {
        /// <summary>FK do CostEstimate. Nullable — link może istnieć tylko po stronie harmonogramu.</summary>
        public Guid? CostEstimateId { get; set; }

        /// <summary>FK do WorkSchedule. Nullable — link może istnieć tylko po stronie kosztorysu.</summary>
        public Guid? WorkScheduleId { get; set; }

        public virtual CostEstimate? CostEstimate { get; set; }
        public virtual WorkSchedule? WorkSchedule { get; set; }

        /// <summary>Kolekcja łączników grup i etapów powiązanych z tym linkiem.</summary>
        public virtual ICollection<CostEstimateGroupWorkScheduleStageLink> GroupStageLinks { get; set; }
            = new List<CostEstimateGroupWorkScheduleStageLink>();
    }
}
