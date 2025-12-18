namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing a file package with its files
    /// </summary>
    public record ProjectFilePackageWeb
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = default!;
        public DateTime CreatedAt { get; init; }
        public Guid OwnerId { get; init; }
        public string OwnerName { get; init; } = default!;
        
        /// <summary>
        /// Files belonging to this package
        /// </summary>
        public List<ProjectFileWeb> Files { get; init; } = new();
        
        /// <summary>
        /// Total number of files in the package
        /// </summary>
        public int TotalFiles { get; init; }
    }
}
