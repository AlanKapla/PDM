using Entities.Models.Base;

namespace Entities.Models.Costs
{
    public class BaseCostAttachment : DeletableEntity
    {
        public Guid CostId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public string OriginalFileName { get; set; } = default!;
        public string BlobName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual BaseCost Cost { get; set; } = default!;
    }
}
