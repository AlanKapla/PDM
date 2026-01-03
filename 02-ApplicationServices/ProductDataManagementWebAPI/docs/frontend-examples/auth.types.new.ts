/**
 * UPDATED Auth Types - Compatible with new backend API
 * 
 * Changes from old version:
 * - TenantRole enum (number) → roleCode (string)
 * - Added permission-based helpers
 * - Updated all interfaces to use roleCode instead of role
 */

import { RoleCodes, PermissionCodes, getRoleName, getRoleColor, hasPermission } from '../constants/roleCodes';

// ============================================================================
// Authentication Requests
// ============================================================================

export interface LoginRequest {
  email: string;
  password: string;
  externalToken: string;
  provider: number;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  externalToken: string;
  provider: number;
}

export interface LogoutRequest {
  refreshToken: string;
}

export interface PasswordResetRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  password: string;
}

// ============================================================================
// User Profile - UPDATED
// ============================================================================

export interface UserProfile {
  id?: string;
  email: string;
  firstName: string;
  lastName: string;
  activeTenantId?: string | null;
  
  // NEW: Permissions in active tenant
  activeTenantPermissions: string[];
  
  // UPDATED: Role codes instead of role numbers
  projectRoleCodes: Record<string, string>;  // projectId → roleCode
  
  // NEW: Permissions per project
  projectPermissions: Record<string, string[]>;  // projectId → permissions[]
}

export interface ActiveTenant {
  activeTenantId: string | null;
}

// ============================================================================
// Tenant Details - UPDATED
// ============================================================================

export interface TenantMemberDetails {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;  // UPDATED: was "role: number"
  isActive: boolean;
  joinedAt: string;
}

export interface TenantDetails {
  id: string;
  name: string;
  createdAt: string;
  roleCode: string;  // UPDATED: was "role: number"
  isActive: boolean;
  members: TenantMemberDetails[];
  invitations: TenantInvitationWeb[];
}

// ============================================================================
// Tenant Invitations
// ============================================================================

export interface InviteTenantMemberRequest {
  tenantId: string;
  email: string;
}

export interface AcceptTenantInvitationRequest {
  token: string;
}

export const InvitationStatus = {
  Pending: 0,
  Accepted: 1,
  Revoked: 2
} as const;

export type InvitationStatusType = typeof InvitationStatus[keyof typeof InvitationStatus];

export interface TenantInvitationWeb {
  invitationId: string;
  tenantId: string;
  tenantName: string;
  email: string;
  invitedByUserEmail: string;
  invitedByUserName: string;
  createdAt: string;
  expiresAt: string | null;
  status: InvitationStatusType;
  token: string;
}

// ============================================================================
// Helper Functions - UPDATED to use roleCode
// ============================================================================

/**
 * Get display name for invitation status
 */
export const getInvitationStatusName = (status: InvitationStatusType): string => {
  switch (status) {
    case InvitationStatus.Pending:
      return 'Oczekuje';
    case InvitationStatus.Accepted:
      return 'Zaakceptowane';
    case InvitationStatus.Revoked:
      return 'Anulowane';
    default:
      return 'Nieznany';
  }
};

/**
 * Get badge color for invitation status
 */
export const getInvitationStatusColor = (status: InvitationStatusType): string => {
  switch (status) {
    case InvitationStatus.Pending:
      return 'orange';
    case InvitationStatus.Accepted:
      return 'green';
    case InvitationStatus.Revoked:
      return 'red';
    default:
      return 'gray';
  }
};

// ============================================================================
// Permission-based helpers (RECOMMENDED)
// ============================================================================

/**
 * Check if user can edit tenant (permission-based)
 */
export const canEditTenant = (user: UserProfile): boolean => {
  return hasPermission(user.activeTenantPermissions, PermissionCodes.TENANT_EDIT);
};

/**
 * Check if user can manage tenant members (permission-based)
 */
export const canManageTenantMembers = (user: UserProfile): boolean => {
  return hasPermission(user.activeTenantPermissions, PermissionCodes.TENANT_MEMBERS_MANAGE);
};

/**
 * Check if user can create projects in tenant (permission-based)
 */
export const canCreateProject = (user: UserProfile): boolean => {
  return hasPermission(user.activeTenantPermissions, PermissionCodes.TENANT_PROJECT_CREATE);
};

/**
 * Check if user is tenant admin (role-based - use sparingly)
 */
export const isTenantAdmin = (roleCode: string): boolean => {
  return roleCode === RoleCodes.TENANT_ADMIN;
};

// ============================================================================
// Role display helpers - Re-export from roleCodes
// ============================================================================

export { getRoleName, getRoleColor } from '../constants/roleCodes';

// ============================================================================
// DEPRECATED - Old enum-based approach
// ============================================================================

/**
 * @deprecated Use RoleCodes.TENANT_ADMIN instead
 */
export const TenantRole = {
  Admin: 0,
  Member: 1,
} as const;

/**
 * @deprecated Use roleCode string comparison instead
 */
export const getTenantRoleName = (role: number): string => {
  console.warn('getTenantRoleName(number) is deprecated. Use getRoleName(roleCode: string) instead.');
  switch (role) {
    case TenantRole.Admin:
      return 'Administrator';
    case TenantRole.Member:
      return 'Członek';
    default:
      return 'Nieznana rola';
  }
};

/**
 * @deprecated Use getRoleColor(roleCode: string) instead
 */
export const getTenantRoleColor = (role: number): string => {
  console.warn('getTenantRoleColor(number) is deprecated. Use getRoleColor(roleCode: string) instead.');
  switch (role) {
    case TenantRole.Admin:
      return 'purple';
    case TenantRole.Member:
      return 'gray';
    default:
      return 'gray';
  }
};
