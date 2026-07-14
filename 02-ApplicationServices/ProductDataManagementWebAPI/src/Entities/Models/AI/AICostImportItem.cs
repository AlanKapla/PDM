using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models.AI
{
    public class AICostImportItem : BaseEntity
    {
        public Guid BatchId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public AICostImportItemStatus Status { get; set; }
        public string OriginalFileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long FileSizeBytes { get; set; }
        public string FileHashSha256 { get; set; } = default!;
        public string BlobPath { get; set; } = default!;
        public string? ParsedDataJson { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }
        public DateTimeOffset? AnalyzedAt { get; set; }
        public Guid? AcceptedCostId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public virtual AICostImportBatch Batch { get; set; } = default!;
    }
}
