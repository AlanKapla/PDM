import { useQuery } from '@tanstack/react-query';
import { projectApi } from '../../api/projectApi';
import type { ProjectDetailsWeb } from '../../types/project.types';

export const projectKeys = {
  all: ['projects'] as const,
  detail: (tenantId: string, projectId: string) =>
    ['projects', tenantId, projectId] as const,
  list: (tenantId: string) =>
    ['projects', tenantId] as const,
  members: (tenantId: string, projectId: string) =>
    ['projects', tenantId, projectId, 'members'] as const,
};

export function useProjectDetails(
  tenantId: string | undefined,
  projectId: string | undefined
) {
  return useQuery<ProjectDetailsWeb>({
    queryKey: projectKeys.detail(tenantId ?? '', projectId ?? ''),
    queryFn: async () => {
      const response = await projectApi.getProjectDetails(tenantId!, projectId!);
      return response.data;
    },
    enabled: Boolean(tenantId && projectId),
  });
}
