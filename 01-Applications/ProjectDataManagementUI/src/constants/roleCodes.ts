/**
 * Role and Permission codes matching backend
 * 
 * Backend sources:
 * - src/Business/Interfaces/Constants/RoleCodes.cs
 * - src/Business/Interfaces/Constants/PermissionCodes.cs
 */

// ===== ROLE CODES =====

export const RoleCodes = {
  // System roles
  SYSTEM_SUPERADMIN: "SYSTEM.SUPERADMIN",
  
  // Tenant roles
  TENANT_ADMIN: "TENANT.ADMIN",
  TENANT_MEMBER: "TENANT.MEMBER",
  
  // Project roles
  PROJECT_ADMIN: "PROJECT.ADMIN",
  PROJECT_EDITOR: "PROJECT.EDITOR",
  PROJECT_VIEWER: "PROJECT.VIEWER",
} as const;

export type RoleCode = typeof RoleCodes[keyof typeof RoleCodes];

// ===== PERMISSION CODES =====

export const PermissionCodes = {
  // Tenant list/context permissions
  TENANT_LIST_AVAILABLE: "TENANT.LIST.AVAILABLE",
  TENANT_ADMIN_LIST_AVAILABLE: "TENANT.ADMIN.LIST.AVAILABLE",
  
  // Tenant permissions
  TENANT_VIEW: "TENANT.VIEW",
  TENANT_EDIT: "TENANT.EDIT",
  TENANT_MEMBERS_MANAGE: "TENANT.MEMBERS.MANAGE",
  TENANT_PROJECT_CREATE: "TENANT.PROJECT.CREATE",
  TENANT_STATUS_MANAGE: "TENANT.STATUS.MANAGE",
  
  // Project permissions
  PROJECT_VIEW: "PROJECT.VIEW",
  PROJECT_EDIT: "PROJECT.EDIT",
  PROJECT_MEMBERS_VIEW: "PROJECT.MEMBERS.VIEW",
  PROJECT_MEMBERS_MANAGE: "PROJECT.MEMBERS.MANAGE",
  PROJECT_STATUS_MANAGE: "PROJECT.STATUS.MANAGE",
  
  // Project resources (files, costs, schedules, estimates) - własne i udostępnione
  PROJECT_RESOURCES_READ: "PROJECT.RESOURCES.READ",
  PROJECT_RESOURCES_WRITE: "PROJECT.RESOURCES.WRITE",
  PROJECT_RESOURCES_READ_SHARED: "PROJECT.RESOURCES.READ_SHARED",
  PROJECT_RESOURCES_WRITE_SHARED: "PROJECT.RESOURCES.WRITE_SHARED",
  
  // Project resources - all (only for ProjectAdmin)
  PROJECT_RESOURCES_READ_ALL: "PROJECT.RESOURCES.READ_ALL",
  PROJECT_RESOURCES_WRITE_ALL: "PROJECT.RESOURCES.WRITE_ALL",
  
  // Project resources - sharing
  PROJECT_RESOURCES_SHARE: "PROJECT.RESOURCES.SHARE",
  
  // Project messages/chat
  PROJECT_MESSAGES_READ: "PROJECT.MESSAGES.READ",
  PROJECT_MESSAGES_WRITE: "PROJECT.MESSAGES.WRITE",
  PROJECT_MESSAGES_DELETE: "PROJECT.MESSAGES.DELETE",
  
  // Role management
  ROLE_LIST: "ROLE.LIST",
} as const;

export type PermissionCode = typeof PermissionCodes[keyof typeof PermissionCodes];

// ===== HELPER FUNCTIONS =====

/**
 * Get Polish display name for role code
 */
export const getRoleName = (roleCode: string): string => {
  const roleNames: Record<string, string> = {
    [RoleCodes.SYSTEM_SUPERADMIN]: 'SuperAdmin',
    
    [RoleCodes.TENANT_ADMIN]: 'Administrator',
    [RoleCodes.TENANT_MEMBER]: 'Członek',
    
    [RoleCodes.PROJECT_ADMIN]: 'Administrator',
    [RoleCodes.PROJECT_EDITOR]: 'Edytor',
    [RoleCodes.PROJECT_VIEWER]: 'Przeglądający',
  };
  
  return roleNames[roleCode] || 'Nieznana rola';
};

/**
 * Get badge color for role code
 */
export const getRoleColor = (roleCode: string): string => {
  const roleColors: Record<string, string> = {
    [RoleCodes.SYSTEM_SUPERADMIN]: 'red',
    
    [RoleCodes.TENANT_ADMIN]: 'purple',
    [RoleCodes.TENANT_MEMBER]: 'gray',
    
    [RoleCodes.PROJECT_ADMIN]: 'purple',
    [RoleCodes.PROJECT_EDITOR]: 'blue',
    [RoleCodes.PROJECT_VIEWER]: 'green',
  };
  
  return roleColors[roleCode] || 'gray';
};

/**
 * Check if user has specific permission
 */
export const hasPermission = (
  permissions: string[] | undefined,
  requiredPermission: string
): boolean => {
  if (!permissions) return false;
  return permissions.includes(requiredPermission);
};

/**
 * Check if user has any of the specified permissions
 */
export const hasAnyPermission = (
  permissions: string[] | undefined,
  requiredPermissions: string[]
): boolean => {
  if (!permissions) return false;
  return requiredPermissions.some(p => permissions.includes(p));
};

/**
 * Check if user has all of the specified permissions
 */
export const hasAllPermissions = (
  permissions: string[] | undefined,
  requiredPermissions: string[]
): boolean => {
  if (!permissions) return false;
  return requiredPermissions.every(p => permissions.includes(p));
};

/**
 * Check if role code is system super admin
 */
export const isSuperAdminRole = (roleCode: string): boolean => {
  return roleCode === RoleCodes.SYSTEM_SUPERADMIN;
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
