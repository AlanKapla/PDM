# Frontend Migration Guide - Role Enums to RoleCode

## 🎯 Overview

Backend has been refactored to use **dynamic role codes** (`string`) instead of hardcoded enums (`number`). Frontend needs to be updated to match.

---

## 📊 Breaking Changes Summary

| Type | Before (❌ Old) | After (✅ New) |
|------|----------------|---------------|
| **Tenant Role** | `role: number` (0, 1, 2, 3) | `roleCode: string` ("TENANT.ADMIN", "TENANT.MEMBER") |
| **Project Role** | `role: number` (0, 1, 2, 3) | `roleCode: string` ("PROJECT.ADMIN", "PROJECT.EDITOR", etc.) |
| **User Details** | `projectRoles: Record<string, number>` | `projectRoleCodes: Record<string, string>` |

---

## 🔧 Required Changes

### **1. Type Definitions (auth.types.ts)**

#### ❌ Before:
```typescript
export const TenantRole = {
  Admin: 0,
  Member: 1,
  Editor: 2,
  Viewer: 3,
} as const;

export interface TenantMemberDetails {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: number;  // ❌ Enum number
  isActive: boolean;
  joinedAt: string;
}

export interface TenantDetails {
  id: string;
  name: string;
  createdAt: string;
  role: number;  // ❌ Enum number
  isActive: boolean;
  members: TenantMemberDetails[];
  invitations: TenantInvitationWeb[];
}
```

#### ✅ After:
```typescript
// RoleCodes constants (matching backend RoleCodes.cs)
export const RoleCodes = {
  // Tenant roles
  TENANT_ADMIN: "TENANT.ADMIN",
  TENANT_MEMBER: "TENANT.MEMBER",
  
  // Project roles
  PROJECT_ADMIN: "PROJECT.ADMIN",
  PROJECT_EDITOR: "PROJECT.EDITOR",
  PROJECT_COLLABORATOR: "PROJECT.COLLABORATOR",
  PROJECT_VIEWER: "PROJECT.VIEWER",
  PROJECT_MEMBER: "PROJECT.MEMBER",
} as const;

export interface TenantMemberDetails {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;  // ✅ Dynamic role code
  isActive: boolean;
  joinedAt: string;
}

export interface TenantDetails {
  id: string;
  name: string;
  createdAt: string;
  roleCode: string;  // ✅ Dynamic role code
  isActive: boolean;
  members: TenantMemberDetails[];
  invitations: TenantInvitationWeb[];
}
```

---

### **2. Type Definitions (project.types.ts)**

#### ❌ Before:
```typescript
export const ProjectRole = {
  Admin: 0,
  Editor: 1,
  Viewer: 2,
  Member: 3,
} as const;

export interface ProjectMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: number;  // ❌ Enum number
  joinedAt: string;
}

export interface ProjectDetailsWeb {
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  userRole: number;  // ❌ Enum number
  membersCount: number;
}
```

#### ✅ After:
```typescript
export interface ProjectMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;  // ✅ Dynamic role code
  joinedAt: string;
}

export interface ProjectDetailsWeb {
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  userRoleCode: string;  // ✅ Dynamic role code
  membersCount: number;
}
```

---

### **3. UserDetailsWeb Interface**

#### ❌ Before:
```typescript
export interface UserProfile {
  id?: string;
  email: string;
  firstName: string;
  lastName: string;
  activeTenantId?: string | null;
  activeTenantPermissions: string[];
  projectRoles: Record<string, number>;  // ❌ Enum numbers
  projectPermissions: Record<string, string[]>;
}
```

#### ✅ After:
```typescript
export interface UserProfile {
  id?: string;
  email: string;
  firstName: string;
  lastName: string;
  activeTenantId?: string | null;
  activeTenantPermissions: string[];
  projectRoleCodes: Record<string, string>;  // ✅ Role code strings
  projectPermissions: Record<string, string[]>;
}
```

---

### **4. Helper Functions - Replace Role Checks with Permission Checks**

#### ❌ Before (Role-based):
```typescript
export const isTenantAdmin = (userRole: number): boolean => {
  return userRole === TenantRole.Admin;
};

export const canEditTenant = (userRole: number): boolean => {
  return hasTenantRoleLevel(userRole, TenantRole.Editor);
};

// Usage in component
if (isTenantAdmin(tenant.role)) {
  // Show admin UI
}
```

