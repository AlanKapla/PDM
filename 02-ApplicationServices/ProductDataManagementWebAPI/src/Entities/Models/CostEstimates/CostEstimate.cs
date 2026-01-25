using Entities.Models.Base;
using Entities.Models.CostEstimateTemplates;

namespace Entities.Models.CostEstimates
{
    public class CostEstimate : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid TemplateId { get; set; }
        public Guid TemplateVersionId { get; set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public CostEstimateStatus Status { get; set; }
        public Guid SelectedCurrencyId { get; set; }
        public decimal? TotalNet { get; set; }
        public decimal? TotalGross { get; set; }
        public decimal? TotalVat { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastCalculatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        public virtual Tenant Tenant { get; set; } = default!;
        public virtual Project Project { get; set; } = default!;
        public virtual CostEstimateTemplate Template { get; set; } = default!;
        public virtual CostEstimateTemplateVersion TemplateVersion { get; set; } = default!;
        public virtual User Owner { get; set; } = default!;
        public virtual CostEstimateTemplateCurrency SelectedCurrency { get; set; } = default!;
        public virtual ICollection<CostEstimateGroup> AllGroups { get; set; } = new List<CostEstimateGroup>();
        public virtual ICollection<CostEstimateItem> AllItems { get; set; } = new List<CostEstimateItem>();
        
        public IEnumerable<CostEstimateGroup> RootGroups => AllGroups?.Where(g => g.ParentGroupId == null) ?? Enumerable.Empty<CostEstimateGroup>();
    }
}
