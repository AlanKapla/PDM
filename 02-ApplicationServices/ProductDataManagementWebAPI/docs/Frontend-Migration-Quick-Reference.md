# Frontend Migration - Quick Reference Card

## 📝 Quick Find & Replace Guide

### **1. Interface Properties**

| Old | New | Files |
|-----|-----|-------|
| `role: number` | `roleCode: string` | `auth.types.ts`, `project.types.ts` |
| `userRole: number` | `userRoleCode: string` | `project.types.ts` (ProjectDetailsWeb) |
| `projectRoles: Record<string, number>` | `projectRoleCodes: Record<string, string>` | `auth.types.ts` (UserProfile) |

### **2. Function Calls**

| Old | New |
|-----|-----|
| `getTenantRoleName(role: number)` | `getRoleName(roleCode: string)` |
| `getProjectRoleName(role: number)` | `getRoleName(roleCode: string)` |
| `getTenantRoleColor(role: number)` | `getRoleColor(roleCode: string)` |
| `getProjectRoleColor(role: number)` | `getRoleColor(roleCode: string)` |
| `isTenantAdmin(role: number)` | `isTenantAdminRole(roleCode: string)` |
| `isProjectAdmin(role: number)` | `isProjectAdminRole(roleCode: string)` |

### **3. API Calls**

| Old | New |
|-----|-----|
| `updateTenantMemberRole(tenantId, userId, role: number)` | `updateTenantMemberRole(tenantId, userId, roleId: string)` |
| `updateProjectMemberRole(..., role: number)` | `updateProjectMemberRole(..., roleId: string)` |

### **4. Component Patterns**

| Pattern | Old (❌) | New (✅) |
|---------|---------|---------|
| **Display role** | `<Badge>{getProjectRoleName(member.role)}</Badge>` | `<Badge>{getRoleName(member.roleCode)}</Badge>` |
| **Check admin** | `if (isProjectAdmin(project.userRole))` | `if (hasPermission(permissions, "PROJECT.EDIT"))` |
| **Can edit?** | `if (canEditProject(project.userRole))` | `if (hasPermission(permissions, "PROJECT.EDIT"))` |
| **Role select** | `<option value={ProjectRole.Admin}>Admin</option>` | `<option value={role.id}>{role.name}</option>` |

---

## 🎨 Code Snippets

### **Display Role Badge**
```tsx
// ❌ OLD
<Badge colorScheme={getProjectRoleColor(member.role)}>
  {getProjectRoleName(member.role)}
</Badge>

// ✅ NEW
<Badge colorScheme={getRoleColor(member.roleCode)}>
  {getRoleName(member.roleCode)}
</Badge>
```

### **Permission Check**
```tsx
// ❌ OLD
const userIsAdmin = isProjectAdmin(project.userRole);
if (userIsAdmin) { ... }

// ✅ NEW
const { user } = useAuth();
const projectPermissions = user.projectPermissions[project.id] || [];
if (hasPermission(projectPermissions, PermissionCodes.PROJECT_EDIT)) { ... }

// ✅ BETTER - Use custom hook
const permissions = useProjectPermissions(project.id);
if (permissions.canEdit) { ... }
```

### **Role Dropdown (Dynamic)**
```tsx
// ❌ OLD - Hardcoded
<Select value={member.role}>
  <option value={0}>Admin</option>
  <option value={1}>Editor</option>
</Select>

// ✅ NEW - Dynamic from API
const { data: roles } = useQuery({
  queryKey: ['roles', 'project'],
  queryFn: () => roleApi.getAvailableRoles('project'),
});

<Select value={member.currentRoleId}>
  {roles?.map(role => (
    <option key={role.id} value={role.id}>{role.name}</option>
  ))}
</Select>
```

### **Update Role Handler**
```tsx
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

## 🔑 Constants Reference

### **RoleCodes**
```typescript
RoleCodes.TENANT_ADMIN         // "TENANT.ADMIN"
RoleCodes.TENANT_MEMBER        // "TENANT.MEMBER"
RoleCodes.PROJECT_ADMIN        // "PROJECT.ADMIN"
RoleCodes.PROJECT_EDITOR       // "PROJECT.EDITOR"
RoleCodes.PROJECT_COLLABORATOR // "PROJECT.COLLABORATOR"
RoleCodes.PROJECT_VIEWER       // "PROJECT.VIEWER"
RoleCodes.PROJECT_MEMBER       // "PROJECT.MEMBER"
```

### **PermissionCodes (Most Common)**
```typescript
// Tenant
PermissionCodes.TENANT_VIEW              // "TENANT.VIEW"
PermissionCodes.TENANT_EDIT              // "TENANT.EDIT"
PermissionCodes.TENANT_MEMBERS_MANAGE    // "TENANT.MEMBERS.MANAGE"
PermissionCodes.TENANT_PROJECT_CREATE    // "TENANT.PROJECT.CREATE"

