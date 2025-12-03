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
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
  uploadedByUserId: string;
  uploadedByUserName: string;
  sasUrl: string;
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
}
