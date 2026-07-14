import { useQuery } from '@tanstack/react-query';
import { projectApi } from '../../api/projectApi';
import type { ProjectInvitationWeb } from '../../types/project.types';

export const projectInvitationKeys = {
  all: ['projectInvitations'] as const,
  active: () => ['projectInvitations', 'active'] as const,
  byProject: (tenantId: string, projectId: string) =>
    ['projectInvitations', tenantId, projectId] as const,
};

export function useActiveProjectInvitations(options?: {
  refetchInterval?: number;
  refetchIntervalInBackground?: boolean;
}) {
  return useQuery<ProjectInvitationWeb[]>({
    queryKey: projectInvitationKeys.active(),
    queryFn: () => projectApi.getActiveProjectInvitations(),
    refetchInterval: options?.refetchInterval,
    refetchIntervalInBackground: options?.refetchIntervalInBackground ?? false,
  });
}

export function useProjectInvitations(tenantId: string, projectId: string, enabled = true) {
  return useQuery<ProjectInvitationWeb[]>({
    queryKey: projectInvitationKeys.byProject(tenantId, projectId),
    queryFn: () => projectApi.getProjectInvitations(tenantId, projectId),
    enabled: enabled && !!tenantId && !!projectId,
  });
}
