// Typy dla API User/Auth

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

export interface UserProfile {
  id?: string;
  email: string;
  firstName: string;
  lastName: string;
  activeTenantId?: string | null;
  
  /**
   * Permissions in the active tenant (empty if no active tenant)
   */
  activeTenantPermissions: string[];

  // Kontaktowe
  phoneNumber?: string | null;

  // Firmowe
  companyName?: string | null;
  taxId?: string | null;

  // Adresowe
  street?: string | null;
  city?: string | null;
  postalCode?: string | null;
  country?: string | null;
}

export interface PasswordResetRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  password: string;
}

export const TenantRole = {
  Admin: 0,
  Member: 1,
  Editor: 2,
  Viewer: 3,
} as const;

export type TenantRoleType = (typeof TenantRole)[keyof typeof TenantRole];

// Funkcja pomocnicza do określania poziomu roli (im niższa wartość, tym wyższe uprawnienia)
export const getTenantRoleLevel = (role: number): number => {
  switch (role) {
    case TenantRole.Admin: return 0;
    case TenantRole.Editor: return 1;
    case TenantRole.Viewer: return 2;
    case TenantRole.Member: return 3;
    default: return Number.MAX_SAFE_INTEGER;
  }
};

// Funkcje pomocnicze do sprawdzania uprawnień
export const hasTenantRoleLevel = (userRole: number, requiredRole: number): boolean => {
  return getTenantRoleLevel(userRole) <= getTenantRoleLevel(requiredRole);
};

export const isTenantAdmin = (userRole: number): boolean => {
  return userRole === TenantRole.Admin;
};

export const canEditTenant = (userRole: number): boolean => {
  return hasTenantRoleLevel(userRole, TenantRole.Editor);
};

export const canViewTenant = (userRole: number): boolean => {
  return hasTenantRoleLevel(userRole, TenantRole.Viewer);
};

export interface TenantMemberDetails {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;
  isActive: boolean;
  joinedAt: string;
}

/**
 * Basic tenant info for user tenant list
 */
export interface UserTenant {
  id: string;
  name: string;
  createdAt: string;
  isActive: boolean;
  roleCode: string;
  isActiveTenant: boolean;
}

/**
 * Basic tenant info for admin tenant list
 */
export interface TenantBasic {
  id: string;
  name: string;
  createdAt: string;
  isActive: boolean;
  roleCode: string;
}

/**
 * Detailed tenant info with members and invitations
 */
export interface TenantDetails {
  id: string;
  name: string;
  createdAt: string;
  roleCode: string;
  isActive: boolean;
  members: TenantMemberDetails[];
  invitations: TenantInvitationWeb[];
}

export interface ActiveTenant {
  activeTenantId: string | null;
}

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

export type InvitationStatus = typeof InvitationStatus[keyof typeof InvitationStatus];

export interface TenantInvitationWeb {
  invitationId: string;
  tenantId: string;
  tenantName: string;
  email: string;
  invitedByUserEmail: string;
  invitedByUserName: string;
  createdAt: string;
  expiresAt: string | null;
  status: InvitationStatus;
  token: string;
}

export const getInvitationStatusName = (status: InvitationStatus): string => {
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

export const getInvitationStatusColor = (status: InvitationStatus): string => {
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

export const getTenantRoleName = (role: number): string => {
  switch (role) {
    case TenantRole.Admin:
      return 'Administrator';
    case TenantRole.Member:
      return 'Członek';
    case TenantRole.Editor:
      return 'Edytor';
    case TenantRole.Viewer:
      return 'Przeglądający';
    default:
      return 'Nieznana rola';
  }
};

export const getTenantRoleColor = (role: number): string => {
  switch (role) {
    case TenantRole.Admin:
      return 'purple';
    case TenantRole.Editor:
      return 'blue';
    case TenantRole.Viewer:
      return 'green';
    case TenantRole.Member:
      return 'gray';
    default:
      return 'gray';
  }
};
