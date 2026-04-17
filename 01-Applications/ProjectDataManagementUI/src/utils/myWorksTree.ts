import type {
  UserAssignedWorksByTenantWeb,
  UserAssignedWorkWeb,
} from "../types/workSchedule.types";

// ─── Immutable update helper ─────────────────────────────────────────────────

type WorkUpdater = (work: UserAssignedWorkWeb) => UserAssignedWorkWeb;

export const updateWorkInTree = (
  data: UserAssignedWorksByTenantWeb[],
  workId: string,
  updater: WorkUpdater
): UserAssignedWorksByTenantWeb[] =>
  data.map(tenant => ({
    ...tenant,
    projects: tenant.projects.map(project => ({
      ...project,
      workSchedules: project.workSchedules.map(ws => ({
        ...ws,
        stages: ws.stages.map(stage => ({
          ...stage,
          works: stage.works.map(work =>
            work.workId === workId ? updater(work) : work
          ),
        })),
      })),
    })),
  }));

// ─── Spłaszczona reprezentacja prac — zachowuje wszystkie ID potrzebne do URL mutacji ─

export interface FlatWork extends UserAssignedWorkWeb {
  tenantId: string;
  tenantName: string;
  projectId: string;
  projectName: string;
  scheduleId: string;
  scheduleName: string;
  stageId: string;
  stageName: string;
}

export const flattenWorks = (data: UserAssignedWorksByTenantWeb[]): FlatWork[] => {
  const result: FlatWork[] = [];
  for (const tenant of data) {
    for (const project of tenant.projects) {
      for (const ws of project.workSchedules) {
        for (const stage of ws.stages) {
          for (const work of stage.works) {
            result.push({
              ...work,
              tenantId: tenant.tenantId,
              tenantName: tenant.tenantName,
              projectId: project.projectId,
              projectName: project.projectName,
              scheduleId: ws.workScheduleId,
              scheduleName: ws.workScheduleName,
              stageId: stage.stageId,
              stageName: stage.stageName,
            });
          }
        }
      }
    }
  }
  return result;
};
