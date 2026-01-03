/**
 * Role Codes - matching backend RoleCodes.cs
 * These replace the old enum-based role system
 */

export const RoleCodes = {
  // Tenant Roles
  TENANT_ADMIN: "TENANT.ADMIN",
  TENANT_MEMBER: "TENANT.MEMBER",
  
  // Project Roles
  PROJECT_ADMIN: "PROJECT.ADMIN",
  PROJECT_EDITOR: "PROJECT.EDITOR",
  PROJECT_COLLABORATOR: "PROJECT.COLLABORATOR",
  PROJECT_VIEWER: "PROJECT.VIEWER",
  PROJECT_MEMBER: "PROJECT.MEMBER",
} as const;

export type RoleCode = typeof RoleCodes[keyof typeof RoleCodes];

/**
 * Permission Codes - matching backend PermissionCodes.cs
 * Use these for permission-based UI rendering
 */
export const PermissionCodes = {
  // Global
  TENANT_LIST_AVAILABLE: "TENANT.LIST.AVAILABLE",
  ROLE_LIST: "ROLE.LIST",  // ✅ NEW: List available roles (for admins)
  
  // Tenant
  TENANT_VIEW: "TENANT.VIEW",
  TENANT_EDIT: "TENANT.EDIT",
  TENANT_MEMBERS_MANAGE: "TENANT.MEMBERS.MANAGE",
  TENANT_STATUS_MANAGE: "TENANT.STATUS.MANAGE",
  TENANT_PROJECT_CREATE: "TENANT.PROJECT.CREATE",
  
  // Project - Basic
  PROJECT_VIEW: "PROJECT.VIEW",
  PROJECT_EDIT: "PROJECT.EDIT",
  
  // Project - Members
  PROJECT_MEMBERS_VIEW: "PROJECT.MEMBERS.VIEW",
  PROJECT_MEMBERS_MANAGE: "PROJECT.MEMBERS.MANAGE",
  
  // Project - Status
  PROJECT_STATUS_MANAGE: "PROJECT.STATUS.MANAGE",
  
  // Project - Resources
  PROJECT_RESOURCES_READ: "PROJECT.RESOURCES.READ",
  PROJECT_RESOURCES_WRITE: "PROJECT.RESOURCES.WRITE",
  PROJECT_RESOURCES_READ_SHARED: "PROJECT.RESOURCES.READ_SHARED",
  PROJECT_RESOURCES_WRITE_SHARED: "PROJECT.RESOURCES.WRITE_SHARED",
} as const;

export type PermissionCode = typeof PermissionCodes[keyof typeof PermissionCodes];

/**
 * Helper function to check if user has a specific permission
 */
export const hasPermission = (
  permissions: string[],
  requiredPermission: string
): boolean => {
  return permissions.includes(requiredPermission);
};

/**
 * Helper function to check if user has ANY of the required permissions
 */
export const hasAnyPermission = (
  permissions: string[],
  requiredPermissions: string[]
): boolean => {
  return requiredPermissions.some(perm => permissions.includes(perm));
};

/**
 * Helper function to check if user has ALL of the required permissions
 */
export const hasAllPermissions = (
  permissions: string[],
  requiredPermissions: string[]
): boolean => {
  return requiredPermissions.every(perm => permissions.includes(perm));
};

/**
 * Get human-readable role name from role code
 */
export const getRoleName = (roleCode: string): string => {
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

/**
 * Get badge color for role display
 */
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

/**
 * Check if role code is tenant admin
 */
export const isTenantAdminRole = (roleCode: string): boolean => {
  return roleCode === RoleCodes.TENANT_ADMIN;
};

/**
 * Check if role code is project admin
 */
export const isProjectAdminRole = (roleCode: string): boolean => {
  return roleCode === RoleCodes.PROJECT_ADMIN;
};

/**
 * DEPRECATED: Temporary helper for migration from old enum system
 * Will be removed in future version
 */
export const migrateRoleNumberToCode = (
  role: number,
  scope: 'tenant' | 'project'
): string => {
  if (scope === 'tenant') {
    const mapping: Record<number, string> = {
      0: RoleCodes.TENANT_ADMIN,
      1: RoleCodes.TENANT_MEMBER,
    };
    return mapping[role] || RoleCodes.TENANT_MEMBER;
  } else {
    const mapping: Record<number, string> = {
      0: RoleCodes.PROJECT_ADMIN,
      1: RoleCodes.PROJECT_EDITOR,
      2: RoleCodes.PROJECT_VIEWER,
      3: RoleCodes.PROJECT_MEMBER,
    };
    return mapping[role] || RoleCodes.PROJECT_MEMBER;
  }
};

// src/api/roleApi.ts (NEW FILE)
import { axiosClient } from './axiosClient';

export interface RoleWeb {
  id: string;
  code: string;
  name: string;
  description?: string;
  scope: 'Tenant' | 'Project';  // Maps to RoleScope enum (0 = Tenant, 1 = Project)
}

export const roleApi = {
  /**
   * Get all available roles for a specific scope
   * @param scope - 'tenant' or 'project'
   * @returns Array of available roles
   */
  getAvailableRoles: async (scope: 'tenant' | 'project'): Promise<RoleWeb[]> => {
    const scopeValue = scope === 'tenant' ? 0 : 1;  // RoleScope enum
    const response = await axiosClient.get('/api/roles', {
      params: { scope: scopeValue }
    });
    return response.data;
  },

  /**
   * Get all available tenant roles (convenience method)
   * @returns Array of tenant roles
   */
  getTenantRoles: async (): Promise<RoleWeb[]> => {
    const response = await axiosClient.get('/api/roles/tenant');
    return response.data;
  },

  /**
   * Get all available project roles (convenience method)
   * @returns Array of project roles
   */
  getProjectRoles: async (): Promise<RoleWeb[]> => {
    const response = await axiosClient.get('/api/roles/project');
    return response.data;
  },
};
