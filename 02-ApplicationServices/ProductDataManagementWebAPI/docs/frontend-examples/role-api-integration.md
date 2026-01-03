# Role API - Frontend Integration Guide

## 🎯 Overview

Backend provides endpoints to fetch available roles dynamically. This replaces the old hardcoded enum approach.

---

## 📡 API Endpoints

### **1. Get All Roles by Scope**
```http
GET /api/roles?scope={0|1}
Authorization: Bearer {token}
```

**Parameters:**
- `scope` (query, required): `0` = Tenant, `1` = Project

**Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "TENANT.ADMIN",
    "name": "Administrator",
    "description": "Full access to tenant",
    "scope": "Tenant"
  },
  {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "code": "TENANT.MEMBER",
    "name": "Członek",
    "description": "Basic tenant member",
    "scope": "Tenant"
  }
]
```

### **2. Get Tenant Roles (Convenience)**
```http
GET /api/roles/tenant
Authorization: Bearer {token}
```

**Response:** Same as above, filtered for Tenant scope.

### **3. Get Project Roles (Convenience)**
```http
GET /api/roles/project
Authorization: Bearer {token}
```

**Response:** Same structure, filtered for Project scope.

---

## 🔧 Frontend Implementation

### **Step 1: Create API Client**

```typescript
// src/api/roleApi.ts
import { axiosClient } from './axiosClient';

export interface RoleWeb {
  id: string;
  code: string;
  name: string;
  description?: string;
  scope: 'Tenant' | 'Project';
}

export const roleApi = {
  getTenantRoles: async (): Promise<RoleWeb[]> => {
    const response = await axiosClient.get('/api/roles/tenant');
    return response.data;
  },

  getProjectRoles: async (): Promise<RoleWeb[]> => {
    const response = await axiosClient.get('/api/roles/project');
    return response.data;
  },

  getAvailableRoles: async (scope: 'tenant' | 'project'): Promise<RoleWeb[]> => {
    const scopeValue = scope === 'tenant' ? 0 : 1;
    const response = await axiosClient.get('/api/roles', {
      params: { scope: scopeValue }
    });
    return response.data;
  },
};
```

### **Step 2: Use in Components with React Query**

```typescript
// Example: Role Selection Dropdown for Tenant Members
import { useQuery } from '@tanstack/react-query';
import { roleApi } from '../api/roleApi';

export function TenantMemberRoleSelect({ 
  currentRoleId, 
  onChange 
}: {
  currentRoleId: string;
  onChange: (roleId: string) => void;
}) {
  // Fetch available tenant roles
  const { data: roles, isLoading } = useQuery({
    queryKey: ['roles', 'tenant'],
    queryFn: roleApi.getTenantRoles,
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  if (isLoading) {
    return <Spinner />;
  }

  return (
    <Select 
      value={currentRoleId} 
      onChange={(e) => onChange(e.target.value)}
    >
      {roles?.map(role => (
        <option key={role.id} value={role.id}>
          {role.name}
          {role.description && ` - ${role.description}`}
        </option>
      ))}
    </Select>
  );
}
```

### **Step 3: Use in Project Member Role Select**

```typescript
// Example: Role Selection Dropdown for Project Members
export function ProjectMemberRoleSelect({ 
  currentRoleId, 
  onChange 
}: {
  currentRoleId: string;
  onChange: (roleId: string) => void;
}) {
  const { data: roles, isLoading } = useQuery({
    queryKey: ['roles', 'project'],
    queryFn: roleApi.getProjectRoles,
    staleTime: 5 * 60 * 1000,
  });

  if (isLoading) {
    return <Spinner />;
  }

  return (
    <Select 
      value={currentRoleId} 
      onChange={(e) => onChange(e.target.value)}
    >
      {roles?.map(role => (
        <option key={role.id} value={role.id}>
          {role.name}
        </option>
      ))}
    </Select>
  );
}
```

### **Step 4: Update Role Change Handler**

```typescript
// Before - with old enum
const handleRoleChange = async (userId: string, newRole: number) => {
  await updateTenantMemberRole(tenantId, userId, newRole);
};

// After - with roleId
const handleRoleChange = async (userId: string, newRoleId: string) => {
  await updateTenantMemberRole(tenantId, userId, newRoleId);
};
```

---

## 🎨 Full Component Example

```typescript
// src/components/EditMemberRoleModal.tsx
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  Select,
  FormControl,
  FormLabel,
  useToast,
} from '@chakra-ui/react';
import { roleApi, RoleWeb } from '../api/roleApi';
import { tenantApi } from '../api/tenantApi';

interface EditMemberRoleModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  userId: string;
  currentRoleId: string;
  memberName: string;
}