#### ✅ After (Permission-based):
```typescript
// Check by RoleCode
export const isTenantAdmin = (roleCode: string): boolean => {
  return roleCode === RoleCodes.TENANT_ADMIN;
};

// BETTER: Check by Permission (recommended)
export const hasPermission = (
  permissions: string[], 
  requiredPermission: string
): boolean => {
  return permissions.includes(requiredPermission);
};

// Usage in component
if (hasPermission(user.activeTenantPermissions, "TENANT.EDIT")) {
  // Show edit UI
}
```

---

### **5. Display Functions - Role Name Mapping**

#### ❌ Before:
```typescript
export const getTenantRoleName = (role: number): string => {
  switch (role) {
    case TenantRole.Admin: return 'Administrator';
    case TenantRole.Member: return 'Członek';
    case TenantRole.Editor: return 'Edytor';
    case TenantRole.Viewer: return 'Przeglądający';
    default: return 'Nieznana rola';
  }
};
```

#### ✅ After:
```typescript
export const getRoleName = (roleCode: string): string => {
  // Map backend role codes to Polish names
  const roleNames: Record<string, string> = {
    [RoleCodes.TENANT_ADMIN]: 'Administrator',
    [RoleCodes.TENANT_MEMBER]: 'Członek',
    [RoleCodes.PROJECT_ADMIN]: 'Administrator',
    [RoleCodes.PROJECT_EDITOR]: 'Edytor',
    [RoleCodes.PROJECT_COLLABORATOR]: 'Współpracownik',
    [RoleCodes.PROJECT_VIEWER]: 'Przeglądający',
    [RoleCodes.PROJECT_MEMBER]: 'Członek',
  };
  
  return roleNames[roleCode] || 'Nieznana rola';
};

export const getRoleColor = (roleCode: string): string => {
  const roleColors: Record<string, string> = {
    [RoleCodes.TENANT_ADMIN]: 'purple',
    [RoleCodes.TENANT_MEMBER]: 'gray',
    [RoleCodes.PROJECT_ADMIN]: 'purple',
    [RoleCodes.PROJECT_EDITOR]: 'blue',
    [RoleCodes.PROJECT_COLLABORATOR]: 'green',
    [RoleCodes.PROJECT_VIEWER]: 'teal',
    [RoleCodes.PROJECT_MEMBER]: 'gray',
  };
  
  return roleColors[roleCode] || 'gray';
};
```

---

### **6. API Calls - Update Request/Response Types**

#### ❌ Before:
```typescript
// Update tenant member role
export const updateTenantMemberRole = async (
  tenantId: string,
  userId: string,
  role: number  // ❌ Enum number
) => {
  return axiosClient.patch(
    `/api/tenants/${tenantId}/members/${userId}/role`,
    { role }
  );
};
```

#### ✅ After:
```typescript
// Update tenant member role
export const updateTenantMemberRole = async (
  tenantId: string,
  userId: string,
  roleId: string  // ✅ RoleId (Guid)
) => {
  return axiosClient.patch(
    `/api/tenants/${tenantId}/members/${userId}/role`,
    { roleId }
  );
};

// You'll need to get available roles first:
export const getAvailableRoles = async (
  scope: 'tenant' | 'project'
) => {
  // This endpoint would need to be created if not exists
  return axiosClient.get(`/api/roles?scope=${scope}`);
};
```

---

### **7. Component Updates**

#### ❌ Before:
```tsx
// ProjectDetails.tsx
const userIsProjectAdmin = project && isProjectAdmin(project.userRole);
const userCanEdit = project && canEditProject(project.userRole);

<Badge colorScheme={getProjectRoleColor(project.userRole)}>
  {getProjectRoleName(project.userRole)}
</Badge>
```

#### ✅ After:
```tsx
// ProjectDetails.tsx
const userIsProjectAdmin = user.projectPermissions[projectId]?.includes("PROJECT.EDIT");
const userCanEdit = user.projectPermissions[projectId]?.includes("PROJECT.RESOURCES.WRITE");

<Badge colorScheme={getRoleColor(project.userRoleCode)}>
  {getRoleName(project.userRoleCode)}
</Badge>
```

---

## 📝 Migration Checklist

### **Phase 1: Type Definitions**
- [ ] Update `auth.types.ts` - replace `TenantRole` enum with `RoleCodes` constants
- [ ] Update `project.types.ts` - replace `ProjectRole` enum with `RoleCodes` constants
- [ ] Update all interfaces: `role: number` → `roleCode: string`
- [ ] Update `UserProfile`: `projectRoles` → `projectRoleCodes`

