# Frontend Migration - Step-by-Step Implementation Guide

## 🎯 Goal
Migrate frontend from role enum-based system to dynamic role code + permission-based system to match the new backend API.

---

## 📋 Prerequisites

Before starting:
- [ ] Backend refactor is complete and deployed
- [ ] You have access to backend API documentation
- [ ] You understand the new permission-based authorization system

---

## 🚀 Step-by-Step Implementation

### **Phase 1: Setup New Constants (15 min)**

#### Step 1.1: Create `roleCodes.ts`
```bash
# Create new constants file
touch src/constants/roleCodes.ts
```

Copy content from `docs/frontend-examples/roleCodes.ts`

#### Step 1.2: Update imports in existing files
Find and replace across codebase:
```typescript
// Old
import { ProjectRole, TenantRole } from '../types/...';

// New
import { RoleCodes, PermissionCodes, getRoleName, getRoleColor } from '../constants/roleCodes';
```

---

### **Phase 2: Update Type Definitions (30 min)**

#### Step 2.1: Update `auth.types.ts`
```bash
# Backup old file
cp src/types/auth.types.ts src/types/auth.types.OLD.ts

# Replace with new version
# Use content from docs/frontend-examples/auth.types.new.ts
```

**Key changes:**
- `role: number` → `roleCode: string` in `TenantMemberDetails`
- `role: number` → `roleCode: string` in `TenantDetails`
- Add `activeTenantPermissions: string[]` to `UserProfile`
- `projectRoles` → `projectRoleCodes` in `UserProfile`
- Add `projectPermissions: Record<string, string[]>` to `UserProfile`

#### Step 2.2: Update `project.types.ts`
```bash
# Backup old file
cp src/types/project.types.ts src/types/project.types.OLD.ts

# Replace with new version
# Use content from docs/frontend-examples/project.types.new.ts
```

**Key changes:**
- `role: number` → `roleCode: string` in `ProjectMemberWeb`
- `userRole: number` → `userRoleCode: string` in `ProjectDetailsWeb`

#### Step 2.3: Run TypeScript compiler to find all breaking changes
```bash
npm run tsc --noEmit
```

This will show you all places where types are incompatible.

---

### **Phase 3: Update API Calls (45 min)**

#### Step 3.1: Update `tenantApi.ts`

**Find and replace:**
```typescript
// ❌ OLD
export const updateTenantMemberRole = async (
  tenantId: string,
  userId: string,
  role: number  // ❌
) => {
  return axiosClient.patch(
    `/api/tenants/${tenantId}/members/${userId}/role`,
    { role }
  );
};

// ✅ NEW
export const updateTenantMemberRole = async (
  tenantId: string,
  userId: string,
  roleId: string  // ✅
) => {
  return axiosClient.patch(
    `/api/tenants/${tenantId}/members/${userId}/role`,
    { roleId }
  );
};
```

#### Step 3.2: Update `projectApi.ts`

**Find and replace:**
```typescript
// ❌ OLD
export const updateProjectMemberRole = async (
  tenantId: string,
  projectId: string,
  userId: string,
  role: number  // ❌
) => {
  return axiosClient.patch(
    `/api/tenants/${tenantId}/projects/${projectId}/members/${userId}/role`,
    { role }
  );
};

// ✅ NEW
export const updateProjectMemberRole = async (
  tenantId: string,
  projectId: string,
  userId: string,
  roleId: string  // ✅
) => {
  return axiosClient.patch(
    `/api/tenants/${tenantId}/projects/${projectId}/members/${userId}/role`,
    { roleId }
  );
};
```

#### Step 3.3: Add new API endpoint for fetching available roles

**GOOD NEWS:** Backend endpoints are already implemented! ✅

```typescript
// src/api/roleApi.ts (NEW FILE)
import { axiosClient } from './axiosClient';

export interface RoleWeb {
  id: string;
  code: string;
  name: string;
  description?: string;
  scope: 'Tenant' | 'Project';
}

export const roleApi = {
  /**
   * Get available roles for a specific scope
   */
  getAvailableRoles: async (scope: 'tenant' | 'project'): Promise<RoleWeb[]> => {
    const scopeValue = scope === 'tenant' ? 0 : 1;  // RoleScope enum
    const response = await axiosClient.get('/api/roles', {
      params: { scope: scopeValue }
    });
    return response.data;
  },
  
  /**
   * Get tenant roles (convenience method)
   */
  getTenantRoles: async (): Promise<RoleWeb[]> => {
    const response = await axiosClient.get('/api/roles/tenant');
    return response.data;
  },
  
  /**
   * Get project roles (convenience method)
   */
  getProjectRoles: async (): Promise<RoleWeb[]> => {
    const response = await axiosClient.get('/api/roles/project');
    return response.data;
  },
};
```

