using Entities.Models.Base;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.WorkSchedules;

namespace Entities.Models.Costs
{
    public abstract class BaseCost : DeletableEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = default!;
        public string? Number { get; set; }
        public string? Description { get; set; }
        public decimal? Net { get; set; }
        public decimal? Gross { get; set; }
        public Guid? ContractorId { get; set; }
        public DateTime? Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CostEstimateItemId { get; set; }
        public Guid? WorkScheduleStageWorkId { get; set; }
        public Guid? CategoryId { get; set; }

        public virtual Contractor? Contractor { get; set; }
        public virtual ProjectCostCategory? Category { get; set; }
        public virtual CostEstimateItem? CostEstimateItem { get; set; }
        public virtual WorkScheduleStageWork? WorkScheduleStageWork { get; set; }
        public virtual Project Project { get; set; } = default!;
        public virtual Tenant Tenant { get; set; } = default!;
        public virtual ICollection<BaseCostAttachment> Attachments { get; set; } = new List<BaseCostAttachment>();
    }
}
