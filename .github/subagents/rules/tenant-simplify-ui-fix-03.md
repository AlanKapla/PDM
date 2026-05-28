# tenant-simplify-ui-fix-03 — Komponenty: roleCode → isAdmin + walidacja duplikatów zaproszeń

## Cel
Zaktualizuj komponenty używające `roleCode` do pracy z `isAdmin`.
Dodaj walidację duplikatów na UI w formularzu zapraszania członka.

## Skill
Przeczytaj `.github/skills/ui/skill-ui-components.md` i `.github/skills/ui/skill-ui-forms-modals.md` przed implementacją.

---

## 1. `src/pages/TenantDetails.tsx`

Ten plik ma liczne odwołania do `roleCode` — zastąp je `isAdmin`.

### Import

Zamień import:
```typescript
// STARE:
import { getRoleName, getRoleColor } from "../constants/roleCodes";

// NOWE:
import { getTenantRoleName, getTenantRoleColor } from "../constants/roleCodes";
```

### Użycie `member.roleCode` — Badge z rolą

Znajdź wszystkie wystąpienia:
```tsx
// STARE:
<Badge colorScheme={getRoleColor(member.roleCode)}>
  {getRoleName(member.roleCode)}
</Badge>

// NOWE:
<Badge colorScheme={getTenantRoleColor(member.isAdmin)}>
  {getTenantRoleName(member.isAdmin)}
</Badge>
```

### Użycie `tenant.roleCode` — Badge z rolą tenanta

```tsx
// STARE:
<Badge colorScheme={getRoleColor(tenant.roleCode)}>
  {getRoleName(tenant.roleCode)}
</Badge>

// NOWE:
<Badge colorScheme={getTenantRoleColor(tenant.isAdmin)}>
  {getTenantRoleName(tenant.isAdmin)}
</Badge>
```

### Dropdown zmiany roli — zastąp wyborem IsAdmin

Znajdź sekcję gdzie renderowany jest dropdown ról dla członka (wcześniej używał `roleId` z listy ról).
Zastąp dropdown prostym przełącznikiem admin/member:

```tsx
// STARE (dropdown z listą ról pobieranych z API):
// <Select value={...} onChange={...}>
//   {roles.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
// </Select>

// NOWE:
<Select
  value={member.isAdmin ? "admin" : "member"}
  onChange={(e) => handleToggleAdmin(member.userId, e.target.value === "admin")}
  size="sm"
>
  <option value="member">Członek</option>
  <option value="admin">Administrator</option>
</Select>
```

Zaktualizuj handler `handleToggleAdmin` (lub zmień nazwę z `handleRoleChange`):
```typescript
const handleToggleAdmin = async (userId: string, isAdmin: boolean) => {
  // wywołaj updateTenantMemberAdmin zamiast updateTenantMemberRole
  await updateTenantMemberAdmin(tenantId, userId, isAdmin);
  // refresh listy
};
```

Usuń pobieranie listy ról przez `roleApi` / React Query jeśli było używane tylko do dropdown ról tenanta.

### Walidacja duplikatów w formularzu zapraszania

Znajdź formularz/modal zapraszania członka (InviteMemberModal lub inline form w TenantDetails).
Dodaj walidację przed wysłaniem — sprawdź czy email istnieje już w `members` lub `invitations`:

```typescript
const handleInvite = async (email: string) => {
  const normalizedEmail = email.trim().toLowerCase();

  // Sprawdź czy jest już aktywnym członkiem
  const alreadyMember = tenantDetails?.members.some(
    (m) => m.email.toLowerCase() === normalizedEmail
  );
  if (alreadyMember) {
    // Pokaż error toast lub field error
    showError("Ten użytkownik jest już członkiem organizacji.");
    return;
  }

  // Sprawdź czy ma już aktywne zaproszenie
  const alreadyInvited = tenantDetails?.invitations.some(
    (i) => i.email.toLowerCase() === normalizedEmail
  );
  if (alreadyInvited) {
    showError("Aktywne zaproszenie dla tego adresu email już istnieje.");
    return;
  }

  // Wyślij zaproszenie
  await inviteMember(tenantId, email);
};
```

**Uwaga:** Użyj istniejącego systemu toastów/notyfikacji z projektu (np. Chakra UI `useToast`). Sprawdź jak inne modale obsługują błędy.

---

## 2. `src/pages/CollaboratingTenants.tsx`

Znajdź użycia `tenant.roleCode`:

```tsx
// STARE:
<Badge colorScheme={getRoleColor(tenant.roleCode)} fontSize="xs">
  {getRoleName(tenant.roleCode)}
</Badge>

// NOWE:
<Badge colorScheme={getTenantRoleColor(tenant.isAdmin)} fontSize="xs">
  {getTenantRoleName(tenant.isAdmin)}
</Badge>
```

Zaktualizuj import z `roleCodes.ts`:
```typescript
// STARE:
import { getRoleName, getRoleColor } from "../constants/roleCodes";

// NOWE:
import { getTenantRoleName, getTenantRoleColor } from "../constants/roleCodes";
```

---

## 3. `src/pages/ManagedTenants.tsx`

Znajdź użycia `tenant.roleCode`:

```tsx
// STARE:
<Badge colorScheme={getRoleColor(tenant.roleCode)} fontSize="xs">
  {getRoleName(tenant.roleCode)}
</Badge>

// NOWE:
<Badge colorScheme={getTenantRoleColor(tenant.isAdmin)} fontSize="xs">
  {getTenantRoleName(tenant.isAdmin)}
</Badge>
```

Zaktualizuj import:
```typescript
// STARE:
import { getRoleName, getRoleColor, RoleCodes } from "../constants/roleCodes";

// NOWE:
import { getTenantRoleName, getTenantRoleColor } from "../constants/roleCodes";
```

Usuń użycia `RoleCodes.TENANT_ADMIN` jeśli istniały do porównań — zastąp `tenant.isAdmin === true`.

---

## 4. Sprawdź inne komponenty

Uruchom:
```
npx tsc --noEmit 2>&1 | Select-String "roleCode|RoleCode|TENANT_ADMIN|TENANT_MEMBER|TenantStatusToggle|activeTenantPermissions" | Select-Object -First 30
```

Napraw wszystkie znalezione błędy TypeScript analogicznie do powyższych wzorców.

---

## Final TypeScript check
```
npx tsc --noEmit
```

Wynik powinien być: `Exit: 0` (brak błędów).
