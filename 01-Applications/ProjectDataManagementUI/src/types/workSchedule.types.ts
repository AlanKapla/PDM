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
  name: string;
  order: number;
  colorRgb: string;
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
  name: string;
  order: number;
  colorRgb: string;
  isClosed: boolean;
  periods: WorkScheduleStageWorkPeriodWeb[];
  assignees: WorkScheduleStageWorkAssigneeWeb[];
  comments: WorkScheduleStageWorkCommentWeb[];
}

export interface WorkScheduleStageWorkAssigneeWeb {
  userId: string;
  userName: string;
}

// User Assigned Works Types
export interface UserAssignedWorksGroupedWeb {
  tenantId: string;
  tenantName: string;
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
}
