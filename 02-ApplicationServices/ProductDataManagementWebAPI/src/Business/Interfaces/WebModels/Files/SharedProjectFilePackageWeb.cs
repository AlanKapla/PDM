namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing a shared package with its files
    /// </summary>
    public sealed record SharedProjectFilePackageWeb
    {
        public required Guid PackageId { get; init; }
        public required string PackageName { get; init; }
        public required Guid PackageOwnerId { get; init; }
        public required string PackageOwnerName { get; init; }

        /// <summary>
        /// Shared files from this package
        /// </summary>
        public List<SharedProjectFileWeb> Files { get; init; } = new();

        /// <summary>
        /// Total number of shared files in this package
        /// </summary>
        public required int TotalSharedFiles { get; init; }
    }
}
