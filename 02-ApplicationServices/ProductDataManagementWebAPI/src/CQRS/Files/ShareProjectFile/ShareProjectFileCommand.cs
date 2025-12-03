namespace CQRS.Files.ShareProjectFile
{
    /// <summary>
    /// Command do udostępnienia plików innemu członkowi projektu
    /// </summary>
    public record ShareProjectFileCommand : IRequestCommand<ShareProjectFileResult>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public List<Guid> ProjectFileIds { get; init; } = new();
        public Guid SharedWithUserId { get; init; }
    }

    /// <summary>
    /// Wynik operacji udostępniania plików
    /// </summary>
    public record ShareProjectFileResult
    {
        public List<Guid> SharedFileIds { get; init; } = new();
        public int SuccessCount { get; init; }
        public int FailedCount { get; init; }
        public List<string> Errors { get; init; } = new();
    }
}
