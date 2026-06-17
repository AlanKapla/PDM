import { useQuery } from '@tanstack/react-query';
import type { QueryClient } from '@tanstack/react-query';
import { projectApi, ResourceScope } from '../../api/projectApi';
import type { ProjectCostListItemWeb } from '../../types/project.types';

export const projectCostKeys = {
  all: ['project-cost'] as const,
  lists: (tenantId: string, projectId: string) =>
    ['project-cost', tenantId, projectId, 'list'] as const,
  list: (tenantId: string, projectId: string, scope: ResourceScope) =>
    ['project-cost', tenantId, projectId, 'list', scope] as const,
};

/** Invaliduje wszystkie listy wydatków projektu (All / Mine / PendingApproval). */
export function invalidateProjectCostLists(
  queryClient: QueryClient,
  tenantId: string,
  projectId: string,
): Promise<void> {
  return queryClient.invalidateQueries({
    queryKey: projectCostKeys.lists(tenantId, projectId),
  });
}

export function useProjectCostsByScope(
  tenantId: string | undefined,
  projectId: string | undefined,
  scope: ResourceScope,
  enabled: boolean = true,
) {
  return useQuery<ProjectCostListItemWeb[]>({
    queryKey: projectCostKeys.list(tenantId ?? '', projectId ?? '', scope),
    queryFn: async () => {
      const response = await projectApi.getProjectCosts(tenantId!, projectId!, scope);
      return response.data;
    },
    enabled: Boolean(tenantId && projectId && enabled),
    staleTime: 0,
  });
}
