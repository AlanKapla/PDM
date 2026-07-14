namespace Business.Interfaces.WebModels.AI
{
    public sealed record TrackedCostContextDto
    {
        public Guid? CostEstimateItemId { get; init; }
        public Guid? WorkScheduleStageWorkId { get; init; }
    }

    public sealed record PendingAICostImportCountWeb
    {
        public required int PendingCount { get; init; }
        public required int ErrorCount { get; init; }
        public required int DuplicateCount { get; init; }
    }

    public sealed record AICostImportBatchWeb
    {
        public required Guid Id { get; init; }
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required string Status { get; init; }
        public required int TotalFiles { get; init; }
        public required int ProcessedFiles { get; init; }
        public required int PendingCount { get; init; }
        public required int ErrorCount { get; init; }
        public required int DuplicateCount { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
    }

    public sealed record AICostImportItemWeb
    {
        public required Guid Id { get; init; }
        public required Guid BatchId { get; init; }
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required string Status { get; init; }
        public required string OriginalFileName { get; init; }
        public required string ContentType { get; init; }
        public required long FileSizeBytes { get; init; }
        public ParsedCostDto? ParsedData { get; init; }
        public string? LastError { get; init; }
        public DateTimeOffset? AnalyzedAt { get; init; }
        public string? PreviewUrl { get; init; }
        public required string CostDocumentType { get; init; }
        public TrackedCostContextDto? TrackedCostContext { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
    }

    public sealed record AICostImportSubmitResultWeb
    {
        public required Guid BatchId { get; init; }
        public required int TotalFiles { get; init; }
        public required string Message { get; init; }
    }

    public sealed record AICostImportAcceptAllResultWeb
    {
        public required int AcceptedCount { get; init; }
        public required int FailedCount { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = [];
    }
}
