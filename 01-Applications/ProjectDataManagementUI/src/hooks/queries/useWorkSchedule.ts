import { useQuery } from '@tanstack/react-query';
import type { QueryClient } from '@tanstack/react-query';
import { workScheduleApi } from '../../api/workScheduleApi';
import { projectApi, ResourceScope } from '../../api/projectApi';
import type {
  WorkScheduleDetailsWeb,
  WorkScheduleStageWeb,
  WorkScheduleSummaryWeb,
  UserAssignedWorksByTenantWeb,
} from '../../types/workSchedule.types';

export const workScheduleKeys = {
  all: ['work-schedule'] as const,
  lists: (tenantId: string, projectId: string) =>
    ['work-schedule', tenantId, projectId, 'list'] as const,
  list: (tenantId: string, projectId: string, scope: ResourceScope) =>
    ['work-schedule', tenantId, projectId, 'list', scope] as const,
  details: (tenantId: string, projectId: string, wsId: string) =>
    ['work-schedule', tenantId, projectId, 'details', wsId] as const,
  allWorks: (tenantId: string, projectId: string) =>
    ['work-schedule', tenantId, projectId, 'all-works'] as const,
  myAssignedWorks: () => ['work-schedule', 'my-assigned-works'] as const,
};

/** Invaliduje listy harmonogramów projektu oraz cache pozycji prac (all-works). */
export function invalidateWorkScheduleLists(
  queryClient: QueryClient,
  tenantId: string,
  projectId: string,
): Promise<void[]> {
  return Promise.all([
    queryClient.invalidateQueries({
      queryKey: workScheduleKeys.lists(tenantId, projectId),
    }),
    queryClient.invalidateQueries({
      queryKey: workScheduleKeys.allWorks(tenantId, projectId),
    }),
  ]);
}

export function useWorkSchedulesByScope(
  tenantId: string | undefined,
  projectId: string | undefined,
  scope: ResourceScope,
  enabled: boolean = true,
) {
  return useQuery<WorkScheduleSummaryWeb[]>({
    queryKey: workScheduleKeys.list(tenantId ?? '', projectId ?? '', scope),
    queryFn: async () => {
      const response = await projectApi.getWorkSchedules(tenantId!, projectId!, scope);
      return response.data;
    },
    enabled: Boolean(tenantId && projectId && enabled),
    staleTime: 0,
  });
}

export interface FlatWorkItem {
  workId: string;
  workName: string;
  stageId: string;
  stageName: string;
  scheduleId: string;
  scheduleName: string;
  costEstimateItemId: string | null;
  label: string;
}

function flattenStageWorks(
  stages: WorkScheduleStageWeb[],
  scheduleName: string,
  scheduleId: string,
  result: FlatWorkItem[],
  parentPath = ''
): void {
  for (const stage of stages) {
    const stagePath = parentPath ? `${parentPath} > ${stage.name}` : stage.name;
    for (const work of stage.works) {
      result.push({
        workId: work.id,
        workName: work.name,
        stageId: stage.id,
        stageName: stage.name,
        scheduleId,
        scheduleName,
        costEstimateItemId: work.costEstimateItemId ?? null,
        label: `${scheduleName} › ${stagePath} › ${work.name}`,
      });
    }
    if (stage.childStages && stage.childStages.length > 0) {
      flattenStageWorks(stage.childStages, scheduleName, scheduleId, result, stagePath);
    }
  }
}

export function useProjectWorkItems(
  tenantId: string | undefined,
  projectId: string | undefined
) {
  return useQuery<FlatWorkItem[]>({
    queryKey: workScheduleKeys.allWorks(tenantId ?? '', projectId ?? ''),
    queryFn: async () => {
      const summariesRes = await projectApi.getWorkSchedules(tenantId!, projectId!, ResourceScope.All);
      const summaries: WorkScheduleSummaryWeb[] = summariesRes.data;
      if (!summaries || summaries.length === 0) return [];
      const details = await Promise.all(
        summaries.map((s) =>
          workScheduleApi.getDetails(tenantId!, projectId!, s.id).then((r) => r.data as WorkScheduleDetailsWeb)
        )
      );
      const result: FlatWorkItem[] = [];
      for (const detail of details) {
        flattenStageWorks(detail.stages, detail.name, detail.id, result);
      }
      return result;
    },
    enabled: Boolean(tenantId && projectId),
    staleTime: 1000 * 60 * 5,
  });
}

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
