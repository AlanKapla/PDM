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
  email: string;
  firstName: string;
  lastName: string;
  activeTenantId?: string | null;
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
} as const;

export type TenantRoleType = (typeof TenantRole)[keyof typeof TenantRole];

export interface TenantMemberDetails {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  joinedAt: string;
}

export interface TenantDetails {
  id: string;
  name: string;
  createdAt: string;
  role: number;
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
    default:
      return 'Nieznana rola';
  }
};

export const getTenantRoleColor = (role: number): string => {
  switch (role) {
    case TenantRole.Admin:
      return 'purple';
    case TenantRole.Member:
      return 'blue';
    default:
      return 'gray';
  }
};
