using Entities.Models.Base;

namespace Entities.Models.CostTrackers
{
    public class TrackedCostAttachment : BaseEntity
    {
        public Guid TrackedCostId { get; set; }
        public string OriginalFileName { get; set; } = default!;
        public string BlobName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual TrackedCost TrackedCost { get; set; } = default!;
    }
}