### **Phase 2: Helper Functions**
- [ ] Replace `getTenantRoleName(role: number)` with `getRoleName(roleCode: string)`
- [ ] Replace `getProjectRoleName(role: number)` with `getRoleName(roleCode: string)`
- [ ] Replace `getTenantRoleColor(role: number)` with `getRoleColor(roleCode: string)`
- [ ] Replace `getProjectRoleColor(role: number)` with `getRoleColor(roleCode: string)`
- [ ] **RECOMMENDED**: Replace role checks with permission checks

### **Phase 3: API Calls**
- [ ] Update `tenantApi.ts` - change `role: number` to `roleId: string`
- [ ] Update `projectApi.ts` - change `role: number` to `roleId: string`
- [ ] Add endpoint to fetch available roles (if needed for dropdowns)

### **Phase 4: Components**
- [ ] Update `TenantDetails.tsx` - use `roleCode` and permissions
- [ ] Update `ProjectDetails.tsx` - use `userRoleCode` and permissions
- [ ] Update `ProjectMembers.tsx` - use `roleCode`
- [ ] Update `TenantMembers.tsx` - use `roleCode`
- [ ] Update any role selection dropdowns to fetch from API

### **Phase 5: Testing**
- [ ] Test login and user details loading
- [ ] Test tenant member management
- [ ] Test project member management
- [ ] Test permission-based UI rendering
- [ ] Test role display badges

---

## 🎯 Recommended Approach: Permission-Based UI

Instead of checking roles, **check permissions directly**:

```typescript
// ❌ OLD: Role-based
if (isProjectAdmin(project.userRole)) {
  showAdminButton();
}

// ✅ NEW: Permission-based
if (user.projectPermissions[projectId]?.includes("PROJECT.MEMBERS.MANAGE")) {
  showManageMembersButton();
}

if (user.projectPermissions[projectId]?.includes("PROJECT.RESOURCES.WRITE")) {
  showUploadButton();
}

if (user.projectPermissions[projectId]?.includes("PROJECT.RESOURCES.WRITE_SHARED")) {
  showEditSharedFilesButton();
}
```

**Benefits:**
- More flexible - new roles automatically work
- Clearer intent - shows exactly what permission is needed
- Backend-driven - no hardcoded role hierarchy in frontend
- Easier to maintain - permissions match backend exactly

---

## 🚀 Implementation Steps

### **Step 1: Create RoleCodes constant**
```typescript
// src/constants/roleCodes.ts
export const RoleCodes = {
  TENANT_ADMIN: "TENANT.ADMIN",
  TENANT_MEMBER: "TENANT.MEMBER",
  PROJECT_ADMIN: "PROJECT.ADMIN",
  PROJECT_EDITOR: "PROJECT.EDITOR",
  PROJECT_COLLABORATOR: "PROJECT.COLLABORATOR",
  PROJECT_VIEWER: "PROJECT.VIEWER",
  PROJECT_MEMBER: "PROJECT.MEMBER",
} as const;

export type RoleCode = typeof RoleCodes[keyof typeof RoleCodes];
```

### **Step 2: Update types systematically**
Start with `auth.types.ts` and `project.types.ts`, then propagate changes through the app.

### **Step 3: Create migration helper**
```typescript
// Temporary helper for gradual migration
export const migrateRoleToRoleCode = (role: number): string => {
  const mapping: Record<number, string> = {
    0: RoleCodes.PROJECT_ADMIN,
    1: RoleCodes.PROJECT_EDITOR,
    2: RoleCodes.PROJECT_VIEWER,
    3: RoleCodes.PROJECT_MEMBER,
  };
  return mapping[role] || RoleCodes.PROJECT_MEMBER;
};
```

### **Step 4: Update components one by one**
Work through each component, testing as you go.

### **Step 5: Remove old enums**
Once all components are migrated, remove `TenantRole` and `ProjectRole` enums entirely.

---

## ⚠️ Common Pitfalls

1. **Forgetting to update API request bodies** - `role: number` → `roleId: string`
2. **Mixing role checks and permission checks** - Choose one approach and stick to it
3. **Hardcoded role names** - Use `getRoleName()` helper instead
4. **Not handling unknown role codes** - Always have a fallback in helpers
5. **Missing permission checks** - Some operations might not have corresponding permissions yet

---

## 📖 Additional Resources

- Backend RoleCodes: `src\Business\Interfaces\Constants\RoleCodes.cs`
- Backend PermissionCodes: `src\Business\Interfaces\Constants\PermissionCodes.cs`
- UserDetailsWeb docs: `docs\UserDetailsWeb-Permissions.md`
- Security fix docs: `docs\Security-Fix-Instance-Cache-Removal.md`

---

**Migration Priority:** HIGH  
**Estimated Effort:** 4-6 hours  
**Risk Level:** Medium (breaking changes, but well-defined)

