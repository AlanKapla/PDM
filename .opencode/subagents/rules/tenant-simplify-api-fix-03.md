# tenant-simplify-api-fix-03 — TenantCtxSnapshot + CurrentUser uproszczony

## Cel
Uprość `TenantCtxSnapshot` — usuń `TenantRoleId` i `TenantPermissionCodes`, zostaw `IsAdmin`.
Uprość `CurrentUser.BuildTenantSnapshotAsync` — bez zapytania do ról/uprawnień DB.

## Skill
Przeczytaj `.github/skills/api/skill-api-services.md` przed implementacją.

## Pliki do modyfikacji

### 1. `src/Business/Interfaces/Model/ContextSnapshots.cs`

**Obecna zawartość:**
```csharp
namespace Business.Interfaces.Model;

public record TenantCtxSnapshot(
    Guid TenantId,
    Guid TenantRoleId,
    HashSet<string> TenantPermissionCodes,
    bool IsTenantAdmin,
    bool IsActive
);

public record ProjectCtxSnapshot(
    Guid ProjectId,
    Guid TenantId,
    HashSet<string> ProjectPermissionCodes,
    bool IsProjectAdmin,
    bool IsActive
);
```

**Nowa zawartość:**
```csharp
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
```

### 2. `src/Business/Implementation/Model/CurrentUser.cs`

Znajdź metodę `BuildTenantSnapshotAsync` i zastąp całą jej implementację:

**Stara implementacja:**
```csharp
private async Task<TenantCtxSnapshot> BuildTenantSnapshotAsync(Guid tenantId, CancellationToken cancellationToken)
{
    // Load tenant to get IsActive
    var tenantEntity = await tenantRepo.GetFirstBySearch(
        t => t.Id == tenantId,
        cancellationToken);

    if (tenantEntity == null)
    {
        throw new InvalidOperationException($"Tenant {tenantId} not found");
    }

    var membership = await tenantMemberRepo.GetFirstBySearch(
        tm => tm.TenantId == tenantId && tm.UserId == _id && tm.IsActive,
        q => q.Include(tm => tm.MemberRole!));

    // No membership - check if SuperAdmin for fallback access
    if (membership?.RoleId == null)
    {
        if (_systemRole == SystemRole.SuperAdmin)
        {
            return new TenantCtxSnapshot(
                tenantId,
                Guid.Empty, // No role ID for non-member SuperAdmin
                SuperAdminFallbackPermissions.TenantReadOnly,
                false, // Not a tenant admin (no membership)
                tenantEntity.IsActive // Include IsActive from tenant
            );
        }

        throw new InvalidOperationException("User is not a member of the tenant");
    }

    // Step 1: Start with permissions from tenant role
    var permissions = await GetRolePermissionsAsync(membership.RoleId.Value, cancellationToken);

    // Step 2: If SuperAdmin, ALWAYS add fallback permissions (independent of other roles)
    if (_systemRole == SystemRole.SuperAdmin)
    {
        foreach (var fallbackPermission in SuperAdminFallbackPermissions.TenantReadOnly)
        {
            permissions.Add(fallbackPermission);
        }
    }

    // Step 3: Every active tenant member gets baseline access (can list their projects)
    permissions.Add(PermissionCodes.TenantView);

    var isTenantAdmin = membership.MemberRole?.Code == RoleCodes.TenantAdmin;

    return new TenantCtxSnapshot(
        tenantId,
        membership.RoleId.Value,
        permissions,
        isTenantAdmin,
        tenantEntity.IsActive // Include IsActive from tenant
    );
}
```

**Nowa implementacja:**
```csharp
private async Task<TenantCtxSnapshot> BuildTenantSnapshotAsync(Guid tenantId, CancellationToken cancellationToken)
{
    var membership = await tenantMemberRepo.GetFirstBySearch(
        tm => tm.TenantId == tenantId && tm.UserId == _id && tm.IsActive,
        cancellationToken);

    if (membership is null)
    {
        if (_systemRole == SystemRole.SuperAdmin)
        {
            return new TenantCtxSnapshot(tenantId, IsAdmin: false, IsActive: true);
        }

        throw new InvalidOperationException("User is not a member of the tenant");
    }

    bool isAdmin = membership.IsAdmin || _systemRole == SystemRole.SuperAdmin;

    return new TenantCtxSnapshot(tenantId, IsAdmin: isAdmin, IsActive: true);
}
```

**Uwaga:** Usuń też `q => q.Include(tm => tm.MemberRole!)` z wywołania — `TenantMember` nie ma już nawigacji `MemberRole`.

### 3. `src/Business/Implementation/Model/CurrentUser.cs` — metoda `IsTenantAdminAsync`

Znajdź metodę `IsTenantAdminAsync` i zastąp implementację:

**Stara implementacja (opierała się na snapshot):**
```csharp
public async Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default)
{
    var snapshot = await GetTenantSnapshotAsync(tenantId, cancellationToken);
    return snapshot?.IsTenantAdmin ?? false;
}
```

**Nowa implementacja:**
```csharp
public async Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default)
{
    var snapshot = await GetTenantSnapshotAsync(tenantId, cancellationToken);
    return snapshot?.IsAdmin ?? false;
}
```

### 4. Usuń nieużywane using'i i dependencje z `CurrentUser`

W `CurrentUser.cs` sprawdź czy `BuildTenantSnapshotAsync` używała:
- `GetRolePermissionsAsync` — jeśli jest wywoływana TYLKO w `BuildTenantSnapshotAsync`, możesz ją usunąć z tej klasy
- `SuperAdminFallbackPermissions.TenantReadOnly` — sprawdź czy jest używane gdzieś indziej (projekty)
- `RoleCodes` — sprawdź czy jest używane gdzieś indziej w `CurrentUser`
- `IReadRepository<Role>` lub `IReadRepository<RolePermission>` — usuń z konstruktora jeśli nie są używane poza `BuildTenantSnapshotAsync`

**WAŻNE:** Nie usuwaj `GetRolePermissionsAsync` jeśli jest używana przez `BuildProjectSnapshotAsync`. Sprawdź przed usunięciem.

## Build check
```
dotnet build src/Business/Business.csproj
```

Oczekiwane błędy w `AccessService.cs` (odwołanie do `TenantPermissionCodes`) — zostanie naprawione w fix-04.
