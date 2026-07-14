using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models.AI
{
    public class AICostImportBatch : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public CostDocumentType CostDocumentType { get; set; }
        public string? TrackedCostContextJson { get; set; }
        public AICostImportBatchStatus Status { get; set; }
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public int PendingCount { get; set; }
        public int ErrorCount { get; set; }
        public int DuplicateCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        public virtual ICollection<AICostImportItem> Items { get; set; } = new List<AICostImportItem>();
    }
}
