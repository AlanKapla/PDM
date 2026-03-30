/**
 * Dane demonstracyjne projektów, członków projektów i plików.
 */

import type {
  ProjectDetailsWeb,
  ProjectMemberWeb,
  ProjectFilePackageWeb,
  ProjectFileWeb,
} from "../../types/project.types";
import { RoleCodes, PermissionCodes } from "../../constants/roleCodes";
import { DEMO_TENANT_ID, DEMO_USERS } from "./users";

// ===== ZESTAWY UPRAWNIEŃ PER ROLA W PROJEKCIE =====

/** Administrator projektu — pełny dostęp do wszystkiego */
const ADMIN_PERMISSIONS = [
  PermissionCodes.PROJECT_VIEW,
  PermissionCodes.PROJECT_EDIT,
  PermissionCodes.PROJECT_MEMBERS_VIEW,
  PermissionCodes.PROJECT_MEMBERS_MANAGE,
  PermissionCodes.PROJECT_STATUS_MANAGE,
  PermissionCodes.PROJECT_RESOURCES_READ,
  PermissionCodes.PROJECT_RESOURCES_WRITE,
  PermissionCodes.PROJECT_RESOURCES_READ_SHARED,
  PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED,
  PermissionCodes.PROJECT_RESOURCES_READ_ALL,
  PermissionCodes.PROJECT_RESOURCES_WRITE_ALL,
  PermissionCodes.PROJECT_RESOURCES_SHARE,
  PermissionCodes.PROJECT_MESSAGES_READ,
  PermissionCodes.PROJECT_MESSAGES_WRITE,
  PermissionCodes.PROJECT_MESSAGES_DELETE,
  PermissionCodes.ROLE_LIST,
];

/** Edytor — może tworzyć, edytować własne zasoby i przeglądać udostępnione */
const EDITOR_PERMISSIONS = [
  PermissionCodes.PROJECT_VIEW,
  PermissionCodes.PROJECT_MEMBERS_VIEW,
  PermissionCodes.PROJECT_RESOURCES_READ,
  PermissionCodes.PROJECT_RESOURCES_WRITE,
  PermissionCodes.PROJECT_RESOURCES_READ_SHARED,
  PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED,
  PermissionCodes.PROJECT_RESOURCES_SHARE,
  PermissionCodes.PROJECT_MESSAGES_READ,
  PermissionCodes.PROJECT_MESSAGES_WRITE,
];

/** Przeglądający — tylko odczyt własnych i udostępnionych zasobów */
const VIEWER_PERMISSIONS = [
  PermissionCodes.PROJECT_VIEW,
  PermissionCodes.PROJECT_MEMBERS_VIEW,
  PermissionCodes.PROJECT_RESOURCES_READ,
  PermissionCodes.PROJECT_RESOURCES_READ_SHARED,
  PermissionCodes.PROJECT_MESSAGES_READ,
];

// ===== PROJEKTY =====

export const DEMO_PROJECT_IDS = {
  biurowe: "proj-biurowe-001",
  drogowa: "proj-drogowa-002",
  hotel: "proj-hotel-003",
} as const;

export const DEMO_PROJECTS: ProjectDetailsWeb[] = [
  {
    id: DEMO_PROJECT_IDS.biurowe,
    tenantId: DEMO_TENANT_ID,
    name: "Centrum Biurowe Nowa Brama",
    isActive: true,
    createdAt: "2023-02-01T09:00:00Z",
    createdByUserId: DEMO_USERS.anna.userId,
    createdByUserName: "Anna Kowalska",
    // Anna jest administratorem projektu
    userRoleCode: RoleCodes.PROJECT_ADMIN,
    membersCount: 4,
    userPermissions: ADMIN_PERMISSIONS,
  },
  {
    id: DEMO_PROJECT_IDS.drogowa,
    tenantId: DEMO_TENANT_ID,
    name: "Modernizacja Drogi Ekspresowej DK7",
    isActive: true,
    createdAt: "2023-05-15T10:00:00Z",
    createdByUserId: DEMO_USERS.piotr.userId,
    createdByUserName: "Piotr Wiśniewski",
    // Anna jest edytorem w tym projekcie
    userRoleCode: RoleCodes.PROJECT_EDITOR,
    membersCount: 3,
    userPermissions: EDITOR_PERMISSIONS,
  },
  {
    id: DEMO_PROJECT_IDS.hotel,
    tenantId: DEMO_TENANT_ID,
    name: "Hotel Panorama – Rozbudowa Skrzydła B",
    isActive: true,
    createdAt: "2023-08-20T11:00:00Z",
    createdByUserId: DEMO_USERS.anna.userId,
    createdByUserName: "Anna Kowalska",
    // Anna jest edytorem w tym projekcie
    userRoleCode: RoleCodes.PROJECT_EDITOR,
    membersCount: 5,
    userPermissions: EDITOR_PERMISSIONS,
  },
];

