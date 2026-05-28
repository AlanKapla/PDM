namespace Business.Interfaces.Constants;

public static class RoleCodes
{
    // SYSTEM
    public const string SystemSuperAdmin = "SYSTEM.SUPERADMIN";
    
    // TENANT
    public const string TenantAdmin = "TENANT.ADMIN";
    public const string TenantMember = "TENANT.MEMBER";
    
    public static readonly string[] All = new[]
    {
        TenantAdmin,
        TenantMember
        // NOTE: SystemSuperAdmin is NOT included - it's a system-level role, not assignable
    };
}
