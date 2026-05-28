# tenant-simplify-api-fix-06 — TenantController + PermissionCodes + Cleanup

## Cel
1. Zaktualizuj `TenantController` — zmień endpoint `PATCH /members/{userId}/role` na `/members/{userId}/admin`, usuń `ToggleTenantStatus`
2. Usuń `TenantStatusToggle` z `PermissionCodes` i `PermissionScopes`
3. Usuń `RoleCodes.TenantAdmin` i `RoleCodes.TenantMember`

## Skill
Przeczytaj `.github/skills/api/skill-api-controllers.md` przed implementacją.

---

## 1. `src/WebApi/Controllers/TenantController.cs`

### Zmiana endpointu roli → admin

**Stare:**
```csharp
[HttpPatch("{tenantId}/members/{userId}/role")]
[Authorize(Policy = PermissionCodes.TenantMembersManage)]
public async Task<IActionResult> UpdateTenantMemberRole(
    Guid tenantId,
    Guid userId,
    [FromBody] UpdateTenantMemberRoleCommand request)
{
    request = request with { TenantId = tenantId, UserId = userId };
    await Send(request);
    return NoContent();
}
```

**Nowe:**
```csharp
[HttpPatch("{tenantId}/members/{userId}/admin")]
[Authorize(Policy = PermissionCodes.TenantMembersManage)]
public async Task<IActionResult> UpdateTenantMemberAdmin(
    Guid tenantId,
    Guid userId,
    [FromBody] UpdateTenantMemberRoleCommand request)
{
    request = request with { TenantId = tenantId, UserId = userId };
    await Send(request);
    return NoContent();
}
```

### Usuń endpoint ToggleTenantStatus

Usuń całą metodę:
```csharp
[HttpPatch("{tenantId}/status")]
[Authorize(Policy = PermissionCodes.TenantStatusToggle)]
public async Task<IActionResult> ToggleTenantStatus([FromRoute] Guid tenantId, [FromQuery] bool isActive)
{
    ToggleTenantStatusCommand command = new ToggleTenantStatusCommand { TenantId = tenantId, IsActive = isActive };
    await Send(command);
    return NoContent();
}
```

Usuń odpowiadający using dla `CQRS.Tenants.ToggleTenantStatus` z sekcji using na górze pliku.

---

## 2. `src/Business/Interfaces/Constants/PermissionCodes.cs`

Usuń:
```csharp
public const string TenantStatusToggle = "TENANT.STATUS.TOGGLE";
```

Zaktualizuj tablicę `All` — usuń `TenantStatusToggle` z listy.

---

## 3. `src/Business/Interfaces/Constants/PermissionScopes.cs`

Usuń wpis:
```csharp
[PermissionCodes.TenantStatusToggle] = PermissionScope.Tenant,
```

---

## 4. `src/Business/Interfaces/Constants/RoleCodes.cs`

Usuń stałe dotyczące tenanta:
```csharp
public const string TenantAdmin = "TENANT.ADMIN";
public const string TenantMember = "TENANT.MEMBER";
```

Zachowaj tylko:
```csharp
public const string SystemSuperAdmin = "SYSTEM.SUPERADMIN";
```

(i ewentualnie PROJECT.* jeśli są używane)

---

## 5. Sprawdź `ToggleTenantStatus` handler/command

Znajdź `src/CQRS/Tenants/ToggleTenantStatus/` — **nie usuwaj** tych plików. Tylko usuń endpoint z controllera. Pozostaw handler jako martwy kod (lub usuń jeśli nie generuje błędów kompilacji po usunięciu endpointu).

Jeśli handler kompiluje się bez błędów i nie jest nigdzie odwołany — możesz go zostawić lub usunąć. Decyzja: **zostaw** (nie usuwaj plików, bo to ryzykowna operacja).

---

## Build check — pełny projekt
```
dotnet build src/WebApi/WebApi.csproj --configuration Release
```

Upewnij się że `Build succeeded` bez błędów.