**Backend Endpoints:**
- `GET /api/roles?scope={0|1}` - Get all roles for scope (0=Tenant, 1=Project)
- `GET /api/roles/tenant` - Get tenant roles only
- `GET /api/roles/project` - Get project roles only

**Full integration guide:** `docs/frontend-examples/role-api-integration.md`

---

### **Phase 4: Update Components (2-3 hours)**

Work through components systematically. Use VSCode search to find all usages.

#### Step 4.1: Update display components (badges, tables)

**Search for:**
- `getProjectRoleName(member.role)`
- `getTenantRoleName(tenant.role)`
- `getProjectRoleColor(member.role)`
- `getTenantRoleColor(tenant.role)`

**Replace with:**
- `getRoleName(member.roleCode)`
- `getRoleName(tenant.roleCode)`
- `getRoleColor(member.roleCode)`
- `getRoleColor(tenant.roleCode)`

**Example files to update:**
- `src/pages/ProjectMembers.tsx`
- `src/pages/TenantMembers.tsx`
- `src/components/MembersList.tsx`
- `src/pages/ProjectDetails.tsx`
- `src/pages/TenantDetails.tsx`

#### Step 4.2: Update permission checks

**Search for:**
- `isProjectAdmin(project.userRole)`
- `isTenantAdmin(tenant.role)`
- `canEditProject(project.userRole)`

**Replace with permission checks:**
```typescript
// Instead of:
if (isProjectAdmin(project.userRole)) { ... }

// Use:
const { user } = useAuth();
const projectPermissions = user.projectPermissions[project.id] || [];
if (hasPermission(projectPermissions, PermissionCodes.PROJECT_MEMBERS_MANAGE)) { ... }

// Or create custom hook:
const permissions = useProjectPermissions(project.id);
if (permissions.canManageMembers) { ... }
```

**Example files to update:**
- `src/pages/ProjectDetails.tsx`
- `src/pages/TenantDetails.tsx`
- `src/components/ProjectActions.tsx`
- `src/components/TenantActions.tsx`

#### Step 4.3: Update role selection dropdowns

**Find components with role selection:**
```bash
grep -r "ProjectRole.Admin\|TenantRole.Admin" src/
```

**Replace hardcoded dropdowns with dynamic role fetching:**

```typescript
// ❌ OLD
<Select value={member.role} onChange={...}>
  <option value={ProjectRole.Admin}>Administrator</option>
  <option value={ProjectRole.Editor}>Edytor</option>
</Select>

// ✅ NEW
import { roleApi } from '../api/roleApi';
import { useQuery } from '@tanstack/react-query';

const { data: availableRoles } = useQuery({
  queryKey: ['roles', 'project'],
  queryFn: () => roleApi.getAvailableRoles('project'),
});

<Select value={currentRoleId} onChange={(e) => handleRoleChange(e.target.value)}>
  {availableRoles?.map(role => (
    <option key={role.id} value={role.id}>
      {role.name}
    </option>
  ))}
</Select>
```

**Files likely needing updates:**
- `src/components/AddProjectMemberModal.tsx`
- `src/components/AddTenantMemberModal.tsx`
- `src/components/EditMemberRoleModal.tsx`

#### Step 4.4: Update role change handlers

**Find:**
```bash
grep -r "updateTenantMemberRole\|updateProjectMemberRole" src/
```

**Update handler:**
```typescript
// ❌ OLD
const handleRoleChange = async (userId: string, newRole: number) => {
  await updateProjectMemberRole(tenantId, projectId, userId, newRole);
};

// ✅ NEW
const handleRoleChange = async (userId: string, newRoleId: string) => {
  await updateProjectMemberRole(tenantId, projectId, userId, newRoleId);
};
```

---

### **Phase 5: Create Custom Hooks (30 min)**

#### Step 5.1: Create `useProjectPermissions` hook

```typescript
// src/hooks/useProjectPermissions.ts
import { useAuth } from '../context/AuthContext';
import { hasPermission, PermissionCodes } from '../constants/roleCodes';

export function useProjectPermissions(projectId: string) {
  const { user } = useAuth();
  const permissions = user.projectPermissions?.[projectId] || [];
  
  return {
    canView: hasPermission(permissions, PermissionCodes.PROJECT_VIEW),
    canEdit: hasPermission(permissions, PermissionCodes.PROJECT_EDIT),
    canManageMembers: hasPermission(permissions, PermissionCodes.PROJECT_MEMBERS_MANAGE),
    canUploadFiles: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE),
    canViewSharedFiles: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_SHARED),
    canEditSharedFiles: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED),
    allPermissions: permissions,
  };
}
```

#### Step 5.2: Create `useTenantPermissions` hook

