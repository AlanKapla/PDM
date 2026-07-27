// Typ zależności między zakresami prac
export enum WorkDependencyType {
  FinishToStart = 0,
  StartToStart = 1,
  FinishToFinish = 2,
  StartToFinish = 3,
}

export const WorkDependencyTypeLabels: Record<WorkDependencyType, string> = {
  [WorkDependencyType.FinishToStart]: 'FS – Koniec → Start',
  [WorkDependencyType.StartToStart]: 'SS – Start → Start',
  [WorkDependencyType.FinishToFinish]: 'FF – Koniec → Koniec',
  [WorkDependencyType.StartToFinish]: 'SF – Start → Koniec',
};

// Zależność — DTO wejściowe (żądanie)
export interface WorkScheduleWorkDependencyDto {
  predecessorDbId?: string;
  predecessorTempId?: string;
  successorDbId?: string;
  successorTempId?: string;
  dependencyType: WorkDependencyType;
  lagDays: number;
}

// Request DTOs
export interface CreateWorkPeriodDto {
  startDate: string;
  endDate: string;
  isClosed: boolean;
}

export interface CreateWorkCommentDto {
  content: string;
}

export interface CreateWorkDto {
  tempId?: string;
  name: string;
  order: number;
  colorRgb: string;
  isClosed: boolean;
  periods: CreateWorkPeriodDto[];
  assignedUserIds: string[];
  comments: CreateWorkCommentDto[];
}

export interface CreateStageDto {
  name: string;
  order: number;
  works: CreateWorkDto[];
  children?: CreateStageDto[];
}

export interface CreateWorkScheduleCommand {
  tenantId: string;
  projectId: string;
  name: string;
  costEstimateId?: string | null;
  stages: CreateStageDto[];
  dependencies?: WorkScheduleWorkDependencyDto[];
}

export interface GenerateScheduleFromEstimateAIRequest {
    /** ISO 8601 date string — overall project start date */
    overallStartDate: string;
    /** ISO 8601 date string — overall project end date */
    overallEndDate: string;
}

export interface UpdateWorkPeriodDto {
  id?: string;
  startDate: string;
  endDate: string;
  isClosed: boolean;
}

export interface UpdateWorkCommentDto {
  id?: string;
  content: string;
}

export interface UpdateWorkDto {
  id?: string;
  tempId?: string;
  name: string;
  order: number;
  colorRgb: string;
  isClosed: boolean;
  periods: UpdateWorkPeriodDto[];
  assignedUserIds: string[];
  comments: UpdateWorkCommentDto[];
}

export interface UpdateStageDto {
  id?: string;
  name: string;
  order: number;
  works: UpdateWorkDto[];
  children?: UpdateStageDto[];
}

export interface UpdateWorkScheduleCommand {
  tenantId: string;
  projectId: string;
  workScheduleId: string;
  name: string;
  stages: UpdateStageDto[];
  dependencies?: WorkScheduleWorkDependencyDto[];
}

// Response Web Models
export interface WorkScheduleSummaryWeb {
  id: string;
  costEstimateId?: string | null;
  name: string;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
}

export interface WorkScheduleWorkDependencyWeb {
  id: string;
  predecessorWorkId: string;
  successorWorkId: string;
  dependencyType: WorkDependencyType;
  lagDays: number;
}

export interface WorkScheduleDetailsWeb {
  id: string;
  tenantId: string;
  projectId: string;
  costEstimateId?: string | null;
  name: string;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  stages: WorkScheduleStageWeb[];
  dependencies: WorkScheduleWorkDependencyWeb[];
}

export interface WorkScheduleStageWeb {
  id: string;
  name: string;
  order: number;
  parentStageId?: string | null;
  costEstimateGroupId?: string | null;
  works: WorkScheduleStageWorkWeb[];
  childStages?: WorkScheduleStageWeb[];
}

export interface WorkScheduleStageWorkPeriodWeb {
  id: string;
  startDate: string;
  endDate: string;
  isClosed: boolean;
}

