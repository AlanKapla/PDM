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

export interface ProjectCurrencyWeb {
  code: string;
  name: string;
  symbol?: string;
}

export interface ProjectDetailsWeb {
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  isAdmin: boolean;
  canViewAllResources: boolean;
  membersCount: number;
  userPermissions: string[];  // User's permissions for this specific project
  currency?: ProjectCurrencyWeb;
}

export interface SetProjectCurrencyRequest {
  code: string;
  name: string;
  symbol?: string;
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
  joinedAt: string;
  isAdmin: boolean;
  modules: number[];
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

export type CostApprovalStatus = 'Draft' | 'PendingApproval' | 'Approved';

export interface ProjectCostListItemWeb {
  id: string;
  userId: string;
  userName: string;
  name: string;
  number: string | null;
  contractorId: string | null;
  contractorName: string | null;
  date: string;
  description?: string;
  net: number | null;
  gross: number | null;
  approvalStatus: CostApprovalStatus;
  approvedByUserId: string | null;
  approvedAt: string | null;
  hasDocument: boolean;
  documentFileName?: string;
  previewSasUrl?: string;
  downloadSasUrl?: string;
  createdAt: string;
}

export interface CreateProjectCostCommand {
  tenantId: string;
  projectId: string;
  name: string;
  number?: string | null;
  contractorId?: string | null;
  date: string;
  description?: string;
  net?: number | null;
  gross?: number | null;
  document?: File;
}

export interface UpdateProjectCostCommand {
  tenantId: string;
  projectId: string;
  costId: string;
  name: string;
  number?: string | null;
  contractorId?: string | null;
  date: string;
  description?: string;
  net?: number | null;
  gross?: number | null;
  /** Nowy dokument dołączany do kosztu który nie miał wcześniej pliku */
  document?: File;
  /** Nowy plik zastępujący istniejący dokument */
  updatedDocument?: File;
  removeDocument: boolean;
}


