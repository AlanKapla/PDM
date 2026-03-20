using Entities.Models.Base;

namespace Entities.Models.CostEstimates
{
    public class SharedCostEstimate : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid CostEstimateId { get; set; }
        public Guid SharedByUserId { get; set; }
        public Guid SharedWithUserId { get; set; }
        public DateTime SharedAt { get; set; }

        public CostEstimate CostEstimate { get; set; } = default!;
        public User SharedByUser { get; set; } = default!;
        public User SharedWithUser { get; set; } = default!;
        public TenantMember SharedByTenantMember { get; set; } = default!;
        public TenantMember SharedWithTenantMember { get; set; } = default!;
        public ProjectMember SharedByProjectMember { get; set; } = default!;
        public ProjectMember SharedWithProjectMember { get; set; } = default!;
    }
}
