using Business.Implementation.Seeders.Models;
using Business.Interfaces.Constants;
using Entities.Enums;

namespace Business.Implementation.Seeders.Data;

public static class RolePermissionSeedData
{
    public static RoleSeed[] GetRoles() => new[]
    {
        // SYSTEM
        new RoleSeed(RoleScope.System, RoleCodes.SystemSuperAdmin, "System SuperAdmin", "Administrator systemowy z dostępem read-only do wszystkich zasobów", IsBuiltIn: true),

        // TENANT
        new RoleSeed(RoleScope.Tenant, RoleCodes.TenantAdmin, "Tenant Admin", "Administrator tenanta", IsBuiltIn: true),
        new RoleSeed(RoleScope.Tenant, RoleCodes.TenantMember, "Tenant Member", "Członek tenanta", IsBuiltIn: true),
    };

    public static PermissionSeed[] GetPermissions() => new[]
    {
        // TENANT – CONTEXT
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantContextList, "List available tenants", "Lista tenantów dostępnych dla usera (do switchera)"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantContextAdminList, "List admin tenants", "Lista tenantów gdzie użytkownik jest adminem"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.RoleList, "List available roles", "Lista dostępnych ról do przypisywania (dla adminów)"),

        // TENANT – SETTINGS
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantSettingsView, "View tenant settings", "Odczyt danych tenanta w aktywnym kontekście"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantSettingsEdit, "Edit tenant settings", "Edycja danych tenanta"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantMembersManage, "Manage tenant members", "Zarządzanie członkami tenanta"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantProjectsCreate, "Create project in tenant", "Tworzenie projektu w tenancie"),

        // PROJECT – MODULES (one permission per module)
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.ProjectSettings, "Project settings", "Dostęp do ustawień projektu"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.ProjectFiles, "Project files", "Dostęp do plików projektu"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.ProjectEstimates, "Project estimates", "Dostęp do kosztorysów projektu"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.ProjectCosts, "Project costs", "Dostęp do wydatków projektu"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.ProjectSchedule, "Project schedule", "Dostęp do harmonogramów projektu"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.ProjectDashboardTracker, "Project dashboard & tracker", "Dostęp do śledzenia kosztów projektu"),
    };

    public static RolePermissionSeed[] GetRolePermissions()
    {
        static RolePermissionSeed RP(string roleCode, string permissionCode)
            => new(roleCode, permissionCode);

        return new[]
        {
            // SYSTEM.SUPERADMIN - READ-ONLY ACCESS TO EVERYTHING
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.TenantContextList),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.TenantContextAdminList),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.RoleList),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.TenantSettingsView),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectSettings),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectFiles),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectEstimates),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectSchedule),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectCosts),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectDashboardTracker),

            // TENANT.ADMIN
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantContextList),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantContextAdminList),
            RP(RoleCodes.TenantAdmin, PermissionCodes.RoleList),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantSettingsView),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantSettingsEdit),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantMembersManage),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantProjectsCreate),

            // TENANT.MEMBER
            RP(RoleCodes.TenantMember, PermissionCodes.TenantContextList),
            RP(RoleCodes.TenantMember, PermissionCodes.TenantSettingsView),
        };
    }
}
