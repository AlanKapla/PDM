# tenant-simplify-api-fix-04 — AccessService: uproszczona autoryzacja tenanta

## Cel
Uprość `AuthorizeTenantPermissionAsync` — zamiast sprawdzać `TenantPermissionCodes`,
sprawdzaj `IsAdmin` na podstawie kategorii permission code.

Usuń:
- `IsCrossTenantEnabled` — admin działa tylko na swoim active tenantcie
- check `!tenantSnapshot.IsActive` — tenant zawsze aktywny, nie obsługujemy toggle

## Skill
Przeczytaj `.opencode/skills/api/skill-api-services.md` przed implementacją.

## Plik do modyfikacji

### `src/Business/Implementation/Services/AccessService.cs`

**Obecna pełna zawartość do zastąpienia:**

```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services;

public sealed class AccessService : IAccessService
{
    private readonly ILogger<AccessService> logger;

    public AccessService(ILogger<AccessService> logger)
    {
        this.logger = logger;
    }

    public async Task<bool> AuthorizeAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
        ResourceScope? resourceScope = null,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsAuthenticated)
        {
            logger.LogWarning("Authorization failed: User not authenticated");
            return false;
        }

        var scope = PermissionScopes.Get(permissionCode);

        if (scope == PermissionScope.Global)
        {
            logger.LogDebug(
                "Permission {PermissionCode} granted — Global scope, no DB check required",
                permissionCode);

            return true;
        }

        if (resource.TenantId == Guid.Empty)
        {
            logger.LogWarning(
                "Authorization failed: TenantId is required for permission {Permission}",
                permissionCode);
            return false;
        }

        if (!IsCrossTenantEnabled(permissionCode))
        {
            if (user.ActiveTenantId.HasValue && resource.TenantId != user.ActiveTenantId.Value)
            {
                logger.LogWarning(
                    "Authorization failed: Resource TenantId {ResourceTenantId} does not match ActiveTenantId {ActiveTenantId} for permission {Permission}",
                    resource.TenantId,
                    user.ActiveTenantId,
                    permissionCode);
                return false;
            }
        }

        if (scope == PermissionScope.Project)
        {
            return await AuthorizeProjectPermissionAsync(user, permissionCode, resource, resourceScope, cancellationToken);
        }
        else
        {
            return await AuthorizeTenantPermissionAsync(user, permissionCode, resource, cancellationToken);
        }
    }

    private static bool IsCrossTenantEnabled(string permissionCode)
    {
        return permissionCode == PermissionCodes.TenantSettingsEdit
            || permissionCode == PermissionCodes.TenantMembersManage
            || permissionCode == PermissionCodes.TenantStatusToggle;
    }

    private async Task<bool> AuthorizeTenantPermissionAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
        CancellationToken cancellationToken)
    {
        var tenantSnapshot = await user.GetTenantSnapshotAsync(resource.TenantId, cancellationToken);
        
        if (tenantSnapshot == null)
        {
            logger.LogWarning(
                "Authorization failed: User {UserId} has no access to tenant {TenantId}",
                user.Id,
                resource.TenantId);
            return false;
        }

        if (!tenantSnapshot.TenantPermissionCodes.Contains(permissionCode))
        {
            logger.LogWarning(
                "Authorization failed: User {UserId} lacks permission {Permission} in tenant {TenantId}",
                user.Id,
                permissionCode,
                resource.TenantId);
            return false;
        }

        if (!tenantSnapshot.IsTenantAdmin && !tenantSnapshot.IsActive && !user.IsSuperAdmin)
        {
            logger.LogWarning(
                "Authorization failed: Tenant {TenantId} is inactive and user is not admin or SuperAdmin",
                resource.TenantId);
            return false;
        }

        logger.LogDebug(
            "Authorization granted: User {UserId} has permission {Permission} in tenant {TenantId}",
            user.Id,
            permissionCode,
            resource.TenantId);

        return true;
    }

    // ... reszta klasy (AuthorizeProjectPermissionAsync) pozostaje bez zmian
```

