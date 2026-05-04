import { useQuery } from '@tanstack/react-query';
import { workScheduleApi } from '../../api/workScheduleApi';
import { projectApi } from '../../api/projectApi';
import type {
  WorkScheduleDetailsWeb,
  UserAssignedWorksByTenantWeb,
} from '../../types/workSchedule.types';

export const workScheduleKeys = {
  all: ['work-schedule'] as const,
  details: (tenantId: string, projectId: string, wsId: string) =>
    ['work-schedule', tenantId, projectId, 'details', wsId] as const,
  myAssignedWorks: () => ['work-schedule', 'my-assigned-works'] as const,
};

export function useWorkScheduleDetails(
  tenantId: string | undefined,
  projectId: string | undefined,
  workScheduleId: string | undefined
) {
  return useQuery<WorkScheduleDetailsWeb>({
    queryKey: workScheduleKeys.details(
      tenantId ?? '',
      projectId ?? '',
      workScheduleId ?? ''
    ),
    queryFn: async () => {
      const response = await workScheduleApi.getDetails(
        tenantId!, projectId!, workScheduleId!
      );
      return response.data;
    },
    enabled: Boolean(tenantId && projectId && workScheduleId),
  });
}

export function useMyAssignedWorks() {
  return useQuery<UserAssignedWorksByTenantWeb[]>({
    queryKey: workScheduleKeys.myAssignedWorks(),
    queryFn: async () => {
      const response = await projectApi.getMyAssignedWorks();
      return (response.data as UserAssignedWorksByTenantWeb[]) ?? [];
    },
  });
}
