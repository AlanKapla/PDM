import { useQuery } from '@tanstack/react-query';
import { projectApi } from '../../api/projectApi';
import type { ProjectMemberWeb } from '../../types/project.types';
import { projectKeys } from './useProjectDetails';

export function useProjectMembers(
  tenantId: string | undefined,
  projectId: string | undefined
) {
  return useQuery<ProjectMemberWeb[]>({
    queryKey: projectKeys.members(tenantId ?? '', projectId ?? ''),
    queryFn: async () => {
      const response = await projectApi.getProjectMembers(tenantId!, projectId!);
      return response.data;
    },
    enabled: Boolean(tenantId && projectId),
  });
}
