namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing a specific version of a project file
    /// </summary>
    public sealed record ProjectFileVersionWeb
    {
        public required Guid Id { get; init; }
        public required Guid ProjectFileId { get; init; }
        public required int VersionNumber { get; init; }
        public required string ContentType { get; init; }
        public required long FileSizeBytes { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required Guid CreatedByUserId { get; init; }
        public required string CreatedByUserName { get; init; }

        /// <summary>
        /// Temporary URL with SAS token for viewing/displaying file (inline)
        /// </summary>
        public required string SasUrlView { get; init; }

        /// <summary>
        /// Temporary URL with SAS token for downloading file (attachment)
        /// </summary>
        public required string SasUrlDownload { get; init; }

        public List<ProjectFileVersionCommentWeb> Comments { get; init; } = new();
    }
}
