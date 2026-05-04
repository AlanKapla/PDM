import { useQuery } from '@tanstack/react-query';
import { projectApi } from '../../api/projectApi';
import type { ProjectDetailsWeb } from '../../types/project.types';
import { projectKeys } from './useProjectDetails';

export function useProjects(tenantId: string | undefined) {
  return useQuery<ProjectDetailsWeb[]>({
    queryKey: projectKeys.list(tenantId ?? ''),
    queryFn: async () => {
      const response = await projectApi.getTenantProjects(tenantId!);
      return response.data;
    },
    enabled: Boolean(tenantId),
  });
}
