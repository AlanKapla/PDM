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

        // PROJECT
        new RoleSeed(RoleScope.Project, RoleCodes.ProjectAdmin, "Project Admin", "Administrator projektu - pełny dostęp do wszystkich zasobów", IsBuiltIn: true),
        new RoleSeed(RoleScope.Project, RoleCodes.ProjectEditor, "Project Editor", "Edytor projektu - może tworzyć i edytować własne zasoby oraz przeglądać udostępnione", IsBuiltIn: true),
        new RoleSeed(RoleScope.Project, RoleCodes.ProjectViewer, "Project Viewer", "Przeglądający projekt - może tylko przeglądać udostępnione zasoby", IsBuiltIn: true),
    };

    public static PermissionSeed[] GetPermissions() => new[]
    {
        // GLOBAL
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantListAvailable, "List available tenants", "Lista tenantów dostępnych dla usera (do switchera)"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantAdminListAvailable, "List admin tenants", "Lista tenantów gdzie użytkownik jest adminem"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.RoleList, "List available roles", "Lista dostępnych ról do przypisywania (dla adminów)"),

        // TENANT – OPERACJE
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantView, "View tenant", "Odczyt danych tenanta w aktywnym kontekście"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantEdit, "Edit tenant", "Edycja danych tenanta"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantMembersManage, "Manage tenant members", "Zarządzanie członkami tenanta"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantStatusManage, "Manage tenant status", "Aktywacja/dezaktywacja tenanta"),
        new PermissionSeed(RoleScope.Tenant, PermissionCodes.TenantProjectCreate, "Create project in tenant", "Tworzenie projektu w tenancie"),

        // PROJECT – PODSTAWOWE
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectView, "View project", "Odczyt danych projektu"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectEdit, "Edit project", "Edycja danych projektu"),

        // PROJECT – CZŁONKOWIE
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectMembersView, "View project members", "Odczyt listy członków projektu"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectMembersManage, "Manage project members", "Zarządzanie członkami projektu"),

        // PROJECT – STATUS
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectStatusManage, "Manage project status", "Aktywacja/dezaktywacja projektu (wyjątek: bez ActiveTenantId)"),

        // PROJECT – ZASOBY (własne i udostępnione)
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesRead, "Read project resources", "Odczyt własnych zasobów projektu"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesWrite, "Write project resources", "Zapis własnych zasobów projektu"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesShare, "Share project resources", "Udostępnianie zasobów projektu innym członkom"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesReadShared, "Read shared resources", "Odczyt zasobów udostępnionych"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesWriteShared, "Write shared resources", "Edycja zasobów udostępnionych"),
        
        // PROJECT – ZASOBY (wszystkie - tylko dla ProjectAdmin)
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesReadAll, "Read all project resources", "Odczyt wszystkich zasobów projektu (także nieudostępnionych)"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesWriteAll, "Write all project resources", "Edycja wszystkich zasobów projektu (także nieudostępnionych)"),
        
        // PROJECT – ZASOBY (pojedynczy obiekt)
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesReadSingle, "Read single resource details", "Odczyt szczegółów pojedynczego zasobu"),

        // PROJECT – ZASOBY (własne - dla przypisanych)
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesWriteOwn, "Write own assigned resources", "Zapis zasobów do których użytkownik jest bezpośrednio przypisany (cross-tenant)"),
    };

    public static RolePermissionSeed[] GetRolePermissions()
    {
        static RolePermissionSeed RP(string roleCode, string permissionCode)
            => new(roleCode, permissionCode);

        return new[]
        {
            // SYSTEM.SUPERADMIN - READ-ONLY ACCESS TO EVERYTHING
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.TenantListAvailable),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.TenantAdminListAvailable),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.RoleList),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.TenantView),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectView),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectResourcesRead),
            RP(RoleCodes.SystemSuperAdmin, PermissionCodes.ProjectResourcesReadSingle),

            // TENANT.ADMIN
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantListAvailable),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantAdminListAvailable),
            RP(RoleCodes.TenantAdmin, PermissionCodes.RoleList),  // ✅ NEW: Admin może listować role
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantView),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantEdit),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantMembersManage),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantStatusManage),
            RP(RoleCodes.TenantAdmin, PermissionCodes.TenantProjectCreate),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectView),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectEdit),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectMembersManage),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectStatusManage),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesRead),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesWrite),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesShare),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesReadShared),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesWriteShared),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesReadAll),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesWriteAll),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesReadSingle),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesWriteOwn),

            // TENANT.MEMBER
            RP(RoleCodes.TenantMember, PermissionCodes.TenantListAvailable),
            RP(RoleCodes.TenantMember, PermissionCodes.TenantView),

            // PROJECT.ADMIN - wszystkie uprawnienia projektowe (w tym READ_ALL, WRITE_ALL, SHARE i READ_SINGLE)
            RP(RoleCodes.ProjectAdmin, PermissionCodes.RoleList),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectView),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectEdit),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectMembersManage),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectStatusManage),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesRead),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesWrite),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesShare),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesReadShared),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesWriteShared),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesReadAll),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesWriteAll),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesReadSingle),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesWriteOwn),

            // PROJECT.EDITOR - read/write własnych i shared oraz SHARE i READ_SINGLE
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectView),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesRead),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesWrite),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesShare),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesReadShared),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesWriteShared),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesReadSingle),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesWriteOwn),

            // PROJECT.VIEWER - tylko read shared i READ_SINGLE
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectView),
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectResourcesReadShared),
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectResourcesWriteShared),
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectResourcesReadSingle),
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectResourcesWriteOwn),
        };
    }
}
