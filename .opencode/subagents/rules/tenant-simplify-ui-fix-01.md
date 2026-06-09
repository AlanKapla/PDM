# tenant-simplify-ui-fix-01 — Typy + API Client: roleCode → isAdmin

## Cel
Zastąp `roleCode: string` przez `isAdmin: boolean` w typach tenanta.
Zastąp `activeTenantPermissions: string[]` przez `isActiveTenantAdmin: boolean` w `UserProfile`.
Zaktualizuj `tenantApi` — endpoint `updateTenantMemberRole` → `updateTenantMemberAdmin`.

## Skill
Przeczytaj `.opencode/skills/ui/skill-ui-types.md` i `.opencode/skills/ui/skill-ui-api-client.md` przed implementacją.

---

## 1. `src/types/auth.types.ts`

### Zmiana `UserProfile`

Zamień pole:
```typescript
// STARE:
activeTenantPermissions: string[];

// NOWE:
isActiveTenantAdmin: boolean;
```

### Zmiana `TenantMemberDetails`

```typescript
// STARE:
export interface TenantMemberDetails {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;
  isActive: boolean;
  joinedAt: string;
}

// NOWE:
export interface TenantMemberDetails {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  isAdmin: boolean;
  isActive: boolean;
  joinedAt: string;
}
```

### Zmiana `UserTenant`

```typescript
// STARE:
export interface UserTenant {
  id: string;
  name: string;
  createdAt: string;
  isActive: boolean;
  roleCode: string;
  isActiveTenant: boolean;
}

// NOWE:
export interface UserTenant {
  id: string;
  name: string;
  createdAt: string;
  isActive: boolean;
  isAdmin: boolean;
  isActiveTenant: boolean;
}
```

### Zmiana `TenantBasic`

```typescript
// STARE:
export interface TenantBasic {
  id: string;
  name: string;
  createdAt: string;
  isActive: boolean;
  roleCode: string;
}

// NOWE:
export interface TenantBasic {
  id: string;
  name: string;
  createdAt: string;
  isActive: boolean;
  isAdmin: boolean;
}
```

### Zmiana `TenantDetails`

```typescript
// STARE:
export interface TenantDetails {
  id: string;
  name: string;
  createdAt: string;
  roleCode: string;
  isActive: boolean;
  members: TenantMemberDetails[];
  invitations: TenantInvitationWeb[];
}

// NOWE:
export interface TenantDetails {
  id: string;
  name: string;
  createdAt: string;
  isAdmin: boolean;
  isActive: boolean;
  members: TenantMemberDetails[];
  invitations: TenantInvitationWeb[];
}
```

### Usuń nieużywany legacy kod

Usuń lub zostaw (decyzja agenta): `TenantRole`, `getTenantRoleLevel`, `hasTenantRoleLevel`, `isTenantAdmin`, `canEditTenant`, `canViewTenant` — są to stare helpersy oparte o enum numeryczny. Jeśli nie są używane w komponentach (szukaj importów), usuń.

---

## 2. `src/constants/roleCodes.ts`

### Usuń role tenanta z `RoleCodes`

```typescript
// STARE:
export const RoleCodes = {
  SYSTEM_SUPERADMIN: "SYSTEM.SUPERADMIN",
  TENANT_ADMIN: "TENANT.ADMIN",
  TENANT_MEMBER: "TENANT.MEMBER",
  // ...
};

// NOWE:
export const RoleCodes = {
  SYSTEM_SUPERADMIN: "SYSTEM.SUPERADMIN",
  // Tenant roles replaced by IsAdmin boolean
  // ...
};
```

### Usuń `TenantStatusToggle` z `PermissionCodes`

```typescript
// Usuń tę linię:
TenantStatusToggle: "TENANT.STATUS.TOGGLE",
```

### Zaktualizuj `getRoleName` i `getRoleColor`

Usuń wpisy dla `TENANT_ADMIN` i `TENANT_MEMBER` z `getRoleName` i `getRoleColor`. Zachowaj PROJECT_* wpisy.

### Dodaj helper `getTenantRoleName`

```typescript
/**
 * Get Polish display name for tenant admin status
 */
export const getTenantRoleName = (isAdmin: boolean): string => {
  return isAdmin ? 'Administrator' : 'Członek';
};

/**
 * Get badge color for tenant admin status
 */
export const getTenantRoleColor = (isAdmin: boolean): string => {
  return isAdmin ? 'level2' : 'gray';
};
```

---

## 3. `src/api/tenantApi.ts`

### Zmień `updateTenantMemberRole` → `updateTenantMemberAdmin`

```typescript
// STARE:
updateTenantMemberRole: async (tenantId: string, userId: string, roleId: string) => {
  return axiosClient.patch(`/tenants/${tenantId}/members/${userId}/role`, { roleId });
},

// NOWE:
updateTenantMemberAdmin: async (tenantId: string, userId: string, isAdmin: boolean) => {
  return axiosClient.patch(`/tenants/${tenantId}/members/${userId}/admin`, { isAdmin });
},
```

### Usuń `toggleTenantStatus`

```typescript
// Usuń:
toggleTenantStatus: async (tenantId: string, isActive: boolean) => {
  return axiosClient.patch(`/tenants/${tenantId}/status?isActive=${isActive}`);
},
```

---

## 4. `src/services/tenantService.ts`

Zaktualizuj `updateTenantMemberRole` → `updateTenantMemberAdmin`:

```typescript
// STARE:
export const updateTenantMemberRole = async (tenantId: string, userId: string, roleId: string): Promise<boolean> => {
  try {
    await tenantApi.updateTenantMemberRole(tenantId, userId, roleId);
    return true;
  } catch (error) {
    return false;
  }
};

// NOWE:
export const updateTenantMemberAdmin = async (tenantId: string, userId: string, isAdmin: boolean): Promise<boolean> => {
  try {
    await tenantApi.updateTenantMemberAdmin(tenantId, userId, isAdmin);
    return true;
  } catch (error) {
    return false;
  }
};
```

---

## TypeScript check
```
npx tsc --noEmit
```

Oczekiwane błędy w komponentach używających `roleCode` (np. TenantDetails.tsx, CollaboratingTenants.tsx, ManagedTenants.tsx) — będą naprawione w fix-03.
