namespace Business.Interfaces.Model;

public record TenantCtxSnapshot(
    Guid TenantId,
    Guid TenantRoleId,
    HashSet<string> TenantPermissionCodes,
    bool IsTenantAdmin
);

public record ProjectCtxSnapshot(
    Guid ProjectId,
    Guid TenantId,
    Guid? ProjectRoleId,
    HashSet<string> ProjectPermissionCodes,
    bool IsProjectAdmin
);
