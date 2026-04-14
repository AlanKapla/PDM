namespace Business.Interfaces.WebModels.CostTrackers
{
    public record TrackedCostWeb
    {
        public required Guid Id { get; init; }
        public required Guid TrackerId { get; init; }
        public Guid? CostEstimateId { get; init; }
        public Guid? CostEstimateItemId { get; init; }
        public required bool IsAdditional { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public string? Contractor { get; init; }
        public DateTime? Date { get; init; }
        public required DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public required List<TrackedCostAttachmentWeb> Attachments { get; init; }
    }
}
