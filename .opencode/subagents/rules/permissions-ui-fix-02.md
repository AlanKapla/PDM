# permissions-ui-fix-02 — PermissionCodes (9 kodów) + useProjectPermissions

## Zadanie

Zastąp 44 granularne PermissionCodes 9 nowymi kodami. Uprość hook `useProjectPermissions` do prostych flag per moduł.

## Krok 1 — constants/roleCodes.ts

Plik: `src/constants/roleCodes.ts`

Znajdź sekcję `export const PermissionCodes = { ... }` i zastąp całą zawartość:

**Nowe PermissionCodes (9 kodów modułowych + zachowane Tenant/Global):**
```typescript
export const PermissionCodes = {
  // TENANT – CONTEXT
  TenantContextList: "TENANT.CONTEXT.LIST",
  TenantContextAdminList: "TENANT.CONTEXT.ADMIN_LIST",

  // ROLE
  RoleList: "ROLE.LIST",

  // TENANT – BASE ACCESS
  TenantView: "TENANT.VIEW",

  // TENANT – SETTINGS
  TenantSettingsView: "TENANT.SETTINGS.VIEW",
  TenantSettingsEdit: "TENANT.SETTINGS.EDIT",
  TenantMembersManage: "TENANT.MEMBERS.MANAGE",
  TenantStatusToggle: "TENANT.STATUS.TOGGLE",
  TenantProjectsCreate: "TENANT.PROJECTS.CREATE",

  // PROJECT – BASE
  ProjectView: "PROJECT.VIEW",

  // PROJECT – MODULES (one per module)
  ProjectSettings: "PROJECT.SETTINGS",
  ProjectMembers: "PROJECT.MEMBERS",
  ProjectFiles: "PROJECT.FILES",
  ProjectEstimates: "PROJECT.ESTIMATES",
  ProjectCosts: "PROJECT.COSTS",
  ProjectSchedule: "PROJECT.SCHEDULE",
  ProjectDashboard: "PROJECT.DASHBOARD",
  ProjectTracker: "PROJECT.TRACKER",
  Chat: "CHAT",
} as const;

export type PermissionCode = (typeof PermissionCodes)[keyof typeof PermissionCodes];
```

Zachowaj bez zmian:
- `RoleCodes` — bez zmian
- `hasPermission`, `hasAnyPermission`, `hasAllPermissions` — bez zmian
- `isSuperAdminRole`, `isTenantAdminRole`, `isProjectAdminRole` — bez zmian

## Krok 2 — hooks/useProjectPermissions.ts

Plik: `src/hooks/useProjectPermissions.ts`

Przepisz hook — zastąp 23 granularne flagi 9 flagami per moduł.

Wzorzec nowej zawartości:
```typescript
import { useProjectDetails } from '../features/projects/hooks/useProjectDetails'; // dostosuj ścieżkę do istniejącej
import { PermissionCodes, hasPermission } from '../constants/roleCodes';

export function useProjectPermissions(projectId: string | undefined) {
  // Pobierz permissions z project details (zachowaj istniejący mechanizm)
  // Sprawdź jak hook aktualnie pobiera permissions i zachowaj ten sam pattern
  
  const permissions = /* istniejący mechanizm pobierania */ [];
  const loading = /* istniejące loading state */;

  return {
    // Settings module
    canSettings: hasPermission(permissions, PermissionCodes.ProjectSettings),
    // Aliasy dla kompatybilności wstecznej z istniejącymi komponentami
    canView: hasPermission(permissions, PermissionCodes.ProjectSettings),
    canEdit: hasPermission(permissions, PermissionCodes.ProjectSettings),
    canManageStatus: hasPermission(permissions, PermissionCodes.ProjectSettings),
    canViewDashboard: hasPermission(permissions, PermissionCodes.ProjectDashboard),

    // Members module
    canMembers: hasPermission(permissions, PermissionCodes.ProjectMembers),
    canViewMembers: hasPermission(permissions, PermissionCodes.ProjectMembers),
    canManageMembers: hasPermission(permissions, PermissionCodes.ProjectMembers),

    // Files module
    canViewFiles: hasPermission(permissions, PermissionCodes.ProjectFiles),

    // Estimates module
    canViewEstimates: hasPermission(permissions, PermissionCodes.ProjectEstimates),

    // Costs module
    canViewCosts: hasPermission(permissions, PermissionCodes.ProjectCosts),

    // Schedule module
    canViewSchedule: hasPermission(permissions, PermissionCodes.ProjectSchedule),

    // Dashboard module
    canDashboard: hasPermission(permissions, PermissionCodes.ProjectDashboard),

    // Chat module
    canChat: hasPermission(permissions, PermissionCodes.Chat),

    // Tracker module
    canTracker: hasPermission(permissions, PermissionCodes.ProjectTracker),

    // Derived
    hasAnyResourceAccess:
      hasPermission(permissions, PermissionCodes.ProjectFiles) ||
      hasPermission(permissions, PermissionCodes.ProjectEstimates) ||
      hasPermission(permissions, PermissionCodes.ProjectSchedule),

    allPermissions: permissions,
    loading,
  };
}
```

**Ważne:** Przeczytaj istniejący plik przed przepisaniem i zachowaj identyczny mechanizm pobierania permissions (jak hook aktualnie uzyskuje `permissions` array i `loading` state). Zmień tylko logikę boolean flag.

## Weryfikacja

```bash
npx tsc --noEmit 2>&1 | grep "useProjectPermissions\|roleCodes\|error TS" | head -30
```
