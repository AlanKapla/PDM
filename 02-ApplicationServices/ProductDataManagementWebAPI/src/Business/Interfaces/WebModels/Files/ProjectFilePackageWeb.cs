namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing a file package with its files
    /// </summary>
    public sealed record ProjectFilePackageWeb
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required Guid OwnerId { get; init; }
        public required string OwnerName { get; init; }

        /// <summary>
        /// Files belonging to this package
        /// </summary>
        public List<ProjectFileWeb> Files { get; init; } = new();

        /// <summary>
        /// Total number of files in the package
        /// </summary>
        public required int TotalFiles { get; init; }
    }
}