**Nowa implementacja `AuthorizeAsync` i `AuthorizeTenantPermissionAsync`:**

Zachowaj `AuthorizeProjectPermissionAsync` bez zmian. Zmień tylko poniższe metody:

```csharp
public async Task<bool> AuthorizeAsync(
    ICurrentUser user,
    string permissionCode,
    ResourceRef resource,
    ResourceScope? resourceScope = null,
    CancellationToken cancellationToken = default)
{
    if (!user.IsAuthenticated)
    {
        logger.LogWarning("Authorization failed: User not authenticated");
        return false;
    }

    PermissionScope scope = PermissionScopes.Get(permissionCode);

    if (scope == PermissionScope.Global)
    {
        logger.LogDebug(
            "Permission {PermissionCode} granted — Global scope, no DB check required",
            permissionCode);
        return true;
    }

    if (resource.TenantId == Guid.Empty)
    {
        logger.LogWarning(
            "Authorization failed: TenantId is required for permission {Permission}",
            permissionCode);
        return false;
    }

    // Enforce active tenant match — no cross-tenant access
    if (user.ActiveTenantId.HasValue && resource.TenantId != user.ActiveTenantId.Value)
    {
        logger.LogWarning(
            "Authorization failed: Resource TenantId {ResourceTenantId} does not match ActiveTenantId {ActiveTenantId} for permission {Permission}",
            resource.TenantId,
            user.ActiveTenantId,
            permissionCode);
        return false;
    }

    if (scope == PermissionScope.Project)
    {
        return await AuthorizeProjectPermissionAsync(user, permissionCode, resource, resourceScope, cancellationToken);
    }

    return await AuthorizeTenantPermissionAsync(user, permissionCode, resource, cancellationToken);
}

/// <summary>
/// Admin-only tenant permissions — require IsAdmin = true on TenantMember.
/// </summary>
private static readonly HashSet<string> AdminPermissions = new()
{
    PermissionCodes.TenantSettingsEdit,
    PermissionCodes.TenantMembersManage,
};

private async Task<bool> AuthorizeTenantPermissionAsync(
    ICurrentUser user,
    string permissionCode,
    ResourceRef resource,
    CancellationToken cancellationToken)
{
    TenantCtxSnapshot? tenantSnapshot = await user.GetTenantSnapshotAsync(resource.TenantId, cancellationToken);

    if (tenantSnapshot is null)
    {
        logger.LogWarning(
            "Authorization failed: User {UserId} has no access to tenant {TenantId}",
            user.Id,
            resource.TenantId);
        return false;
    }

    // Member-level permissions: any active tenant member is authorized
    // Admin-level permissions: only admins (IsAdmin = true) are authorized
    if (AdminPermissions.Contains(permissionCode) && !tenantSnapshot.IsAdmin)
    {
        logger.LogWarning(
            "Authorization failed: User {UserId} lacks admin access for permission {Permission} in tenant {TenantId}",
            user.Id,
            permissionCode,
            resource.TenantId);
        return false;
    }

    logger.LogDebug(
        "Authorization granted: User {UserId} has permission {Permission} in tenant {TenantId}",
        user.Id,
        permissionCode,
        resource.TenantId);

    return true;
}
```

## Ważne uwagi

1. Usuń metodę `IsCrossTenantEnabled` — nie jest już potrzebna.
2. `AdminPermissions` zawiera tylko `TenantSettingsEdit` i `TenantMembersManage`. Pozostałe tenant permissions (`TenantView`, `TenantSettingsView`, `TenantProjectsCreate`) są dostępne dla wszystkich członków.
3. `TenantStatusToggle` nie jest używane — endpoint zostanie usunięty w fix-06. Nie dodawaj go do `AdminPermissions`.

## Build check
```
dotnet build src/Business/Business.csproj
```
