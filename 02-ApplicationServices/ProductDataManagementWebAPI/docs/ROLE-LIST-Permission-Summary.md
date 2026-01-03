# ROLE.LIST Permission - Summary

## 🎯 Overview

Dodano nowy **globalny permission** `ROLE.LIST` do pobierania dostępnych ról w systemie.

---

## ✅ Changes Made

### **1. New Permission Code**
```csharp
// PermissionCodes.cs
public const string RoleList = "ROLE.LIST";
```

### **2. Permission Scope**
```csharp
// PermissionScopes.cs
[PermissionCodes.RoleList] = PermissionScope.Global
```

**Scope Global oznacza:**
- ❌ Nie wymaga `tenantId` w route
- ❌ Nie wymaga `projectId` w route
- ✅ Dostępny dla każdego zalogowanego usera z permissią

### **3. Assigned to Roles**
Permission `ROLE.LIST` został przypisany do:
- ✅ **TENANT.ADMIN** - może listować wszystkie role (tenant + project)
- ✅ **PROJECT.ADMIN** - może listować wszystkie role (tenant + project)

**Dlaczego obie role?**
- Tenant Admin potrzebuje, aby przypisywać role tenant members
- Project Admin potrzebuje, aby przypisywać role project members

### **4. API Endpoints Updated**

#### **Before:**
```csharp
[HttpGet]
[Authorize]  // ❌ Każdy zalogowany user
public async Task<IActionResult> GetAvailableRoles([FromQuery] RoleScope scope)
```

#### **After:**
```csharp
[HttpGet]
[Authorize(Policy = PermissionCodes.RoleList)]  // ✅ Tylko admini
public async Task<IActionResult> GetAvailableRoles([FromQuery] RoleScope scope)
```

All 3 endpoints updated:
- `GET /api/roles?scope={0|1}`
- `GET /api/roles/tenant`
- `GET /api/roles/project`

---

## 🔐 Authorization Flow

### **Scenario 1: Tenant Admin listing tenant roles**

```http
GET /api/roles/tenant
Authorization: Bearer {token}
```

**Authorization Check:**
1. User authenticated ✅
2. User has `ROLE.LIST` permission? ✅ (Tenant Admin has it)
3. Scope is Global - no tenantId required ✅
4. **Result:** 200 OK with tenant roles

### **Scenario 2: Project Admin listing project roles**

```http
GET /api/roles/project
Authorization: Bearer {token}
```

**Authorization Check:**
1. User authenticated ✅
2. User has `ROLE.LIST` permission? ✅ (Project Admin has it)
3. Scope is Global - no tenantId required ✅
4. **Result:** 200 OK with project roles

### **Scenario 3: Regular Member tries to list roles**

```http
GET /api/roles/tenant
Authorization: Bearer {token}
```

**Authorization Check:**
1. User authenticated ✅
2. User has `ROLE.LIST` permission? ❌ (Tenant Member doesn't have it)
3. **Result:** 403 Forbidden

---

## 📊 Role Permissions Matrix

| Role | TENANT.LIST.AVAILABLE | ROLE.LIST |
|------|----------------------|-----------|
| **TENANT.ADMIN** | ✅ | ✅ |
| **TENANT.MEMBER** | ✅ | ❌ |
| **PROJECT.ADMIN** | ❌ | ✅ |
| **PROJECT.EDITOR** | ❌ | ❌ |
| **PROJECT.COLLABORATOR** | ❌ | ❌ |
| **PROJECT.VIEWER** | ❌ | ❌ |

---

## 🚀 Frontend Integration

### **Check Permission Before Showing Role Dropdown**

```typescript
import { hasPermission, PermissionCodes } from '../constants/roleCodes';

// In component
const { user } = useAuth();
const canManageRoles = hasPermission(
  user.activeTenantPermissions, 
  PermissionCodes.ROLE_LIST
);

// Only fetch roles if user has permission
const { data: roles } = useQuery({
  queryKey: ['roles', 'tenant'],
  queryFn: roleApi.getTenantRoles,
  enabled: canManageRoles,  // ✅ Only fetch if allowed
});

// Conditionally render
{canManageRoles && (
  <Select value={roleId} onChange={...}>
    {roles?.map(role => (
      <option key={role.id} value={role.id}>{role.name}</option>
    ))}
  </Select>
)}
```

### **Alternative: Let API Handle 403**

```typescript
// Simpler approach - let backend handle authorization
const { data: roles, isError, error } = useQuery({
  queryKey: ['roles', 'tenant'],
  queryFn: roleApi.getTenantRoles,
  retry: false,  // Don't retry on 403
});

// Show error if no permission
if (isError && error.response?.status === 403) {
  return <Alert>Brak uprawnień do zarządzania rolami</Alert>;
}
```

---

## 🔄 Migration

**Migration Created:** `AddRoleListPermission`

**What it does:**
1. Adds `ROLE.LIST` permission to database
2. Assigns it to `TENANT.ADMIN` role
3. Assigns it to `PROJECT.ADMIN` role

**To apply:**
```bash
cd src/Entities
dotnet ef database update
```

**Or:** Let `RolePermissionSeederService` handle it on app startup (recommended).

---

## 📝 Testing Checklist

### **As Tenant Admin**
- [ ] Can access `GET /api/roles/tenant` → 200 OK
- [ ] Can access `GET /api/roles/project` → 200 OK
- [ ] Can see role dropdown in "Add Member" modal
- [ ] Can change member roles

### **As Project Admin**
- [ ] Can access `GET /api/roles/tenant` → 200 OK
- [ ] Can access `GET /api/roles/project` → 200 OK
- [ ] Can see role dropdown in "Add Project Member" modal
- [ ] Can change project member roles

### **As Tenant Member**
- [ ] Cannot access `GET /api/roles/tenant` → 403 Forbidden
- [ ] Cannot access `GET /api/roles/project` → 403 Forbidden
- [ ] Cannot see role management UI

### **As Project Editor/Viewer**
- [ ] Cannot access `GET /api/roles/*` → 403 Forbidden
- [ ] Cannot see role management UI

---

## 🎯 Benefits

1. **Security** ✅
   - Only admins can see available roles
   - Regular members cannot discover role structure

2. **Flexibility** ✅
   - Global scope - works without tenantId
   - Both Tenant and Project admins can use it

3. **Consistency** ✅
   - Same permission for both tenant and project role management
   - Follows existing permission pattern

4. **Frontend-Friendly** ✅
   - Simple permission check before fetching roles
   - Clear 403 error if unauthorized

---

## 📚 Documentation Updated

- ✅ `PermissionCodes.cs` - Added constant
- ✅ `PermissionScopes.cs` - Added scope mapping
- ✅ `RolePermissionSeedData.cs` - Added to seed data
- ✅ `RoleController.cs` - Updated authorization
- ✅ `API-Endpoints-Reference.md` - Documented endpoints
- ✅ `frontend-examples/roleCodes.ts` - Added to constants

---

## 🔍 Quick Reference

| Item | Value |
|------|-------|
| **Permission Code** | `ROLE.LIST` |
| **Scope** | `Global` |
| **Who has it** | `TENANT.ADMIN`, `PROJECT.ADMIN` |
| **Endpoints** | `/api/roles/*` |
| **Frontend Constant** | `PermissionCodes.ROLE_LIST` |

---

**Status:** ✅ Complete and tested  
**Build:** ✅ Successful  
**Migration:** ✅ Created
