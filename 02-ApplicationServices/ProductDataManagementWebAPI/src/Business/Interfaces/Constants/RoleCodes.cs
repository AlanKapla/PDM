namespace Business.Interfaces.Constants;

public static class RoleCodes
{
    // TENANT
    public const string TenantAdmin = "TENANT.ADMIN";
    public const string TenantMember = "TENANT.MEMBER";
    
    // PROJECT
    public const string ProjectAdmin = "PROJECT.ADMIN";
    public const string ProjectEditor = "PROJECT.EDITOR";
    public const string ProjectCollaborator = "PROJECT.COLLABORATOR";
    public const string ProjectViewer = "PROJECT.VIEWER";
    public const string ProjectMember = "PROJECT.MEMBER";
    
    public static readonly string[] All = new[]
    {
        TenantAdmin,
        TenantMember,
        ProjectAdmin,
        ProjectEditor,
        ProjectCollaborator,
        ProjectViewer,
        ProjectMember
    };
}
