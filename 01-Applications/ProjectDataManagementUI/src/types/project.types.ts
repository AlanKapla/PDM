// ===== Roles =====

export const ProjectRole = {
  Admin: 0,
  Editor: 1,
  Viewer: 2,
} as const;

export type ProjectRoleType = (typeof ProjectRole)[keyof typeof ProjectRole];

// Funkcja pomocnicza do określania poziomu roli (im niższa wartość, tym wyższe uprawnienia)
// Wartości enum są już bezpośrednio poziomami uprawnień
export const getProjectRoleLevel = (role: number): number => {
  return role;
};

// Funkcje pomocnicze do sprawdzania uprawnień
export const hasProjectRoleLevel = (userRole: number, requiredRole: number): boolean => {
  return getProjectRoleLevel(userRole) <= getProjectRoleLevel(requiredRole);
};

export const isProjectAdmin = (userRole: number): boolean => {
  return userRole === ProjectRole.Admin;
};

export const canEditProject = (userRole: number): boolean => {
  return hasProjectRoleLevel(userRole, ProjectRole.Editor);
};

export const canViewProject = (userRole: number): boolean => {
  return hasProjectRoleLevel(userRole, ProjectRole.Viewer);
};

// Re-export TenantRole z auth.types dla wygody
export { TenantRole } from './auth.types';

// ===== Interfaces =====

export interface ProjectDetailsWeb {
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  userRoleCode: string;
  membersCount: number;
  userPermissions: string[];  // User's permissions for this specific project
}

export interface TenantMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;
  isActive: boolean;
  joinedAt: string;
}

export interface AddProjectMemberCommand {
  tenantId: string;
  projectId: string;
  userId: string;
}

export interface ProjectMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleCode: string;
  joinedAt: string;
}

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

// ===== Koszty projektowe =====

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
  isClosed?: boolean;
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
