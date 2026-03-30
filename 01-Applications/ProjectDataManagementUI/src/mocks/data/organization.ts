/**
 * Dane demonstracyjne organizacji (tenantów).
 * - ArchPlan Sp. z o.o.       — aktywna, Anna jest administratorem
 * - BudProjekt S.A.           — Anna jest członkiem (zaproszonym jako specjalista)
 * - Infrastruktura Południe   — Anna nie jest członkiem (tylko widoczna lista na liście "admin-tenants")
 */

import type { UserTenant, TenantDetails, TenantMemberDetails, TenantInvitationWeb } from "../../types/auth.types";
import { InvitationStatus } from "../../types/auth.types";
import { RoleCodes } from "../../constants/roleCodes";
import { DEMO_TENANT_ID, DEMO_TENANT_MEMBERS, DEMO_USERS } from "./users";

/** Zwraca datę ISO przesuniętą o `days` dni od dziś */
const isoDate = (days: number) => {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString();
};

// ===== TENANT 1: ArchPlan Sp. z o.o. (aktywny) =====

export const DEMO_USER_TENANT: UserTenant = {
  id: DEMO_TENANT_ID,
  name: "ArchPlan Sp. z o.o.",
  createdAt: "2023-01-10T08:00:00Z",
  isActive: true,
  roleCode: RoleCodes.TENANT_ADMIN,
  isActiveTenant: true,
};

// Zaproszenia wysłane przez Annę w ramach ArchPlan (oczekujące)
const ARCHPLAN_INVITATIONS: TenantInvitationWeb[] = [
  {
    invitationId: "inv-archplan-001",
    tenantId: DEMO_TENANT_ID,
    tenantName: "ArchPlan Sp. z o.o.",
    email: "p.kowalczyk@archplan.pl",
    invitedByUserEmail: DEMO_USERS.anna.email,
    invitedByUserName: "Anna Kowalska",
    createdAt: isoDate(-5),
    expiresAt: isoDate(+25),
    status: InvitationStatus.Pending,
    token: "tok-archplan-inv-001",
  },
  {
    invitationId: "inv-archplan-002",
    tenantId: DEMO_TENANT_ID,
    tenantName: "ArchPlan Sp. z o.o.",
    email: "m.lewandowski@archplan.pl",
    invitedByUserEmail: DEMO_USERS.anna.email,
    invitedByUserName: "Anna Kowalska",
    createdAt: isoDate(-2),
    expiresAt: isoDate(+28),
    status: InvitationStatus.Pending,
    token: "tok-archplan-inv-002",
  },
  {
    invitationId: "inv-archplan-003",
    tenantId: DEMO_TENANT_ID,
    tenantName: "ArchPlan Sp. z o.o.",
    email: "i.zielinska@zumi.pl",
    invitedByUserEmail: DEMO_USERS.piotr.email,
    invitedByUserName: "Piotr Wiśniewski",
    createdAt: isoDate(-12),
    expiresAt: isoDate(+18),
    status: InvitationStatus.Pending,
    token: "tok-archplan-inv-003",
  },
];

export const DEMO_TENANT_DETAILS: TenantDetails = {
  id: DEMO_TENANT_ID,
  name: "ArchPlan Sp. z o.o.",
  createdAt: "2023-01-10T08:00:00Z",
  roleCode: RoleCodes.TENANT_ADMIN,
  isActive: true,
  members: DEMO_TENANT_MEMBERS,
  invitations: ARCHPLAN_INVITATIONS,
};

// ===== TENANT 2: BudProjekt S.A. — Anna jest CZŁONKIEM =====

export const DEMO_TENANT_BUDPROJEKT_ID = "tenant-budprojekt-002";

