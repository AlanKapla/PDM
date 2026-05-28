namespace Business.Interfaces.Constants;

public static class PermissionCodes
{
    // TENANT – CONTEXT
    public const string TenantContextList = "TENANT.CONTEXT.LIST";
    public const string TenantContextAdminList = "TENANT.CONTEXT.ADMIN_LIST";

    // TENANT – BASE ACCESS
    public const string TenantView = "TENANT.VIEW";

    // TENANT – SETTINGS
    public const string TenantSettingsView = "TENANT.SETTINGS.VIEW";
    public const string TenantSettingsEdit = "TENANT.SETTINGS.EDIT";
    public const string TenantMembersManage = "TENANT.MEMBERS.MANAGE";
    public const string TenantProjectsCreate = "TENANT.PROJECTS.CREATE";

    // PROJECT – BASE ACCESS
    public const string ProjectView = "PROJECT.VIEW";

    // PROJECT – MODULES (one permission per module)
    public const string ProjectSettings = "PROJECT.SETTINGS";
    public const string ProjectMembers = "PROJECT.MEMBERS";
    public const string ProjectFiles = "PROJECT.FILES";
    public const string ProjectEstimates = "PROJECT.ESTIMATES";
    public const string ProjectCosts = "PROJECT.COSTS";
    public const string ProjectSchedule = "PROJECT.SCHEDULE";
    public const string ProjectDashboardTracker = "PROJECT.DASHBOARD_TRACKER";

    public static readonly string[] All = new[]
    {
        TenantContextList, TenantContextAdminList,
        TenantView,
        TenantSettingsView, TenantSettingsEdit, TenantMembersManage, TenantProjectsCreate,
        ProjectView,
        ProjectSettings, ProjectMembers, ProjectFiles, ProjectEstimates,
        ProjectCosts, ProjectSchedule, ProjectDashboardTracker
    };
}
