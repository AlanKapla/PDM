# permissions-api-fix-02 — PermissionCodes (9 kodów) + ModulePermissionTranslator

## Zadanie

Zastąp 44 granularne PermissionCodes 9 kodami per moduł. Przepisz `ModulePermissionTranslator` — jeden moduł → jeden kod.

## Nowy model

| Moduł | Stare kody (do usunięcia) | Nowy kod |
|-------|--------------------------|---------|
| Settings | PROJECT.SETTINGS.VIEW, PROJECT.SETTINGS.EDIT, PROJECT.STATUS.TOGGLE, PROJECT.DASHBOARD.VIEW | `PROJECT.SETTINGS` |
| Members | PROJECT.MEMBERS.VIEW, PROJECT.MEMBERS.MANAGE | `PROJECT.MEMBERS` |
| Files | PROJECT.FILES.READ_SHARED, READ_OWN, READ_ALL, WRITE_ASSIGNED, WRITE_SHARED, WRITE_OWN, WRITE_ALL, SHARE | `PROJECT.FILES` |
| Estimates | PROJECT.ESTIMATES.READ_SHARED, READ_OWN, READ_ALL, WRITE_ASSIGNED, WRITE_SHARED, WRITE_OWN, WRITE_ALL, SHARE | `PROJECT.ESTIMATES` |
| Costs | PROJECT.COSTS.VIEW, WRITE, ACCEPT, SHARE | `PROJECT.COSTS` |
| Schedule | PROJECT.SCHEDULE.READ_SHARED, READ_OWN, READ_ALL, WRITE_ASSIGNED, WRITE_SHARED, WRITE_OWN, WRITE_ALL, SHARE | `PROJECT.SCHEDULE` |
| Dashboard | PROJECT.DASHBOARD.VIEW | `PROJECT.DASHBOARD` |
| Chat | CHAT.READ, WRITE, MEMBERS.MANAGE, RENAME, DELETE | `CHAT` |
| Tracker | PROJECT.TRACKER.VIEW, PROJECT.TRACKER.WRITE | `PROJECT.TRACKER` |

Zachować bez zmian (nie są modułami projektowymi):
- `TENANT.CONTEXT.LIST`, `TENANT.CONTEXT.ADMIN_LIST`
- `ROLE.LIST`
- `TENANT.VIEW`, `TENANT.SETTINGS.VIEW`, `TENANT.SETTINGS.EDIT`, `TENANT.MEMBERS.MANAGE`, `TENANT.STATUS.TOGGLE`, `TENANT.PROJECTS.CREATE`
- `PROJECT.VIEW` (bazowy dostęp do projektu)

## Krok 1 — Modyfikacja PermissionCodes.cs

Plik: `src/Business/Interfaces/Constants/PermissionCodes.cs`

Nowa zawartość:
```csharp
namespace Business.Interfaces.Constants;

public static class PermissionCodes
{
    // TENANT – CONTEXT
    public const string TenantContextList = "TENANT.CONTEXT.LIST";
    public const string TenantContextAdminList = "TENANT.CONTEXT.ADMIN_LIST";

    // ROLE
    public const string RoleList = "ROLE.LIST";

    // TENANT – BASE ACCESS
    public const string TenantView = "TENANT.VIEW";

    // TENANT – SETTINGS
    public const string TenantSettingsView = "TENANT.SETTINGS.VIEW";
    public const string TenantSettingsEdit = "TENANT.SETTINGS.EDIT";
    public const string TenantMembersManage = "TENANT.MEMBERS.MANAGE";
    public const string TenantStatusToggle = "TENANT.STATUS.TOGGLE";
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
    public const string ProjectDashboard = "PROJECT.DASHBOARD";
    public const string ProjectTracker = "PROJECT.TRACKER";
    public const string Chat = "CHAT";
}
```

## Krok 2 — Przepisanie ModulePermissionTranslator.cs

Plik: `src/Business/Interfaces/Constants/ModulePermissionTranslator.cs`