const BUDPROJEKT_MEMBERS: TenantMemberDetails[] = [
  {
    userId: "user-budprojekt-admin",
    email: "m.kaminski@budprojekt.pl",
    firstName: "Marek",
    lastName: "Kamiński",
    roleCode: RoleCodes.TENANT_ADMIN,
    isActive: true,
    joinedAt: "2022-05-01T08:00:00Z",
  },
  {
    userId: "user-budprojekt-02",
    email: "j.wieczorek@budprojekt.pl",
    firstName: "Joanna",
    lastName: "Wieczorek",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2022-06-15T09:00:00Z",
  },
  {
    userId: "user-budprojekt-03",
    email: "r.lis@budprojekt.pl",
    firstName: "Robert",
    lastName: "Lis",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2022-07-01T10:00:00Z",
  },
  // Anna zaproszona jako ekspert zewnętrzny
  {
    userId: DEMO_USERS.anna.userId,
    email: DEMO_USERS.anna.email,
    firstName: DEMO_USERS.anna.firstName,
    lastName: DEMO_USERS.anna.lastName,
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2024-01-08T08:00:00Z",
  },
];

export const DEMO_TENANT_BUDPROJEKT: UserTenant = {
  id: DEMO_TENANT_BUDPROJEKT_ID,
  name: "BudProjekt S.A.",
  createdAt: "2022-05-01T08:00:00Z",
  isActive: true,
  roleCode: RoleCodes.TENANT_MEMBER,
  isActiveTenant: false,
};

export const DEMO_TENANT_BUDPROJEKT_DETAILS: TenantDetails = {
  id: DEMO_TENANT_BUDPROJEKT_ID,
  name: "BudProjekt S.A.",
  createdAt: "2022-05-01T08:00:00Z",
  roleCode: RoleCodes.TENANT_MEMBER,
  isActive: true,
  members: BUDPROJEKT_MEMBERS,
  invitations: [],
};

// ===== TENANT 3: Infrastruktura Południe Sp. z o.o. =====

export const DEMO_TENANT_INFRA_ID = "tenant-infra-003";

const INFRA_MEMBERS: TenantMemberDetails[] = [
  {
    userId: "user-infra-admin",
    email: "a.nowacki@infrapol.pl",
    firstName: "Adam",
    lastName: "Nowacki",
    roleCode: RoleCodes.TENANT_ADMIN,
    isActive: true,
    joinedAt: "2021-03-12T08:00:00Z",
  },
  {
    userId: "user-infra-02",
    email: "e.kowal@infrapol.pl",
    firstName: "Ewa",
    lastName: "Kowal",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2021-04-01T09:00:00Z",
  },
  {
    userId: "user-infra-03",
    email: "m.szymanski@infrapol.pl",
    firstName: "Michał",
    lastName: "Szymański",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2021-05-20T10:00:00Z",
  },
];

export const DEMO_TENANT_INFRA: UserTenant = {
  id: DEMO_TENANT_INFRA_ID,
  name: "Infrastruktura Południe Sp. z o.o.",
  createdAt: "2021-03-12T08:00:00Z",
  isActive: true,
  roleCode: RoleCodes.TENANT_MEMBER,
  isActiveTenant: false,
};

export const DEMO_TENANT_INFRA_DETAILS: TenantDetails = {
  id: DEMO_TENANT_INFRA_ID,
  name: "Infrastruktura Południe Sp. z o.o.",
  createdAt: "2021-03-12T08:00:00Z",
  roleCode: RoleCodes.TENANT_MEMBER,
  isActive: true,
  members: INFRA_MEMBERS,
  invitations: [],
};

// ===== TENANT 4: Studio Rewitalizacji Sp. z o.o. — Anna jest ADMINEM =====

export const DEMO_TENANT_STUDIO_ID = "tenant-studio-004";