export interface WorkScheduleStageWorkCommentWeb {
  id: string;
  content: string;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
}

export interface WorkScheduleStageWorkWeb {
  id: string;
  costEstimateItemId?: string | null;
  name: string;
  order: number;
  colorRgb: string;
  isClosed: boolean;
  periods: WorkScheduleStageWorkPeriodWeb[];
  assignees: WorkScheduleStageWorkAssigneeWeb[];
  comments: WorkScheduleStageWorkCommentWeb[];
}

export interface WorkScheduleStageWorkAssigneeWeb {
  userId?: string | null;
  userName?: string | null;
  contractorId?: string | null;
  contractorName?: string | null;
  companyName?: string | null;
}

export interface WorkScheduleAssignableMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  companyName?: string | null;
  assignments: WorkScheduleAssigneeBusyPeriodWeb[];
}

export interface WorkScheduleAssignableContractorWeb {
  id: string;
  name: string;
  assignments: WorkScheduleAssigneeBusyPeriodWeb[];
}

export interface WorkScheduleAssignableAssigneesWeb {
  members: WorkScheduleAssignableMemberWeb[];
  contractors: WorkScheduleAssignableContractorWeb[];
}

export interface WorkScheduleAssigneeBusyPeriodWeb {
  workId: string;
  workName: string;
  workScheduleId: string;
  workScheduleName: string;
  projectId: string;
  projectName: string;
  startDate: string;
  endDate: string;
}

/** Konflikt wyliczony lokalnie na podstawie busy periods z assignable-assignees */
export interface WorkScheduleAssignmentConflictWeb {
  userId?: string | null;
  contractorId?: string | null;
  assigneeName: string;
  conflictingWorkId: string;
  conflictingWorkName: string;
  conflictingWorkScheduleId: string;
  conflictingWorkScheduleName: string;
  conflictingProjectId: string;
  conflictingProjectName: string;
  overlapStart: string;
  overlapEnd: string;
}

export function getAssigneeKey(a: WorkScheduleStageWorkAssigneeWeb): string {
  return a.userId ?? a.contractorId ?? "";
}

export function getAssigneeDisplayName(a: WorkScheduleStageWorkAssigneeWeb): string {
  const name = a.userName ?? a.contractorName ?? "Unknown";
  const company = a.companyName?.trim();
  return company ? `${name} (${company})` : name;
}

// Lokalne typy edycyjne — rozszerzają Web modele o opcjonalne ID nowo dodanych elementów przed zapisem
export type EditableComment = Omit<WorkScheduleStageWorkCommentWeb, 'id'> & { id?: string };
export type EditableWork = Omit<WorkScheduleStageWorkWeb, 'comments'> & { comments: EditableComment[] };
export type EditableStage = Omit<WorkScheduleStageWeb, 'works' | 'childStages'> & {
  works: EditableWork[];
  childStages?: EditableStage[];
};

// User Assigned Works Types
export interface UserAssignedWorksByTenantWeb {
  tenantId: string;
  tenantName: string;
  projects: UserAssignedWorksGroupedWeb[];
}

export interface UserAssignedWorksGroupedWeb {
  projectId: string;
  projectName: string;
  workSchedules: UserAssignedWorkScheduleWeb[];
}

export interface UserAssignedWorkScheduleWeb {
  workScheduleId: string;
  workScheduleName: string;
  workScheduleCreatedAt: string;
  stages: UserAssignedStageWeb[];
}

export interface UserAssignedStageWeb {
  stageId: string;
  stageName: string;
  stageOrder: number;
  works: UserAssignedWorkWeb[];
}

export interface UserAssignedWorkWeb {
  workId: string;
  workName: string;
  workOrder: number;
  colorRgb: string;
  isClosed: boolean;
  periods: WorkScheduleStageWorkPeriodWeb[];
  comments: WorkScheduleStageWorkCommentWeb[];
}
