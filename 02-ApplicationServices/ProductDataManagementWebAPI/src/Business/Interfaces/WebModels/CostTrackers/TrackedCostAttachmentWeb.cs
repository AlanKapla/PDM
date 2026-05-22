namespace Business.Interfaces.WebModels.CostTrackers
{
    public sealed record TrackedCostAttachmentWeb
    {
        public required Guid Id { get; init; }
        public required string OriginalFileName { get; init; }
        public required string FileUrl { get; init; }
        public required string ContentType { get; init; }
        public required long FileSize { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
