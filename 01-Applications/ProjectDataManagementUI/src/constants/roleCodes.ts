/**
 * Role and Permission codes matching backend
 *
 * Backend sources:
 * - src/Business/Interfaces/Constants/RoleCodes.cs
 * - src/Business/Interfaces/Constants/PermissionCodes.cs
 * - src/Entities/Enums/ProjectModule.cs
 * - src/Entities/Enums/ModuleAccessLevel.cs
 */

// ===== ROLE CODES =====

export const RoleCodes = {
  // System roles
  SYSTEM_SUPERADMIN: "SYSTEM.SUPERADMIN",

  // Tenant roles replaced by IsAdmin boolean

  // Project roles (derived labels — not stored in DB)
  PROJECT_ADMIN: "PROJECT.ADMIN",
  PROJECT_EDITOR: "PROJECT.EDITOR",
  PROJECT_VIEWER: "PROJECT.VIEWER",
} as const;

export type RoleCode = typeof RoleCodes[keyof typeof RoleCodes];

// ===== PERMISSION CODES =====

export const PermissionCodes = {
  // TENANT – CONTEXT
  TenantContextList: "TENANT.CONTEXT.LIST",
  TenantContextAdminList: "TENANT.CONTEXT.ADMIN_LIST",

  // ROLE
  RoleList: "ROLE.LIST",

  // TENANT – BASE ACCESS
  TenantView: "TENANT.VIEW",

  // TENANT – SETTINGS
  TenantSettingsView: "TENANT.SETTINGS.VIEW",
  TenantSettingsEdit: "TENANT.SETTINGS.EDIT",
  TenantMembersManage: "TENANT.MEMBERS.MANAGE",
  TenantProjectsCreate: "TENANT.PROJECTS.CREATE",

  // PROJECT – BASE
  ProjectView: "PROJECT.VIEW",

  // PROJECT – MODULES (one per module)
  ProjectSettings: "PROJECT.SETTINGS",
  ProjectFiles: "PROJECT.FILES",
  ProjectEstimates: "PROJECT.ESTIMATES",
  ProjectCosts: "PROJECT.COSTS",
  ProjectSchedule: "PROJECT.SCHEDULE",
  ProjectDashboardTracker: "PROJECT.DASHBOARD_TRACKER",
} as const;

export type PermissionCode = typeof PermissionCodes[keyof typeof PermissionCodes];

// ===== HELPER FUNCTIONS =====

/**
 * Get Polish display name for role code
 */
export const getRoleName = (roleCode: string): string => {
  const roleNames: Record<string, string> = {
    [RoleCodes.SYSTEM_SUPERADMIN]: 'SuperAdmin',
    
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

    [RoleCodes.PROJECT_ADMIN]:   'level2',
    [RoleCodes.PROJECT_EDITOR]:  'primary',
    [RoleCodes.PROJECT_VIEWER]:  'neutral',
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
 * Check if role code is project admin
 */
export const isProjectAdminRole = (roleCode: string): boolean => {
  return roleCode === RoleCodes.PROJECT_ADMIN;
};

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
