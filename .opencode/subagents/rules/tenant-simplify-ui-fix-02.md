# tenant-simplify-ui-fix-02 — Hook useTenantPermissions + AuthContext

## Cel
Uprość `useTenantPermissions` — zamiast sprawdzać permission codes z tablicy, sprawdzaj `isActiveTenantAdmin`.
Zaktualizuj `AuthContext` jeśli odwołuje się do `activeTenantPermissions`.

## Skill
Przeczytaj `.github/skills/ui/skill-ui-hooks.md` przed implementacją.

---

## 1. `src/hooks/useTenantPermissions.ts`

Zastąp całą zawartość:

```typescript
import { useAuth } from '../context/AuthContext';

/**
 * Hook do sprawdzania uprawnień użytkownika w aktywnym tenancie.
 * Uprawnienia bazują na fladze isActiveTenantAdmin zamiast permission codes.
 */
export function useTenantPermissions() {
  const { user } = useAuth();

  if (!user || !user.activeTenantId) {
    return {
      isAdmin: false,
      canView: false,
      canEdit: false,
      canManageMembers: false,
      canCreateProject: false,
    };
  }

  const isAdmin = user.isActiveTenantAdmin ?? false;

  return {
    // Czy user jest administratorem aktywnego tenanta
    isAdmin,

    // Wszyscy członkowie mogą przeglądać
    canView: true,

    // Tylko admin może edytować ustawienia tenanta
    canEdit: isAdmin,

    // Tylko admin może zarządzać członkami
    canManageMembers: isAdmin,

    // Wszyscy członkowie mogą tworzyć projekty
    canCreateProject: true,
  };
}
```

---

## 2. `src/context/AuthContext.tsx`

Sprawdź czy plik odwołuje się do `activeTenantPermissions` bezpośrednio (np. w logach lub użyciu).
Jeśli tak, zaktualizuj do `isActiveTenantAdmin`.

Zazwyczaj `AuthContext` tylko ustawia `user` ze response API — nie powinien ręcznie czytać uprawnień.
Jeśli nie ma bezpośredniego odwołania do `activeTenantPermissions` — plik nie wymaga zmian.

---

## TypeScript check
```
npx tsc --noEmit 2>&1 | Select-String "useTenantPermissions|AuthContext|auth.types" | Select-Object -First 20
```
