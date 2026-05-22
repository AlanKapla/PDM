namespace Business.Interfaces.WebModels.Files
{
    public sealed record ProjectFileVersionCommentWeb
    {
        public required Guid Id { get; init; }
        public required Guid ProjectFileVersionId { get; init; }
        public required Guid UserId { get; init; }
        public required string UserName { get; init; }
        public required string Content { get; init; }
        public required DateTime CreatedAt { get; init; }
        public DateTime? EditedAt { get; init; }
        public required bool IsEdited { get; init; }
        public required bool CanEdit { get; init; }
        public required bool CanDelete { get; init; }
    }
}
