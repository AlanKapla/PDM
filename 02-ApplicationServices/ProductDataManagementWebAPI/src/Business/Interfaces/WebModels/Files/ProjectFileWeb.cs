namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing project file returned by API
    /// </summary>
    public record ProjectFileWeb
    {
        public Guid Id { get; init; }
        public string FileName { get; init; } = default!;
        public string DisplayName { get; init; } = default!;
        public string PackageName { get; init; } = default!;
        public string ContentType { get; init; } = default!;
        public long FileSizeBytes { get; init; }
        public DateTime UploadedAt { get; init; }
        public Guid UploadedByUserId { get; init; }
        public string UploadedByUserName { get; init; } = default!;
        
        /// <summary>
        /// Temporary URL with SAS token for direct file access
        /// </summary>
        public string SasUrl { get; init; } = default!;
    }
}
