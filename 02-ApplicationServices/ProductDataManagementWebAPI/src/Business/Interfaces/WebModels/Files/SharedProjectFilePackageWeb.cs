namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing a shared package with its files
    /// </summary>
    public record SharedProjectFilePackageWeb
    {
        public Guid PackageId { get; init; }
        public string PackageName { get; init; } = default!;
        public Guid PackageOwnerId { get; init; }
        public string PackageOwnerName { get; init; } = default!;
        
        /// <summary>
        /// Shared files from this package
        /// </summary>
        public List<SharedProjectFileWeb> Files { get; init; } = new();
        
        /// <summary>
        /// Total number of shared files in this package
        /// </summary>
        public int TotalSharedFiles { get; init; }
    }
}
