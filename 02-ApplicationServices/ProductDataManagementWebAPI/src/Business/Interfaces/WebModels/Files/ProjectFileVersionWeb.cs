namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing a specific version of a project file
    /// </summary>
    public record ProjectFileVersionWeb
    {
        public Guid Id { get; init; }
        public Guid ProjectFileId { get; init; }
        public int VersionNumber { get; init; }
        public string ContentType { get; init; } = default!;
        public long FileSizeBytes { get; init; }
        public DateTime CreatedAt { get; init; }
        public Guid CreatedByUserId { get; init; }
        public string CreatedByUserName { get; init; } = default!;
        
        /// <summary>
        /// Temporary URL with SAS token for viewing/displaying file (inline)
        /// </summary>
        public string SasUrlView { get; init; } = default!;
        
        /// <summary>
        /// Temporary URL with SAS token for downloading file (attachment)
        /// </summary>
        public string SasUrlDownload { get; init; } = default!;
        
        public List<ProjectFileVersionCommentWeb> Comments { get; init; } = new();
    }
}
