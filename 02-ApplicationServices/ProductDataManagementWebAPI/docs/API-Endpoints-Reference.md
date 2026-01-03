# API Endpoints Reference - Role Management

## 🎯 New Role Endpoints (Backend Ready)

### **GET /api/roles**
Get all available roles for a specific scope.

**Authorization:** Requires `ROLE.LIST` permission (Tenant Admin or Project Admin)

**Request:**
```http
GET /api/roles?scope=0
Authorization: Bearer {token}
```

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| scope | int | Yes | 0 = Tenant, 1 = Project |

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "code": "TENANT.ADMIN",
    "name": "Administrator",
    "description": "Full tenant access",
    "scope": "Tenant"
  }
]
```

**Error Responses:**
- `400 Bad Request` - Invalid scope value
- `403 Forbidden` - User does not have ROLE.LIST permission

---

### **GET /api/roles/tenant**
Get all available tenant roles.

**Authorization:** Requires `ROLE.LIST` permission (Tenant Admin or Project Admin)

**Request:**
```http
GET /api/roles/tenant
Authorization: Bearer {token}
```

**Response:** `200 OK`
```json
[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "code": "TENANT.ADMIN",
    "name": "Administrator",
    "description": "Full access to tenant",
    "scope": "Tenant"
  },
  {
    "id": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
    "code": "TENANT.MEMBER",
    "name": "Członek",
    "description": "Basic member",
    "scope": "Tenant"
  }
]
```

**Error Responses:**
- `403 Forbidden` - User does not have ROLE.LIST permission

---

### **GET /api/roles/project**
Get all available project roles.

**Authorization:** Requires `ROLE.LIST` permission (Tenant Admin or Project Admin)

**Request:**
```http
GET /api/roles/project
Authorization: Bearer {token}
```

**Response:** `200 OK`
```json
[
  {
    "id": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
    "code": "PROJECT.ADMIN",
    "name": "Administrator",
    "description": "Full project admin",
    "scope": "Project"
  },
  {
    "id": "d4e5f6g7-h8i9-0123-defg-hi4567890123",
    "code": "PROJECT.EDITOR",
    "name": "Edytor",
    "description": "Can edit project",
    "scope": "Project"
  },
  {
    "id": "e5f6g7h8-i9j0-1234-efgh-ij5678901234",
    "code": "PROJECT.COLLABORATOR",
    "name": "Współpracownik",
    "description": "Can collaborate",
    "scope": "Project"
  },
  {
    "id": "f6g7h8i9-j0k1-2345-fghi-jk6789012345",
    "code": "PROJECT.VIEWER",
    "name": "Przeglądający",
    "description": "Read-only",
    "scope": "Project"
  }
]
```

**Error Responses:**
- `403 Forbidden` - User does not have ROLE.LIST permission

---

## 🔄 Updated Role Change Endpoints

### **PATCH /api/tenants/{tenantId}/members/{userId}/role**
Update tenant member role.

**Request:**
```http
PATCH /api/tenants/{tenantId}/members/{userId}/role
Authorization: Bearer {token}
Content-Type: application/json

