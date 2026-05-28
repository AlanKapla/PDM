namespace Business.Interfaces.Model;

public record TenantCtxSnapshot(
    Guid TenantId,
    bool IsAdmin,
    bool IsActive
);

public record ProjectCtxSnapshot(
    Guid ProjectId,
    Guid TenantId,
    HashSet<string> ProjectPermissionCodes,
    bool IsProjectAdmin,
    bool IsActive
);
