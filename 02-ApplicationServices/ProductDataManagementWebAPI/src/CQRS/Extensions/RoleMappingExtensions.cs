using Business.Interfaces.Constants;

namespace CQRS.Extensions;

/// <summary>
/// Extension methods for role code checks - no longer mapping to enums
/// </summary>
public static class RoleCodeExtensions
{
    public static bool IsTenantAdmin(this string? roleCode)
    {
        return roleCode == RoleCodes.TenantAdmin;
    }

    public static bool IsProjectAdmin(this string? roleCode)
    {
        return roleCode == RoleCodes.ProjectAdmin;
    }

    public static bool IsProjectEditor(this string? roleCode)
    {
        return roleCode == RoleCodes.ProjectEditor;
    }

    public static bool IsProjectCollaborator(this string? roleCode)
    {
        return roleCode == RoleCodes.ProjectCollaborator;
    }

    public static bool IsProjectViewer(this string? roleCode)
    {
        return roleCode == RoleCodes.ProjectViewer;
    }

    public static bool IsProjectAdminOrEditor(this string? roleCode)
    {
        return roleCode == RoleCodes.ProjectAdmin || roleCode == RoleCodes.ProjectEditor;
    }
}
