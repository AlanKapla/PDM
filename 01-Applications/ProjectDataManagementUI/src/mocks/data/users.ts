/**
 * Stali użytkownicy demonstracyjni.
 * Anna Kowalska jest bieżącym użytkownikiem (zalogowanym w trybie demo).
 */

import type { UserProfile, TenantMemberDetails } from "../../types/auth.types";
import { RoleCodes, PermissionCodes } from "../../constants/roleCodes";

export const DEMO_TENANT_ID = "tenant-archplan-001";

export const DEMO_USERS = {
  anna: {
    userId: "user-anna-001",
    email: "anna.kowalska@archplan.pl",
    firstName: "Anna",
    lastName: "Kowalska",
    roleCode: RoleCodes.TENANT_ADMIN,
    isActive: true,
    joinedAt: "2023-01-15T08:00:00Z",
  },
  piotr: {
    userId: "user-piotr-002",
    email: "p.wisniewski@archplan.pl",
    firstName: "Piotr",
    lastName: "Wiśniewski",
    roleCode: RoleCodes.TENANT_ADMIN,
    isActive: true,
    joinedAt: "2023-01-20T09:00:00Z",
  },
  marta: {
    userId: "user-marta-003",
    email: "m.nowak@archplan.pl",
    firstName: "Marta",
    lastName: "Nowak",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2023-03-10T10:00:00Z",
  },
  tomasz: {
    userId: "user-tomasz-004",
    email: "t.zajac@archplan.pl",
    firstName: "Tomasz",
    lastName: "Zając",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2023-04-05T11:00:00Z",
  },
  katarzyna: {
    userId: "user-katarzyna-005",
    email: "k.wojcik@archplan.pl",
    firstName: "Katarzyna",
    lastName: "Wójcik",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2023-06-01T12:00:00Z",
  },
} as const;

/** Profil bieżącego użytkownika (Anna) zwracany przez /user/me */
export const DEMO_CURRENT_USER: UserProfile = {
  id: DEMO_USERS.anna.userId,
  email: DEMO_USERS.anna.email,
  firstName: DEMO_USERS.anna.firstName,
  lastName: DEMO_USERS.anna.lastName,
  activeTenantId: DEMO_TENANT_ID,
  // Anna jest administratorem tenanta — pełne uprawnienia
  activeTenantPermissions: [
    PermissionCodes.TENANT_LIST_AVAILABLE,
    PermissionCodes.TENANT_ADMIN_LIST_AVAILABLE,
    PermissionCodes.TENANT_VIEW,
    PermissionCodes.TENANT_EDIT,
    PermissionCodes.TENANT_MEMBERS_MANAGE,
    PermissionCodes.TENANT_PROJECT_CREATE,
    PermissionCodes.TENANT_STATUS_MANAGE,
  ],
};

/** Lista członków organizacji */
export const DEMO_TENANT_MEMBERS: TenantMemberDetails[] = Object.values(
  DEMO_USERS
).map((u) => ({
  userId: u.userId,
  email: u.email,
  firstName: u.firstName,
  lastName: u.lastName,
  roleCode: u.roleCode,
  isActive: u.isActive,
  joinedAt: u.joinedAt,
}));