export function EditMemberRoleModal({
  isOpen,
  onClose,
  tenantId,
  userId,
  currentRoleId,
  memberName,
}: EditMemberRoleModalProps) {
  const [selectedRoleId, setSelectedRoleId] = useState(currentRoleId);
  const toast = useToast();
  const queryClient = useQueryClient();

  // Fetch available roles
  const { data: roles, isLoading: rolesLoading } = useQuery({
    queryKey: ['roles', 'tenant'],
    queryFn: roleApi.getTenantRoles,
    staleTime: 5 * 60 * 1000,
  });

  // Mutation to update role
  const updateRoleMutation = useMutation({
    mutationFn: (roleId: string) => 
      tenantApi.updateTenantMemberRole(tenantId, userId, roleId),
    onSuccess: () => {
      toast({
        title: 'Rola zaktualizowana',
        description: `Rola użytkownika ${memberName} została zmieniona`,
        status: 'success',
        duration: 3000,
      });
      queryClient.invalidateQueries({ queryKey: ['tenant', tenantId, 'members'] });
      onClose();
    },
    onError: (error) => {
      toast({
        title: 'Błąd',
        description: 'Nie udało się zmienić roli',
        status: 'error',
        duration: 5000,
      });
    },
  });

  const handleSave = () => {
    if (selectedRoleId === currentRoleId) {
      onClose();
      return;
    }
    updateRoleMutation.mutate(selectedRoleId);
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Zmień rolę: {memberName}</ModalHeader>
        <ModalBody>
          <FormControl>
            <FormLabel>Nowa rola</FormLabel>
            <Select
              value={selectedRoleId}
              onChange={(e) => setSelectedRoleId(e.target.value)}
              isDisabled={rolesLoading || updateRoleMutation.isPending}
            >
              {roles?.map((role: RoleWeb) => (
                <option key={role.id} value={role.id}>
                  {role.name}
                  {role.description && ` - ${role.description}`}
                </option>
              ))}
            </Select>
          </FormControl>
        </ModalBody>
        <ModalFooter>
          <Button 
            variant="ghost" 
            mr={3} 
            onClick={onClose}
            isDisabled={updateRoleMutation.isPending}
          >
            Anuluj
          </Button>
          <Button
            colorScheme="blue"
            onClick={handleSave}
            isLoading={updateRoleMutation.isPending}
            isDisabled={selectedRoleId === currentRoleId}
          >
            Zapisz
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
```

---

## 🔄 Integration with Existing Components

### **Update AddProjectMemberModal**

```typescript
// Before
<Select value={selectedRole} onChange={...}>
  <option value={ProjectRole.Admin}>Admin</option>
  <option value={ProjectRole.Editor}>Editor</option>
</Select>

// After
const { data: roles } = useQuery({
  queryKey: ['roles', 'project'],
  queryFn: roleApi.getProjectRoles,
});

<Select value={selectedRoleId} onChange={...}>
  {roles?.map(role => (
    <option key={role.id} value={role.id}>{role.name}</option>
  ))}
</Select>
```

### **Update TenantMembers Page**

```typescript
// Add role fetching
const { data: availableRoles } = useQuery({
  queryKey: ['roles', 'tenant'],
  queryFn: roleApi.getTenantRoles,
});

// Use in role display
const getRoleNameById = (roleId: string) => {
  return availableRoles?.find(r => r.id === roleId)?.name || 'Unknown';
};
```

---

## 🎯 Best Practices

### **1. Cache Roles Globally**

Roles don't change frequently, so cache them:

```typescript
// React Query config
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      cacheTime: 30 * 60 * 1000, // 30 minutes
    },
  },
});
```

### **2. Pre-fetch Roles on App Load**

```typescript
// In App.tsx or AuthProvider
const { user } = useAuth();