Nowa zawartość:
```csharp
using Entities.Enums;

namespace Business.Interfaces.Constants;

public static class ModulePermissionTranslator
{
    public static HashSet<string> Translate(ProjectModule module)
    {
        return module switch
        {
            ProjectModule.Settings => new HashSet<string> { PermissionCodes.ProjectSettings },
            ProjectModule.Members => new HashSet<string> { PermissionCodes.ProjectMembers },
            ProjectModule.Files => new HashSet<string> { PermissionCodes.ProjectFiles },
            ProjectModule.Estimates => new HashSet<string> { PermissionCodes.ProjectEstimates },
            ProjectModule.Costs => new HashSet<string> { PermissionCodes.ProjectCosts },
            ProjectModule.Schedule => new HashSet<string> { PermissionCodes.ProjectSchedule },
            ProjectModule.Dashboard => new HashSet<string> { PermissionCodes.ProjectDashboard },
            ProjectModule.Chat => new HashSet<string> { PermissionCodes.Chat },
            ProjectModule.Tracker => new HashSet<string> { PermissionCodes.ProjectTracker },
            _ => new HashSet<string>()
        };
    }

    /// <summary>Returns all module permission codes (all 9 modules).</summary>
    public static HashSet<string> GetAllModulePermissions()
    {
        HashSet<string> result = new();
        foreach (ProjectModule module in Enum.GetValues<ProjectModule>())
        {
            foreach (string code in Translate(module))
                result.Add(code);
        }
        return result;
    }
}
```

## Krok 3 — Aktualizacja CurrentUser.cs

Plik: `src/Business/Implementation/Model/CurrentUser.cs`

W metodzie `BuildProjectSnapshotAsync` znajdź fragment iterujący ModulePermissions (krok 3 dla zwykłego membera):

```csharp
// STARE — znajdź i zastąp:
foreach (string code in ModulePermissionTranslator.Translate(mp.Module, mp.AccessLevel))
    permissions.Add(code);
```

Zastąp na:
```csharp
foreach (string code in ModulePermissionTranslator.Translate(mp.Module))
    permissions.Add(code);
```

Oraz zaktualizuj wywołanie `GetAllAdminPermissions()` → `GetAllModulePermissions()` jeśli jest używane (szukaj w tym samym pliku `GetAllAdminPermissions`).

## Krok 4 — PermissionScopes.cs

Plik: `src/Business/Interfaces/Constants/PermissionScopes.cs`

Zaktualizuj słownik scope'ów — zastąp stare kody modułowe nowymi. Przejrzyj plik i dla każdego starego kodu projektowego podmień na nowy. Przykład:

Stare:
```csharp
PermissionCodes.ProjectSettingsView => PermissionScope.Project,
PermissionCodes.ProjectSettingsEdit => PermissionScope.Project,
PermissionCodes.ProjectStatusToggle => PermissionScope.Project,
// ... itd wszystkie Project.*
```

Nowe:
```csharp
PermissionCodes.ProjectSettings => PermissionScope.Project,
PermissionCodes.ProjectMembers => PermissionScope.Project,
PermissionCodes.ProjectFiles => PermissionScope.Project,
PermissionCodes.ProjectEstimates => PermissionScope.Project,
PermissionCodes.ProjectCosts => PermissionScope.Project,
PermissionCodes.ProjectSchedule => PermissionScope.Project,
PermissionCodes.ProjectDashboard => PermissionScope.Project,
PermissionCodes.ProjectTracker => PermissionScope.Project,
PermissionCodes.Chat => PermissionScope.Project,
```

## Krok 5 — SuperAdminFallbackPermissions (jeśli istnieje)

Szukaj pliku `SuperAdminFallbackPermissions.cs` lub podobnego. Jeśli zawiera stare kody modułowe — podmień na nowe 9 kodów.

## Krok 6 — Weryfikacja build

```powershell
cd 02-ApplicationServices/ProductDataManagementWebAPI
dotnet build src/Business/Business.csproj 2>&1 | Select-Object -Last 5
```