{
  "roleId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Body Schema:**
```typescript
{
  roleId: string;  // ✅ NEW: Guid of the role (was: role: number)
}
```

**Response:** `204 No Content`

---

### **PATCH /api/tenants/{tenantId}/projects/{projectId}/members/{userId}/role**
Update project member role.

**Request:**
```http
PATCH /api/tenants/{tenantId}/projects/{projectId}/members/{userId}/role
Authorization: Bearer {token}
Content-Type: application/json

{
  "roleId": "c3d4e5f6-g7h8-9012-cdef-gh3456789012"
}
```

**Body Schema:**
```typescript
{
  roleId: string;  // ✅ NEW: Guid of the role (was: role: number)
}
```

**Response:** `204 No Content`

---

## 📊 Updated Response Schemas

### **TenantDetailsWeb** (UPDATED)
```typescript
{
  id: string;
  name: string;
  createdAt: string;
  roleCode: string;  // ✅ NEW: e.g., "TENANT.ADMIN" (was: role: number)
  isActive: boolean;
  members: TenantMemberWeb[];
  invitations: TenantInvitationWeb[];
}
```

### **TenantMemberWeb** (UPDATED)
```typescript
{
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;  // ✅ NEW: e.g., "TENANT.ADMIN" (was: role: number)
  isActive: boolean;
  joinedAt: string;
}
```

### **ProjectDetailsWeb** (UPDATED)
```typescript
{
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  userRoleCode: string;  // ✅ NEW: e.g., "PROJECT.ADMIN" (was: userRole: number)
  membersCount: number;
}
```

### **ProjectMemberWeb** (UPDATED)
```typescript
{
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;  // ✅ NEW: e.g., "PROJECT.EDITOR" (was: role: number)
  joinedAt: string;
}
```

### **UserDetailsWeb** (UPDATED)
```typescript
{
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  activeTenantId?: string | null;
  activeTenantPermissions: string[];  // ✅ NEW: e.g., ["TENANT.VIEW", "TENANT.EDIT"]
  projectRoleCodes: Record<string, string>;  // ✅ NEW: { projectId: "PROJECT.ADMIN" } (was: projectRoles)
  projectPermissions: Record<string, string[]>;  // ✅ NEW: { projectId: ["PROJECT.VIEW", ...] }
}
```

### **RoleWeb** (NEW)
```typescript
{
  id: string;
  code: string;  // e.g., "TENANT.ADMIN", "PROJECT.EDITOR"
  name: string;  // Display name
  description?: string;
  scope: "Tenant" | "Project";
}
```

---

## 🗂️ Role Codes Reference

### **Tenant Roles**
| Code | Name | Description |
|------|------|-------------|
| `TENANT.ADMIN` | Administrator | Full tenant management |
| `TENANT.MEMBER` | Członek | Basic member access |

### **Project Roles**
| Code | Name | Description |
|------|------|-------------|
| `PROJECT.ADMIN` | Administrator | Full project admin |
| `PROJECT.EDITOR` | Edytor | Can edit project |
| `PROJECT.COLLABORATOR` | Współpracownik | Can collaborate |
| `PROJECT.VIEWER` | Przeglądający | Read-only access |
| `PROJECT.MEMBER` | Członek | Basic member (deprecated, use VIEWER) |

---

## 🔑 Permission Codes (Common)

### **Global Permissions**
```typescript
TENANT.LIST.AVAILABLE    // List available tenants for switcher
ROLE.LIST                // List available roles (Tenant/Project Admin only)
```

### **Tenant Permissions**
```typescript
TENANT.VIEW              // View tenant details
TENANT.EDIT              // Edit tenant settings
TENANT.MEMBERS.MANAGE    // Add/remove members
TENANT.STATUS.MANAGE     // Activate/deactivate tenant
TENANT.PROJECT.CREATE    // Create new projects
```

### **Project Permissions**
```typescript
PROJECT.VIEW                    // View project
PROJECT.EDIT                    // Edit project settings
PROJECT.MEMBERS.VIEW            // View members
PROJECT.MEMBERS.MANAGE          // Add/remove members
PROJECT.STATUS.MANAGE           // Activate/deactivate
PROJECT.RESOURCES.READ          // Read own files
PROJECT.RESOURCES.WRITE         // Upload/edit own files
PROJECT.RESOURCES.READ_SHARED   // Read shared files
PROJECT.RESOURCES.WRITE_SHARED  // Edit shared files
```

---

## 📝 Frontend TypeScript Types

```typescript
// Role API Response Type
export interface RoleWeb {
  id: string;
  code: string;
  name: string;
  description?: string;
  scope: 'Tenant' | 'Project';
}

// Update Role Request Type
export interface UpdateMemberRoleRequest {
  roleId: string;  // Guid of the role
}

// Role Codes Constants
export const RoleCodes = {
  TENANT_ADMIN: "TENANT.ADMIN",
  TENANT_MEMBER: "TENANT.MEMBER",
  PROJECT_ADMIN: "PROJECT.ADMIN",
  PROJECT_EDITOR: "PROJECT.EDITOR",
  PROJECT_COLLABORATOR: "PROJECT.COLLABORATOR",
  PROJECT_VIEWER: "PROJECT.VIEWER",
  PROJECT_MEMBER: "PROJECT.MEMBER",
} as const;

// Permission Codes Constants
export const PermissionCodes = {
  TENANT_VIEW: "TENANT.VIEW",
  TENANT_EDIT: "TENANT.EDIT",
  TENANT_MEMBERS_MANAGE: "TENANT.MEMBERS.MANAGE",
  PROJECT_VIEW: "PROJECT.VIEW",
  PROJECT_EDIT: "PROJECT.EDIT",
  PROJECT_RESOURCES_WRITE: "PROJECT.RESOURCES.WRITE",
  // ... etc
} as const;
```

---

## 🚀 Quick Test with cURL

### **Get Tenant Roles**
```bash
curl -X GET "https://your-api.com/api/roles/tenant" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### **Get Project Roles**
```bash
curl -X GET "https://your-api.com/api/roles/project" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### **Update Tenant Member Role**
```bash
curl -X PATCH "https://your-api.com/api/tenants/{tenantId}/members/{userId}/role" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"roleId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"}'
```

### **Update Project Member Role**
```bash
curl -X PATCH "https://your-api.com/api/tenants/{tenantId}/projects/{projectId}/members/{userId}/role" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"roleId": "c3d4e5f6-g7h8-9012-cdef-gh3456789012"}'
```

---

## ✅ Migration Summary

| Endpoint | Old Behavior | New Behavior |
|----------|--------------|--------------|
| Get Roles | ❌ Hardcoded enum list | ✅ Dynamic from database |
| Update Role | Request: `role: number` | Request: `roleId: string` (Guid) |
| Role Display | Response: `role: number` | Response: `roleCode: string` |
| User Details | `projectRoles: Record<string, number>` | `projectRoleCodes: Record<string, string>` |

**All endpoints are backward compatible in structure, only data types changed.**

---

**For detailed integration guide, see:** `docs/frontend-examples/role-api-integration.md`