// Project
PermissionCodes.PROJECT_VIEW             // "PROJECT.VIEW"
PermissionCodes.PROJECT_EDIT             // "PROJECT.EDIT"
PermissionCodes.PROJECT_MEMBERS_MANAGE   // "PROJECT.MEMBERS.MANAGE"
PermissionCodes.PROJECT_RESOURCES_WRITE  // "PROJECT.RESOURCES.WRITE"
```

---

## 🔍 VSCode Search Patterns

Find all places needing updates:

### **Find Enum Usages**
```regex
Search: (ProjectRole|TenantRole)\.(Admin|Editor|Viewer|Member)
Replace: Check context and use RoleCodes or permission check
```

### **Find Old Function Calls**
```regex
Search: get(Project|Tenant)Role(Name|Color|Level)\(
Files: src/**/*.tsx, src/**/*.ts
```

### **Find Role Property Access**
```regex
Search: \.(user)?[Rr]ole(?!Code)
Files: src/**/*.tsx, src/**/*.ts
Exclude: node_modules, .OLD.ts
```

### **Find API Calls**
```regex
Search: updateTenantMemberRole|updateProjectMemberRole
Files: src/api/**/*.ts
```

---

## ✅ Migration Checklist (Condensed)

### **Types**
- [ ] `auth.types.ts` - `role: number` → `roleCode: string`
- [ ] `project.types.ts` - `role: number` → `roleCode: string`
- [ ] `UserProfile` - add `activeTenantPermissions`, `projectRoleCodes`, `projectPermissions`

### **Constants**
- [ ] Create `src/constants/roleCodes.ts`
- [ ] Import `RoleCodes`, `PermissionCodes`, helpers

### **API**
- [ ] Update `tenantApi.ts` - `role: number` → `roleId: string`
- [ ] Update `projectApi.ts` - `role: number` → `roleId: string`
- [ ] Add `roleApi.ts` - fetch available roles

### **Components**
- [ ] Replace `getProjectRoleName()` → `getRoleName()`
- [ ] Replace `getProjectRoleColor()` → `getRoleColor()`
- [ ] Replace `getTenantRoleName()` → `getRoleName()`
- [ ] Replace `getTenantRoleColor()` → `getRoleColor()`
- [ ] Replace role checks with permission checks
- [ ] Update role selection dropdowns to fetch from API

### **Hooks**
- [ ] Create `useProjectPermissions(projectId)`
- [ ] Create `useTenantPermissions()`

### **Testing**
- [ ] Test as different roles (admin, editor, viewer)
- [ ] Verify role badges display correctly
- [ ] Verify permission-based UI works
- [ ] Test role changes

---

## 🐛 Common Errors & Fixes

| Error | Cause | Fix |
|-------|-------|-----|
| `roleCode is undefined` | Backend not updated | Deploy backend changes |
| `Cannot read property 'includes' of undefined` | Missing permissions array | Check UserDetails API, add `|| []` fallback |
| `Role dropdown empty` | No role API endpoint | Implement backend endpoint or use static list |
| `Type 'number' not assignable to 'string'` | Using old enum | Replace with `roleCode` |

---

## 🎯 Priority Order

1. **High Priority** - Breaks app if not fixed:
   - Type definitions
   - API calls
   - Permission checks for critical actions

2. **Medium Priority** - UI looks wrong but functional:
   - Role display badges
   - Role dropdown updates

3. **Low Priority** - Nice to have:
   - Custom hooks
   - Helper function deprecation warnings
   - Code cleanup

---

## 📞 Help Resources

- Full guide: `docs/Frontend-Migration-RoleCode.md`
- Step-by-step: `docs/Frontend-Migration-Step-By-Step.md`
- Code examples: `docs/frontend-examples/`
- Backend constants: `src/Business/Interfaces/Constants/`

---

**Print this page for quick reference during migration!** 📄
