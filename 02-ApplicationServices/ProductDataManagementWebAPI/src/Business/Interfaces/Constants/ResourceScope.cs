namespace Business.Interfaces.Constants;

/// <summary>
/// Defines the scope of resources to retrieve
/// </summary>
public enum ResourceScope
{
    /// <summary>
    /// All resources in the project (requires READ_ALL permission)
    /// </summary>
    All = 0,
    
    /// <summary>
    /// Only resources owned by the current user (requires READ permission)
    /// </summary>
    Mine = 1,
    
    /// <summary>
    /// Only resources shared with the current user (requires READ_SHARED permission)
    /// </summary>
    Shared = 2,

    /// <summary>
    /// Only resources pending approval (requires admin role)
    /// </summary>
    PendingApproval = 3
}
