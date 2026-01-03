/**
 * UPDATED Project Types - Compatible with new backend API
 * 
 * Changes from old version:
 * - ProjectRole enum (number) → roleCode (string)
 * - Added permission-based helpers
 * - Updated all interfaces to use roleCode instead of role
 */

import { RoleCodes, PermissionCodes, getRoleName, getRoleColor, hasPermission } from '../constants/roleCodes';

// ============================================================================
// Project Details - UPDATED
// ============================================================================

export interface ProjectDetailsWeb {
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  userRoleCode: string;  // UPDATED: was "userRole: number"
  membersCount: number;
}

// ============================================================================
// Project Members - UPDATED
// ============================================================================

export interface TenantMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;  // UPDATED: was "role: number"
  isActive: boolean;
  joinedAt: string;
}

export interface ProjectMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;  // UPDATED: was "role: number"
  joinedAt: string;
}

export interface AddProjectMemberCommand {
  tenantId: string;
  projectId: string;
  userId: string;
}

// ============================================================================
// Project Files
// ============================================================================

export interface ProjectFilePackageWeb {
  id: string;
  name: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  files: ProjectFileWeb[];
  totalFiles: number;
}

export interface ProjectFileWeb {
  id: string;
  fileName: string;
  displayName: string;
  packageName: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  currentVersion?: ProjectFileVersionWeb;
  versions: ProjectFileVersionWeb[];
  totalVersions: number;
  isOwner: boolean;
  isShared: boolean;
  sharedWithUserIds: string[];
}

export interface ProjectFileVersionWeb {
  id: string;
  projectFileId: string;
  versionNumber: number;
  contentType: string;
  fileSizeBytes: number;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  sasUrlView: string;
  sasUrlDownload: string;
  comments: ProjectFileVersionCommentWeb[];
}

export interface ProjectFileVersionCommentWeb {
  id: string;
  projectFileVersionId: string;
  userId: string;
  userName: string;
  content: string;
  createdAt: string;
  editedAt?: string;
  isEdited: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export interface ShareProjectFileResult {
  sharedFileIds: string[];
  successCount: number;
  failedCount: number;
  errors: string[];
}

export interface SharedProjectFilePackageWeb {
  packageId: string;
  packageName: string;
  packageOwnerId: string;
  packageOwnerName: string;
  files: SharedProjectFileWeb[];
  totalSharedFiles: number;
}

export interface SharedProjectFileWeb {
  id: string;
  projectFileId: string;
  fileName: string;
  displayName: string;
  packageName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
  sharedAt: string;
  sharedByUserId: string;
  sharedByUserName: string;
  originalOwnerUserId: string;
  originalOwnerUserName: string;
  sasUrl: string;
  currentVersion?: ProjectFileVersionWeb;
  versions: ProjectFileVersionWeb[];
  totalVersions: number;
}

// ============================================================================
// Project Costs
// ============================================================================

export interface ProjectCostListItemWeb {
  id: string;
  userId: string;
  userName: string;
  name: string;
  place?: string;
  date: string;
  description?: string;
  netAmount?: number;
  vatRate?: number;
  grossAmount: number;
  isClosed: boolean;
  hasDocument: boolean;
  documentFileName?: string;
  previewSasUrl?: string;
  downloadSasUrl?: string;
  sharedWithUserIds: string[];
  createdAt: string;
}

export interface CreateProjectCostCommand {
  tenantId: string;
  projectId: string;
  name: string;
  place?: string;
  date: string;
  description?: string;
  netAmount?: number;
  vatRate?: number;
  grossAmount?: number;
  document?: File;
}

export interface UpdateProjectCostCommand {
  tenantId: string;
  projectId: string;
  costId: string;
  name: string;
  place?: string;
  date: string;
  description?: string;
  netAmount?: number;
  vatRate?: number;
  grossAmount?: number;
  isClosed: boolean;
  document?: File;
  removeDocument: boolean;
}

export interface SharedProjectCostWeb {
  id: string;
  projectCostId: string;
  sharedWithUserId: string;
  sharedWithUserName: string;
  sharedByUserId: string;
  sharedByUserName: string;
  sharedAt: string;
  costName: string;
  costPlace?: string;
  costDate: string;
  costDescription?: string;
  costNetAmount?: number;
  costVatRate?: number;
  costGrossAmount: number;
  costIsClosed: boolean;
  costHasDocument: boolean;
  costDocumentFileName?: string;
  previewSasUrl?: string;
  downloadSasUrl?: string;
}

// ============================================================================
// Permission-based helpers (RECOMMENDED)
// ============================================================================

/**
 * Check if user can edit project (permission-based)
 */
export const canEditProject = (
  projectPermissions: Record<string, string[]>,
  projectId: string
): boolean => {
  const permissions = projectPermissions[projectId] || [];
  return hasPermission(permissions, PermissionCodes.PROJECT_EDIT);
};

/**
 * Check if user can manage project members (permission-based)
 */
export const canManageProjectMembers = (
  projectPermissions: Record<string, string[]>,
  projectId: string
): boolean => {
  const permissions = projectPermissions[projectId] || [];
  return hasPermission(permissions, PermissionCodes.PROJECT_MEMBERS_MANAGE);
};

/**
 * Check if user can upload files to project (permission-based)
 */
export const canUploadFiles = (
  projectPermissions: Record<string, string[]>,
  projectId: string
): boolean => {
  const permissions = projectPermissions[projectId] || [];
  return hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE);
};