// ===== CZŁONKOWIE PROJEKTÓW =====

const makeProjectMembers = (
  entries: Array<{ user: (typeof DEMO_USERS)[keyof typeof DEMO_USERS]; roleCode: string; joinedAt: string }>
): ProjectMemberWeb[] =>
  entries.map(({ user, roleCode, joinedAt }) => ({
    userId: user.userId,
    email: user.email,
    firstName: user.firstName,
    lastName: user.lastName,
    roleCode,
    joinedAt,
  }));

export const DEMO_PROJECT_MEMBERS: Record<string, ProjectMemberWeb[]> = {
  [DEMO_PROJECT_IDS.biurowe]: makeProjectMembers([
    { user: DEMO_USERS.anna,     roleCode: RoleCodes.PROJECT_ADMIN,  joinedAt: "2023-02-01T09:00:00Z" },
    { user: DEMO_USERS.piotr,    roleCode: RoleCodes.PROJECT_EDITOR, joinedAt: "2023-02-10T10:00:00Z" },
    { user: DEMO_USERS.marta,    roleCode: RoleCodes.PROJECT_EDITOR, joinedAt: "2023-03-15T11:00:00Z" },
    { user: DEMO_USERS.tomasz,   roleCode: RoleCodes.PROJECT_VIEWER, joinedAt: "2023-04-10T12:00:00Z" },
  ]),
  [DEMO_PROJECT_IDS.drogowa]: makeProjectMembers([
    { user: DEMO_USERS.piotr,    roleCode: RoleCodes.PROJECT_ADMIN,  joinedAt: "2023-05-15T10:00:00Z" },
    { user: DEMO_USERS.anna,     roleCode: RoleCodes.PROJECT_EDITOR, joinedAt: "2023-05-16T09:00:00Z" },
    { user: DEMO_USERS.tomasz,   roleCode: RoleCodes.PROJECT_EDITOR, joinedAt: "2023-06-01T11:00:00Z" },
  ]),
  [DEMO_PROJECT_IDS.hotel]: makeProjectMembers([
    { user: DEMO_USERS.piotr,    roleCode: RoleCodes.PROJECT_ADMIN,  joinedAt: "2023-08-20T11:00:00Z" },
    { user: DEMO_USERS.anna,     roleCode: RoleCodes.PROJECT_EDITOR, joinedAt: "2023-08-20T11:00:00Z" },
    { user: DEMO_USERS.marta,    roleCode: RoleCodes.PROJECT_EDITOR, joinedAt: "2023-09-01T10:00:00Z" },
    { user: DEMO_USERS.tomasz,   roleCode: RoleCodes.PROJECT_EDITOR, joinedAt: "2023-09-05T09:00:00Z" },
    { user: DEMO_USERS.katarzyna, roleCode: RoleCodes.PROJECT_VIEWER, joinedAt: "2023-10-01T08:00:00Z" },
  ]),
};

// ===== PACZKI PLIKÓW =====

const makePkg = (
  id: string,
  name: string,
  ownerId: string,
  ownerName: string,
  createdAt: string,
  files: ProjectFileWeb[]
): ProjectFilePackageWeb => ({
  id,
  name,
  createdAt,
  ownerId,
  ownerName,
  files,
  totalFiles: files.length,
});

const makeFile = (
  id: string,
  fileName: string,
  displayName: string,
  packageName: string,
  ownerId: string,
  ownerName: string,
  createdAt: string,
  sizeBytes: number,
  contentType = "application/pdf",
  isShared = false
): ProjectFileWeb => ({
  id,
  fileName,
  displayName,
  packageName,
  createdAt,
  ownerId,
  ownerName,
  currentVersion: {
    id: `${id}-v1`,
    projectFileId: id,
    versionNumber: 1,
    contentType,
    fileSizeBytes: sizeBytes,
    createdAt,
    createdByUserId: ownerId,
    createdByUserName: ownerName,
    sasUrlView: "",
    sasUrlDownload: "",
    comments: [],
  },
  versions: [],
  totalVersions: 1,
  isOwner: ownerId === DEMO_USERS.anna.userId,
  isShared,
  sharedWithUserIds: [],
});