```typescript
// src/hooks/useTenantPermissions.ts
import { useAuth } from '../context/AuthContext';
import { hasPermission, PermissionCodes } from '../constants/roleCodes';

export function useTenantPermissions() {
  const { user } = useAuth();
  const permissions = user.activeTenantPermissions || [];
  
  return {
    canView: hasPermission(permissions, PermissionCodes.TENANT_VIEW),
    canEdit: hasPermission(permissions, PermissionCodes.TENANT_EDIT),
    canManageMembers: hasPermission(permissions, PermissionCodes.TENANT_MEMBERS_MANAGE),
    canCreateProjects: hasPermission(permissions, PermissionCodes.TENANT_PROJECT_CREATE),
    canManageStatus: hasPermission(permissions, PermissionCodes.TENANT_STATUS_MANAGE),
    allPermissions: permissions,
  };
}
```

#### Step 5.3: Use hooks in components

```typescript
// Before
const userCanEdit = canEditProject(project.userRole);

// After
const permissions = useProjectPermissions(project.id);
const userCanEdit = permissions.canEdit;
```

---

### **Phase 6: Testing (1-2 hours)**

#### Step 6.1: Test role display
- [ ] Login as different users (admin, editor, viewer)
- [ ] Verify correct role badges are displayed
- [ ] Check role names are in Polish and correct

#### Step 6.2: Test permission-based UI
- [ ] Login as admin - verify all buttons visible
- [ ] Login as editor - verify limited buttons
- [ ] Login as viewer - verify read-only UI

#### Step 6.3: Test role changes
- [ ] Change tenant member role
- [ ] Change project member role
- [ ] Verify permissions update immediately (may need to refresh permissions)

#### Step 6.4: Test edge cases
- [ ] User with no active tenant
- [ ] User not member of project
- [ ] Inactive project/tenant access
- [ ] Unknown role codes (fallback behavior)

---

### **Phase 7: Cleanup (30 min)**

#### Step 7.1: Remove deprecated code

**Search and remove:**
```bash
# Find all @deprecated usages
grep -r "@deprecated" src/

# Find old enum usages
grep -r "ProjectRole.Admin\|TenantRole.Admin" src/
```

#### Step 7.2: Remove old backup files
```bash
rm src/types/auth.types.OLD.ts
rm src/types/project.types.OLD.ts
```

#### Step 7.3: Update documentation
- [ ] Update README with new permission system
- [ ] Document available permissions
- [ ] Add examples of permission checks

---

## 🔍 Verification Checklist

### Code Quality
- [ ] No TypeScript errors (`npm run tsc`)
- [ ] No ESLint errors (`npm run lint`)
- [ ] All tests pass (`npm run test`)
- [ ] No console warnings in browser

### Functionality
- [ ] Login works
- [ ] User details load with permissions
- [ ] Tenant list displays with correct roles
- [ ] Project list displays with correct roles
- [ ] Member lists show correct roles
- [ ] Role changes work
- [ ] Permission-based UI renders correctly

### UI/UX
- [ ] Role badges display correct colors
- [ ] Role names in Polish
- [ ] Buttons show/hide based on permissions
- [ ] No flickering during permission checks
- [ ] Loading states handled

---

## 🐛 Troubleshooting

### **Issue: "roleCode is undefined"**
**Cause:** API response still returning `role: number`  
**Fix:** Verify backend is deployed and returning new format

### **Issue: "Permission checks always fail"**
**Cause:** `user.projectPermissions` or `user.activeTenantPermissions` is empty  
**Fix:** Check UserDetails API response, verify permissions are included

### **Issue: "Role dropdown is empty"**
**Cause:** Role API endpoint not implemented  
**Fix:** Implement backend endpoint or use static role list temporarily

### **Issue: "Old enum errors in console"**
**Cause:** Some components still using old enums  
**Fix:** Search codebase for `ProjectRole.` and `TenantRole.` usages

---

## 📚 Additional Resources

- Migration guide: `docs/Frontend-Migration-RoleCode.md`
- Type examples: `docs/frontend-examples/`
- Component examples: `docs/frontend-examples/component-migration-examples.tsx`
- Backend RoleCodes: `src/Business/Interfaces/Constants/RoleCodes.cs`
- Backend PermissionCodes: `src/Business/Interfaces/Constants/PermissionCodes.cs`

---

## ⏱️ Estimated Timeline

| Phase | Time | Priority |
|-------|------|----------|
| Setup Constants | 15 min | High |
| Update Types | 30 min | High |
| Update API Calls | 45 min | High |
| Update Components | 2-3 hours | High |
| Create Hooks | 30 min | Medium |
| Testing | 1-2 hours | High |
| Cleanup | 30 min | Low |
| **Total** | **5-7 hours** | |

---

Good luck with the migration! 🚀
