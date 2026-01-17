namespace Business.Interfaces.Constants;

public static class PermissionCodes
{
    // TENANT – LIST/CONTEXT
    public const string TenantListAvailable = "TENANT.LIST.AVAILABLE";
    public const string TenantAdminListAvailable = "TENANT.ADMIN.LIST.AVAILABLE";
    
    // ROLE – LIST
    public const string RoleList = "ROLE.LIST";
    
    // TENANT – OPERACJE
    public const string TenantView = "TENANT.VIEW";
    public const string TenantEdit = "TENANT.EDIT";
    public const string TenantMembersManage = "TENANT.MEMBERS.MANAGE";
    public const string TenantStatusManage = "TENANT.STATUS.MANAGE";
    public const string TenantProjectCreate = "TENANT.PROJECT.CREATE";
    
    // PROJECT – PODSTAWOWE
    public const string ProjectView = "PROJECT.VIEW";
    public const string ProjectEdit = "PROJECT.EDIT";
    
    // PROJECT – CZŁONKOWIE
    public const string ProjectMembersView = "PROJECT.MEMBERS.VIEW";
    public const string ProjectMembersManage = "PROJECT.MEMBERS.MANAGE";
    
    // PROJECT – STATUS
    public const string ProjectStatusManage = "PROJECT.STATUS.MANAGE";
    
    // PROJECT – RESOURCES
    public const string ProjectResourcesRead = "PROJECT.RESOURCES.READ";
    public const string ProjectResourcesWrite = "PROJECT.RESOURCES.WRITE";
    public const string ProjectResourcesShare = "PROJECT.RESOURCES.SHARE";
    public const string ProjectResourcesReadShared = "PROJECT.RESOURCES.READ_SHARED";
    public const string ProjectResourcesWriteShared = "PROJECT.RESOURCES.WRITE_SHARED";
    public const string ProjectResourcesReadAll = "PROJECT.RESOURCES.READ_ALL";
    public const string ProjectResourcesWriteAll = "PROJECT.RESOURCES.WRITE_ALL";
    public const string ProjectResourcesReadSingle = "PROJECT.RESOURCES.READ_SINGLE";


    public static readonly string[] All = new[]
    {
        TenantListAvailable,
        TenantAdminListAvailable,
        RoleList,
        TenantView,
        TenantEdit,
        TenantMembersManage,
        TenantStatusManage,
        TenantProjectCreate,
        ProjectView,
        ProjectEdit,
        ProjectMembersView,
        ProjectMembersManage,
        ProjectStatusManage,
        ProjectResourcesRead,
        ProjectResourcesWrite,
        ProjectResourcesShare,
        ProjectResourcesReadShared,
        ProjectResourcesWriteShared,
        ProjectResourcesReadAll,
        ProjectResourcesWriteAll,
        ProjectResourcesReadSingle
    };
}
