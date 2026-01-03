using Business.Implementation.Seeders.Models;
using Business.Interfaces.Constants;
using Entities.Enums;

namespace Business.Implementation.Seeders.Data;

public static class RolePermissionSeedData
{
    public static RoleSeed[] GetRoles() => new[]
    {
        // TENANT
        new RoleSeed(RoleScope.Tenant, RoleCodes.TenantAdmin, "Tenant Admin", "Administrator tenanta", IsBuiltIn: true),
        new RoleSeed(RoleScope.Tenant, RoleCodes.TenantMember, "Tenant Member", "Członek tenanta", IsBuiltIn: true),

        // PROJECT
        new RoleSeed(RoleScope.Project, RoleCodes.ProjectAdmin, "Project Admin", "Administrator projektu", IsBuiltIn: true),
        new RoleSeed(RoleScope.Project, RoleCodes.ProjectEditor, "Project Editor", "Edytor projektu - pełny dostęp do zasobów", IsBuiltIn: true),
        new RoleSeed(RoleScope.Project, RoleCodes.ProjectCollaborator, "Project Collaborator", "Współpracownik - może przeglądać i edytować udostępnione zasoby", IsBuiltIn: true),
        new RoleSeed(RoleScope.Project, RoleCodes.ProjectViewer, "Project Viewer", "Przeglądający projekt - może tylko przeglądać udostępnione zasoby", IsBuiltIn: true),
        new RoleSeed(RoleScope.Project, RoleCodes.ProjectMember, "Project Member", "Członek projektu bez dostępu do danych", IsBuiltIn: true),
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

        // PROJECT – ZASOBY
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesRead, "Read project resources", "Odczyt własnych zasobów projektu"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesWrite, "Write project resources", "Zapis własnych zasobów projektu"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesReadShared, "Read shared resources", "Odczyt zasobów udostępnionych"),
        new PermissionSeed(RoleScope.Project, PermissionCodes.ProjectResourcesWriteShared, "Write shared resources", "Edycja zasobów udostępnionych (Collaborator)"),
    };

    public static RolePermissionSeed[] GetRolePermissions()
    {
        static RolePermissionSeed RP(string roleCode, string permissionCode)
            => new(roleCode, permissionCode);

        return new[]
        {
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
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesReadShared),
            RP(RoleCodes.TenantAdmin, PermissionCodes.ProjectResourcesWriteShared),

            // TENANT.MEMBER
            RP(RoleCodes.TenantMember, PermissionCodes.TenantListAvailable),
            RP(RoleCodes.TenantMember, PermissionCodes.TenantView),

            // PROJECT.ADMIN
            RP(RoleCodes.ProjectAdmin, PermissionCodes.RoleList),  // ✅ NEW: Project Admin może listować role projektowe
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectView),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectEdit),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectMembersManage),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesRead),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesWrite),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesReadShared),
            RP(RoleCodes.ProjectAdmin, PermissionCodes.ProjectResourcesWriteShared),

            // PROJECT.EDITOR - może tworzyć i edytować własne zasoby
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectView),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesRead),
            RP(RoleCodes.ProjectEditor, PermissionCodes.ProjectResourcesWrite),

            // PROJECT.COLLABORATOR - może przeglądać i edytować udostępnione zasoby
            RP(RoleCodes.ProjectCollaborator, PermissionCodes.ProjectView),
            RP(RoleCodes.ProjectCollaborator, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.ProjectCollaborator, PermissionCodes.ProjectResourcesReadShared),
            RP(RoleCodes.ProjectCollaborator, PermissionCodes.ProjectResourcesWriteShared),

            // PROJECT.VIEWER - może tylko przeglądać udostępnione zasoby
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectView),
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectMembersView),
            RP(RoleCodes.ProjectViewer, PermissionCodes.ProjectResourcesReadShared),

            // PROJECT.MEMBER - tylko odczyt projektu
            RP(RoleCodes.ProjectMember, PermissionCodes.ProjectView),
        };
    }
}