const STUDIO_MEMBERS: TenantMemberDetails[] = [
  // Anna założyła i zarządza tym studiem
  {
    userId: DEMO_USERS.anna.userId,
    email: DEMO_USERS.anna.email,
    firstName: DEMO_USERS.anna.firstName,
    lastName: DEMO_USERS.anna.lastName,
    roleCode: RoleCodes.TENANT_ADMIN,
    isActive: true,
    joinedAt: "2024-03-01T08:00:00Z",
  },
  {
    userId: "user-studio-02",
    email: "b.dabrowska@studio-rew.pl",
    firstName: "Barbara",
    lastName: "Dąbrowska",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2024-03-05T09:00:00Z",
  },
  {
    userId: "user-studio-03",
    email: "k.mazur@studio-rew.pl",
    firstName: "Krzysztof",
    lastName: "Mazur",
    roleCode: RoleCodes.TENANT_MEMBER,
    isActive: true,
    joinedAt: "2024-04-10T10:00:00Z",
  },
];

export const DEMO_TENANT_STUDIO: UserTenant = {
  id: DEMO_TENANT_STUDIO_ID,
  name: "Studio Rewitalizacji Sp. z o.o.",
  createdAt: "2024-03-01T08:00:00Z",
  isActive: true,
  roleCode: RoleCodes.TENANT_ADMIN,
  isActiveTenant: false,
};

export const DEMO_TENANT_STUDIO_DETAILS: TenantDetails = {
  id: DEMO_TENANT_STUDIO_ID,
  name: "Studio Rewitalizacji Sp. z o.o.",
  createdAt: "2024-03-01T08:00:00Z",
  roleCode: RoleCodes.TENANT_ADMIN,
  isActive: true,
  members: STUDIO_MEMBERS,
  invitations: [],
};

// ===== ZBIORCZY SŁOWNIK szczegółów wszystkich tenantów =====

export const ALL_TENANT_DETAILS: Record<string, TenantDetails> = {
  [DEMO_TENANT_ID]:            DEMO_TENANT_DETAILS,
  [DEMO_TENANT_BUDPROJEKT_ID]: DEMO_TENANT_BUDPROJEKT_DETAILS,
  [DEMO_TENANT_INFRA_ID]:      DEMO_TENANT_INFRA_DETAILS,
  [DEMO_TENANT_STUDIO_ID]:     DEMO_TENANT_STUDIO_DETAILS,
};

// ===== ZAPROSZENIA ODEBRANE przez Annę (od innych organizacji) =====
// Używane przez GET /tenant/invitations — strona /tenants/invitations

export const DEMO_ANNA_RECEIVED_INVITATIONS: TenantInvitationWeb[] = [
  // Zaproszenie od Ekobud — oczekuje na akceptację
  {
    invitationId: "inv-received-001",
    tenantId: "tenant-ekobud-099",
    tenantName: "Ekobud Consortium Sp. z o.o.",
    email: DEMO_USERS.anna.email,
    invitedByUserEmail: "d.wojcik@ekobud.pl",
    invitedByUserName: "Dariusz Wójcik",
    createdAt: isoDate(-3),
    expiresAt: isoDate(+27),
    status: InvitationStatus.Pending,
    token: "tok-ekobud-inv-anna-001",
  },
  // Zaproszenie od GreenCity — oczekuje na akceptację
  {
    invitationId: "inv-received-002",
    tenantId: "tenant-greencity-088",
    tenantName: "GreenCity Development",
    email: DEMO_USERS.anna.email,
    invitedByUserEmail: "e.nowak@greencity.pl",
    invitedByUserName: "Elżbieta Nowak",
    createdAt: isoDate(-1),
    expiresAt: isoDate(+29),
    status: InvitationStatus.Pending,
    token: "tok-greencity-inv-anna-001",
  },
];

/** Słownik userId → wyświetlana nazwa  */
export const DEMO_USER_DISPLAY_NAMES: Record<string, string> = {
  [DEMO_USERS.anna.userId]: "Anna Kowalska",
  [DEMO_USERS.piotr.userId]: "Piotr Wiśniewski",
  [DEMO_USERS.marta.userId]: "Marta Nowak",
  [DEMO_USERS.tomasz.userId]: "Tomasz Zając",
  [DEMO_USERS.katarzyna.userId]: "Katarzyna Wójcik",
};

