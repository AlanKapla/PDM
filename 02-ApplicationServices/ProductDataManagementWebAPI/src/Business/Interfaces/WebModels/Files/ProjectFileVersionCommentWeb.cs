namespace Business.Interfaces.WebModels.Files
{
    public record ProjectFileVersionCommentWeb
    {
        public Guid Id { get; init; }
        public Guid ProjectFileVersionId { get; init; }
        public Guid UserId { get; init; }
        public string UserName { get; init; } = default!;
        public string Content { get; init; } = default!;
        public DateTime CreatedAt { get; init; }
        public DateTime? EditedAt { get; init; }
        public bool IsEdited { get; init; }
        public bool CanEdit { get; init; }
        public bool CanDelete { get; init; }
    }
}
