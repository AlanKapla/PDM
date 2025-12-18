// Request DTOs
export interface CreateWorkPeriodDto {
  startDate: string;
  endDate: string;
}

export interface CreateWorkDto {
  name: string;
  order: number;
  colorRgb: string;
  periods: CreateWorkPeriodDto[];
  assignedUserIds: string[];
}

export interface CreateStageDto {
  name: string;
  order: number;
  works: CreateWorkDto[];
}

export interface CreateWorkScheduleCommand {
  tenantId: string;
  projectId: string;
  name: string;
  stages: CreateStageDto[];
}

export interface UpdateWorkPeriodDto {
  id?: string;
  startDate: string;
  endDate: string;
}

export interface UpdateWorkDto {
  id?: string;
  name: string;
  order: number;
  colorRgb: string;
  isClosed: boolean;
  periods: UpdateWorkPeriodDto[];
  assignedUserIds: string[];
}

export interface UpdateStageDto {
  id?: string;
  name: string;
  order: number;
  works: UpdateWorkDto[];
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
  name: string;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
}

export interface WorkScheduleDetailsWeb {
  id: string;
  tenantId: string;
  projectId: string;
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
  works: WorkScheduleStageWorkWeb[];
}

export interface WorkScheduleStageWorkPeriodWeb {
  startDate: string;
  endDate: string;
}

export interface WorkScheduleStageWorkWeb {
  id: string;
  name: string;
  order: number;
  colorRgb: string;
  isClosed: boolean;
  periods: WorkScheduleStageWorkPeriodWeb[];
  assignees: WorkScheduleStageWorkAssigneeWeb[];
}

export interface WorkScheduleStageWorkAssigneeWeb {
  userId: string;
  userName: string;
}

// User Assigned Works Types
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
}