/**
 * Check if user can view shared files (permission-based)
 */
export const canViewSharedFiles = (
  projectPermissions: Record<string, string[]>,
  projectId: string
): boolean => {
  const permissions = projectPermissions[projectId] || [];
  return hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_SHARED);
};

/**
 * Check if user can edit shared files (permission-based)
 */
export const canEditSharedFiles = (
  projectPermissions: Record<string, string[]>,
  projectId: string
): boolean => {
  const permissions = projectPermissions[projectId] || [];
  return hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED);
};

/**
 * Check if user is project admin (role-based - use sparingly)
 */
export const isProjectAdmin = (roleCode: string): boolean => {
  return roleCode === RoleCodes.PROJECT_ADMIN;
};

// ============================================================================
// Role display helpers - Re-export from roleCodes
// ============================================================================

export { getRoleName, getRoleColor } from '../constants/roleCodes';

// ============================================================================
// DEPRECATED - Old enum-based approach
// ============================================================================

/**
 * @deprecated Use RoleCodes.PROJECT_* instead
 */
export const ProjectRole = {
  Admin: 0,
  Editor: 1,
  Viewer: 2,
  Member: 3,
} as const;

/**
 * @deprecated Use permission checks instead
 */
export const getProjectRoleLevel = (role: number): number => {
  console.warn('getProjectRoleLevel(number) is deprecated. Use permission checks instead.');
  return role;
};

/**
 * @deprecated Use permission checks instead
 */
export const hasProjectRoleLevel = (userRole: number, requiredRole: number): boolean => {
  console.warn('hasProjectRoleLevel is deprecated. Use permission checks instead.');
  return getProjectRoleLevel(userRole) <= getProjectRoleLevel(requiredRole);
};

/**
 * @deprecated Use roleCode string comparison instead
 */
export const getProjectRoleName = (role: number): string => {
  console.warn('getProjectRoleName(number) is deprecated. Use getRoleName(roleCode: string) instead.');
  switch (role) {
    case ProjectRole.Admin:
      return 'Administrator';
    case ProjectRole.Editor:
      return 'Edytor';
    case ProjectRole.Viewer:
      return 'Przeglądający';
    case ProjectRole.Member:
      return 'Członek';
    default:
      return 'Nieznana rola';
  }
};

/**
 * @deprecated Use getRoleColor(roleCode: string) instead
 */
export const getProjectRoleColor = (role: number): string => {
  console.warn('getProjectRoleColor(number) is deprecated. Use getRoleColor(roleCode: string) instead.');
  switch (role) {
    case ProjectRole.Admin:
      return 'purple';
    case ProjectRole.Editor:
      return 'blue';
    case ProjectRole.Viewer:
      return 'green';
    case ProjectRole.Member:
      return 'gray';
    default:
      return 'gray';
  }
};
