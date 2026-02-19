namespace Business.Interfaces.WebModels.ProjectCosts;

/// <summary>
/// Response for AI-based project cost extraction from files
/// </summary>
public sealed record ExtractProjectCostsFromFilesResponseWeb
{
    /// <summary>
    /// Successfully created project cost IDs
    /// </summary>
    public List<Guid> CreatedProjectCostIds { get; init; } = new();
    
    /// <summary>
    /// Errors that occurred during processing
    /// </summary>
    public List<FileProcessingErrorWeb> Errors { get; init; } = new();
    
    /// <summary>
    /// Total number of files processed
    /// </summary>
    public int TotalFilesProcessed { get; init; }
    
    /// <summary>
    /// Number of successfully processed files
    /// </summary>
    public int SuccessCount { get; init; }
    
    /// <summary>
    /// Number of failed files
    /// </summary>
    public int ErrorCount { get; init; }
}

/// <summary>
/// Error details for a single file processing failure
/// </summary>
public sealed record FileProcessingErrorWeb
{
    /// <summary>
    /// Name of the file that failed
    /// </summary>
    public string FileName { get; init; } = default!;
    
    /// <summary>
    /// Error message describing what went wrong
    /// </summary>
    public string ErrorMessage { get; init; } = default!;
    
    /// <summary>
    /// Error type/category
    /// </summary>
    public string ErrorType { get; init; } = default!;
}
