// ===== Roles =====

export const ProjectRole = {
  Admin: 0,
  Member: 1,
} as const;

export type ProjectRoleType = (typeof ProjectRole)[keyof typeof ProjectRole];

// Re-export TenantRole z auth.types dla wygody
export { TenantRole } from './auth.types';

// ===== Interfaces =====

export interface TenantMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
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
  role: number;
  joinedAt: string;
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