export const DEMO_FILE_PACKAGES: Record<string, ProjectFilePackageWeb[]> = {
  [DEMO_PROJECT_IDS.biurowe]: [
    makePkg(
      "pkg-biurowe-001",
      "Dokumentacja projektowa – etap I",
      DEMO_USERS.anna.userId,
      "Anna Kowalska",
      "2023-03-10T09:00:00Z",
      [
        makeFile("file-biu-001", "projekt_architektoniczny_v3.pdf", "Projekt architektoniczny v3", "Dokumentacja projektowa – etap I", DEMO_USERS.anna.userId, "Anna Kowalska", "2023-03-10T09:00:00Z", 8453120),
        makeFile("file-biu-002", "projekt_konstrukcyjny.pdf", "Projekt konstrukcyjny", "Dokumentacja projektowa – etap I", DEMO_USERS.anna.userId, "Anna Kowalska", "2023-03-12T10:00:00Z", 12288000),
        makeFile("file-biu-003", "pozwolenie_na_budowe.pdf", "Pozwolenie na budowę", "Dokumentacja projektowa – etap I", DEMO_USERS.piotr.userId, "Piotr Wiśniewski", "2023-03-20T14:00:00Z", 2097152),
      ]
    ),
    makePkg(
      "pkg-biurowe-002",
      "Wizualizacje 3D",
      DEMO_USERS.marta.userId,
      "Marta Nowak",
      "2023-04-05T11:00:00Z",
      [
        makeFile("file-biu-004", "wizualizacja_elewacja_polnoc.png", "Wizualizacja – elewacja północna", "Wizualizacje 3D", DEMO_USERS.marta.userId, "Marta Nowak", "2023-04-05T11:00:00Z", 5242880, "image/png"),
        makeFile("file-biu-005", "wizualizacja_atrium.png", "Wizualizacja – atrium", "Wizualizacje 3D", DEMO_USERS.marta.userId, "Marta Nowak", "2023-04-06T10:00:00Z", 6291456, "image/png"),
      ]
    ),
  ],
  [DEMO_PROJECT_IDS.drogowa]: [
    makePkg(
      "pkg-drogowa-001",
      "Dokumentacja techniczna",
      DEMO_USERS.piotr.userId,
      "Piotr Wiśniewski",
      "2023-06-10T09:00:00Z",
      [
        makeFile("file-drg-001", "projekt_drogowy_km0-km12.pdf", "Projekt drogowy km 0+000 – km 12+500", "Dokumentacja techniczna", DEMO_USERS.piotr.userId, "Piotr Wiśniewski", "2023-06-10T09:00:00Z", 15728640),
        makeFile("file-drg-002", "geotechnika_raport.pdf", "Raport geotechniczny", "Dokumentacja techniczna", DEMO_USERS.tomasz.userId, "Tomasz Zając", "2023-06-15T10:00:00Z", 4194304),
      ]
    ),
    makePkg(
      "pkg-drogowa-002",
      "Harmonogramy i organizacja robót",
      DEMO_USERS.anna.userId,
      "Anna Kowalska",
      "2023-07-01T08:00:00Z",
      [
        makeFile("file-drg-003", "harmonogram_generalny_v2.xlsx", "Harmonogram generalny v2", "Harmonogramy i organizacja robót", DEMO_USERS.anna.userId, "Anna Kowalska", "2023-07-01T08:00:00Z", 1048576, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
      ]
    ),
  ],
  [DEMO_PROJECT_IDS.hotel]: [
    makePkg(
      "pkg-hotel-001",
      "Projekt rozbudowy – Skrzydło B",
      DEMO_USERS.piotr.userId,
      "Piotr Wiśniewski",
      "2023-09-10T10:00:00Z",
      [
        makeFile("file-htl-001", "projekt_budowlany_skrzydlo_B.pdf", "Projekt budowlany – Skrzydło B", "Projekt rozbudowy – Skrzydło B", DEMO_USERS.piotr.userId, "Piotr Wiśniewski", "2023-09-10T10:00:00Z", 18874368),
        makeFile("file-htl-002", "projekt_wnętrz_piętro1-5.pdf", "Projekt wnętrz – piętra 1–5", "Projekt rozbudowy – Skrzydło B", DEMO_USERS.marta.userId, "Marta Nowak", "2023-09-15T11:00:00Z", 9437184),
      ]
    ),
  ],
};