// Pre-fetch tenant roles
useQuery({
  queryKey: ['roles', 'tenant'],
  queryFn: roleApi.getTenantRoles,
  enabled: !!user,
  staleTime: 10 * 60 * 1000,
});

// Pre-fetch project roles
useQuery({
  queryKey: ['roles', 'project'],
  queryFn: roleApi.getProjectRoles,
  enabled: !!user,
  staleTime: 10 * 60 * 1000,
});
```

### **3. Create Custom Hook**

```typescript
// src/hooks/useAvailableRoles.ts
import { useQuery } from '@tanstack/react-query';
import { roleApi } from '../api/roleApi';

export function useAvailableRoles(scope: 'tenant' | 'project') {
  return useQuery({
    queryKey: ['roles', scope],
    queryFn: () => 
      scope === 'tenant' 
        ? roleApi.getTenantRoles() 
        : roleApi.getProjectRoles(),
    staleTime: 5 * 60 * 1000,
  });
}

// Usage
const { data: roles, isLoading } = useAvailableRoles('tenant');
```

### **4. Role Lookup Helper**

```typescript
// src/utils/roleHelpers.ts
import type { RoleWeb } from '../api/roleApi';

export function getRoleNameById(
  roles: RoleWeb[] | undefined, 
  roleId: string
): string {
  return roles?.find(r => r.id === roleId)?.name || 'Unknown Role';
}

export function getRoleByCode(
  roles: RoleWeb[] | undefined, 
  code: string
): RoleWeb | undefined {
  return roles?.find(r => r.code === code);
}
```

---

## 📊 API Response Examples

### **Tenant Roles Response**

```json
[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "code": "TENANT.ADMIN",
    "name": "Administrator",
    "description": "Full access to tenant management and settings",
    "scope": "Tenant"
  },
  {
    "id": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
    "code": "TENANT.MEMBER",
    "name": "Członek",
    "description": "Basic tenant member with limited access",
    "scope": "Tenant"
  }
]
```

### **Project Roles Response**

```json
[
  {
    "id": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
    "code": "PROJECT.ADMIN",
    "name": "Administrator",
    "description": "Full project administration rights",
    "scope": "Project"
  },
  {
    "id": "d4e5f6g7-h8i9-0123-defg-hi4567890123",
    "code": "PROJECT.EDITOR",
    "name": "Edytor",
    "description": "Can edit project content and resources",
    "scope": "Project"
  },
  {
    "id": "e5f6g7h8-i9j0-1234-efgh-ij5678901234",
    "code": "PROJECT.COLLABORATOR",
    "name": "Współpracownik",
    "description": "Can collaborate on project tasks",
    "scope": "Project"
  },
  {
    "id": "f6g7h8i9-j0k1-2345-fghi-jk6789012345",
    "code": "PROJECT.VIEWER",
    "name": "Przeglądający",
    "description": "Read-only access to project",
    "scope": "Project"
  }
]
```

---

## 🐛 Error Handling

```typescript
const { data: roles, isLoading, isError, error } = useQuery({
  queryKey: ['roles', 'tenant'],
  queryFn: roleApi.getTenantRoles,
  retry: 2,
  onError: (error) => {
    toast({
      title: 'Błąd ładowania ról',
      description: 'Nie udało się pobrać dostępnych ról',
      status: 'error',
      duration: 5000,
    });
  },
});

if (isError) {
  return (
    <Alert status="error">
      <AlertIcon />
      Nie udało się załadować ról. Spróbuj odświeżyć stronę.
    </Alert>
  );
}
```

---

## ✅ Migration Checklist

- [ ] Create `roleApi.ts` with API calls
- [ ] Update `AddProjectMemberModal` to fetch roles
- [ ] Update `AddTenantMemberModal` to fetch roles
- [ ] Update `EditMemberRoleModal` to fetch roles
- [ ] Create `useAvailableRoles` hook
- [ ] Pre-fetch roles on app load
- [ ] Test role dropdowns work
- [ ] Test role changes work
- [ ] Add error handling for role fetching

---

**Backend endpoints are ready!** 🎉  
Use this guide to integrate dynamic role fetching in the frontend.
