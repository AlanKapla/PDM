namespace Business.Interfaces.Constants;

public static class RoleCodes
{
    // SYSTEM
    public const string SystemSuperAdmin = "SYSTEM.SUPERADMIN";
    
    // TENANT
    public const string TenantAdmin = "TENANT.ADMIN";
    public const string TenantMember = "TENANT.MEMBER";
    
    // PROJECT
    public const string ProjectAdmin = "PROJECT.ADMIN";
    public const string ProjectEditor = "PROJECT.EDITOR";
    public const string ProjectViewer = "PROJECT.VIEWER";
    
    public static readonly string[] All = new[]
    {
        TenantAdmin,
        TenantMember,
        ProjectAdmin,
        ProjectEditor,
        ProjectViewer
        // NOTE: SystemSuperAdmin is NOT included - it's a system-level role, not assignable
    };
}
