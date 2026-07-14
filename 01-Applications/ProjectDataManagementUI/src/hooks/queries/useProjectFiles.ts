import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { projectApi, ResourceScope } from '../../api/projectApi';
import type {
  ProjectFilePackageWeb,
  ProjectFileVersionWeb,
  ProjectFileVersionCommentWeb,
  ProjectFileWeb,
} from '../../types/project.types';

export const fileKeys = {
  all: ['project-files'] as const,
  packages: (tenantId: string, projectId: string, scope: ResourceScope) =>
    ['project-files', tenantId, projectId, 'packages', scope] as const,
  packageFiles: (
    tenantId: string,
    projectId: string,
    packageId: string,
    scope: ResourceScope
  ) =>
    ['project-files', tenantId, projectId, 'package-files', packageId, scope] as const,
  fileVersions: (
    tenantId: string,
    projectId: string,
    fileId: string,
    scope: ResourceScope
  ) =>
    ['project-files', tenantId, projectId, 'versions', fileId, scope] as const,
  versionComments: (
    tenantId: string,
    projectId: string,
    fileId: string,
    versionId: string,
    scope: ResourceScope
  ) =>
    ['project-files', tenantId, projectId, 'comments', fileId, versionId, scope] as const,
};

export function useFilePackages(
  tenantId: string | undefined,
  projectId: string | undefined,
  scope: ResourceScope,
  enabled: boolean = true
) {
  return useQuery<ProjectFilePackageWeb[]>({
    queryKey: fileKeys.packages(tenantId ?? '', projectId ?? '', scope),
    queryFn: async () => {
      const response = await projectApi.getProjectFilePackages(
        tenantId!, projectId!, scope
      );
      return response.data;
    },
    enabled: Boolean(tenantId && projectId) && enabled,
  });
}

export function usePackageFiles(
  tenantId: string | undefined,
  projectId: string | undefined,
  packageId: string | undefined,
  scope: ResourceScope,
  enabled: boolean = true
) {
  return useQuery<ProjectFileWeb[]>({
    queryKey: fileKeys.packageFiles(
      tenantId ?? '', projectId ?? '', packageId ?? '', scope
    ),
    queryFn: async () => {
      const response = await projectApi.getPackageFiles(
        tenantId!, projectId!, packageId!, scope
      );
      return response.data;
    },
    enabled: Boolean(tenantId && projectId && packageId) && enabled,
  });
}

export function useFileVersions(
  tenantId: string | undefined,
  projectId: string | undefined,
  fileId: string | undefined,
  scope: ResourceScope,
  enabled: boolean = true
) {
  return useQuery<ProjectFileVersionWeb[]>({
    queryKey: fileKeys.fileVersions(
      tenantId ?? '', projectId ?? '', fileId ?? '', scope
    ),
    queryFn: async () => {
      const response = await projectApi.getFileVersions(
        tenantId!, projectId!, fileId!, scope
      );
      return response.data;
    },
    enabled: Boolean(tenantId && projectId && fileId) && enabled,
  });
}

export function useVersionComments(
  tenantId: string | undefined,
  projectId: string | undefined,
  fileId: string | undefined,
  versionId: string | undefined,
  scope: ResourceScope,
  enabled: boolean = true
) {
  return useQuery<ProjectFileVersionCommentWeb[]>({
    queryKey: fileKeys.versionComments(
      tenantId ?? '', projectId ?? '', fileId ?? '', versionId ?? '', scope
    ),
    queryFn: async () => {
      const response = await projectApi.getVersionComments(
        tenantId!, projectId!, fileId!, versionId!, scope
      );
      return response.data;
    },
    enabled: Boolean(tenantId && projectId && fileId && versionId) && enabled,
  });
}

export function useCreateDirectory() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      tenantId,
      projectId,
      directoryName,
      parentId,
    }: {
      tenantId: string;
      projectId: string;
      directoryName: string;
      parentId?: string | null;
    }) => projectApi.createDirectory(tenantId, projectId, directoryName, parentId),

    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: fileKeys.all });
    },
  });
}
