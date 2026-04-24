// Enumy — wartości odpowiadają dokładnie C# enum values
export enum FinancialStatus {
  NoBudget   = 0,
  NoCosts    = 1,
  InProgress = 2,
  NearLimit  = 3,
  OverBudget = 4,
}

export enum TimelineStatus {
  NoSchedule    = 0,
  NotStarted    = 1,
  InProgress    = 2,
  Delayed       = 3,
  Completed     = 4,
  CompletedLate = 5,
  NoWorkItems   = 6,
}

export interface TimelineStatsWeb {
  plannedStart: string | null;
  plannedEnd: string | null;
  totalPlannedDays: number | null;
  totalWorkCount: number;
  completedCount: number;
  completedLateCount: number;
  inProgressCount: number;
  notStartedCount: number;
  delayedCount: number;
  progressPercent: number | null;
  delayDays: number | null;
  overallStatus: TimelineStatus;
  isDelayed: boolean;
  isCompleted: boolean;
}

export interface StatusedNodeWeb {
  financialStatus: FinancialStatus;
  timelineStatus: TimelineStatus;
}

export interface TrackerNodeWeb extends StatusedNodeWeb {
  budgetNet: number | null;
  budgetGross: number | null;
  costsNet: number | null;
  costsGross: number | null;
  deviationNet: number | null;
  deviationGross: number | null;
  deviationPercent: number | null;
  coveredPercent: number | null;
  isBudgetExceeded: boolean;
  costCount: number;
}

export interface TrackerNodeWithTimelineWeb extends TrackerNodeWeb {
  timeline: TimelineStatsWeb | null;
  hasLinkedSchedule: boolean;
}

export interface TrackedCostAttachmentWeb {
  id: string;
  originalFileName: string;
  fileUrl: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
}

export interface TrackedCostWeb {
  id: string;
  workItemLinkId: string | null;
  costEstimateItemId: string | null;
  workScheduleStageWorkId: string | null;
  isAdditional: boolean;
  name: string;
  description: string | null;
  net: number | null;
  gross: number | null;
  vatRate: number | null;
  contractor: string | null;
  date: string | null;
  number: string | null;
  attachments: TrackedCostAttachmentWeb[];
  createdAt: string;
  updatedAt: string | null;
  sourceType: 'ProjectAdditional' | 'ScheduleWorkItem' | 'EstimateItem' | 'LinkedWorkItem';
  // ScheduleWorkItem / LinkedWorkItem
  scheduleName: string | null;
  stageName: string | null;
  workItemName: string | null;
  // EstimateItem / LinkedWorkItem
  estimateName: string | null;
  estimateGroupName: string | null;
  estimateItemName: string | null;
}

export interface TrackerAdditionalCostsWeb {
  totalNet: number | null;
  totalGross: number | null;
  costsCount: number;
  costs: TrackedCostWeb[];
}

export interface ProjectAdditionalCostsWeb {
  totalNet: number | null;
  totalGross: number | null;
  costsCount: number;
  costs: TrackedCostWeb[];
}

export enum WorkItemType {
  Link = 0,
  Estimate = 1,
  Schedule = 2,
}

export interface WorkItemLinkWeb extends TrackerNodeWithTimelineWeb {
  workItemLinkId: string | null;
  displayName: string;
  order: number;
  workItemType: WorkItemType;
  costEstimateItemId: string | null;
  workScheduleStageWorkId: string | null;
  costs: TrackedCostWeb[];
  // Direct timeline fields (estimate context — hasLinkedSchedule)
  timelinePlannedStart: string | null;
  timelinePlannedEnd: string | null;
  timelineTotalDays: number | null;
}

export interface TrackerGroupWeb extends TrackerNodeWithTimelineWeb {
  groupId: string;
  groupName: string;
  order: number;
  totalItemsCount: number;
  itemsWithCostsCount: number;
  itemsWithoutCostsCount: number;
  itemsOverBudgetCount: number;
  itemsNearLimitCount: number;
  timelinePlannedStart: string | null;
  timelinePlannedEnd: string | null;
  timelineTotalDays: number | null;
  items: WorkItemLinkWeb[];
  childGroups: TrackerGroupWeb[];
  additionalCosts: TrackerAdditionalCostsWeb;
}

export interface ScheduleStageWeb extends TrackerNodeWithTimelineWeb {
  stageId: string;
  stageName: string;
  order: number;
  totalWorkItemsCount: number;
  completedWorkItemsCount: number;
  delayedWorkItemsCount: number;
  totalCostsNet: number | null;
  totalCostsGross: number | null;
  workItems: WorkItemLinkWeb[];
  childStages: ScheduleStageWeb[];
}

export interface CostEstimateSummaryWeb extends TrackerNodeWithTimelineWeb {
  costEstimateId: string;
  costEstimateName: string;
  totalItemsCount: number;
  itemsWithCostsCount: number;
  itemsWithoutCostsCount: number;
  itemsOverBudgetCount: number;
  itemsNearLimitCount: number;
  linkedWorkScheduleId: string | null;
  timelinePlannedStart: string | null;
  timelinePlannedEnd: string | null;
  timelineTotalDays: number | null;
  groups: TrackerGroupWeb[];
  additionalCosts: TrackerAdditionalCostsWeb;
}

export interface ScheduleCostSummaryWeb {
  totalSchedulesCostsNet: number;
  totalSchedulesCostsGross: number;
  schedulesWithCostsCount: number;
  schedulesWithoutCostsCount: number;
}

export interface ScheduleSummaryWeb extends TrackerNodeWithTimelineWeb {
  workScheduleId: string;
  workScheduleName: string;
  hasLinkedEstimate: boolean;
  linkedCostEstimateId: string | null;
  totalWorkItemsCount: number;
  workItemsWithCostsCount: number;
  workItemsOverBudgetCount: number;
  workItemsNearLimitCount: number;
  workItemsDelayedCount: number;
  totalCostsNet: number | null;
  totalCostsGross: number | null;
  stages: ScheduleStageWeb[];
}

export interface ProjectFinancialSummaryWeb {
  totalBudgetNet: number | null;
  totalBudgetGross: number | null;
  estimateBudgetNet: number | null;
  estimateBudgetGross: number | null;
  projectReserveBudgetNet: number | null;
  projectReserveBudgetGross: number | null;
  totalCostsNet: number | null;
  totalCostsGross: number | null;
  linkedCostsNet: number | null;
  linkedCostsGross: number | null;
  additionalCostsNet: number | null;
  additionalCostsGross: number | null;
  deviationNet: number | null;
  deviationGross: number | null;
  deviationPercent: number | null;
  coveredPercent: number | null;
  isBudgetExceeded: boolean;
  financialStatus: FinancialStatus;
  totalCostCount: number;
  linkedCostCount: number;
  additionalCostCount: number;
  costEstimatesCount: number;
  costEstimatesWithCostsCount: number;
  costEstimatesOverBudgetCount: number;
  workSchedulesCount: number;
  scheduleCostSummary: ScheduleCostSummaryWeb;
}

export interface ProjectTimelineSummaryWeb {
  earliestStart: string | null;
  latestEnd: string | null;
  totalPlannedDays: number | null;
  totalWorkCount: number;
  completedCount: number;
  completedLateCount: number;
  inProgressCount: number;
  notStartedCount: number;
  delayedCount: number;
  progressPercent: number | null;
  delayDays: number | null;
  overallStatus: TimelineStatus;
  isDelayed: boolean;
  isCompleted: boolean;
  workSchedulesCount: number;
  activeSchedulesCount: number;
  completedSchedulesCount: number;
}

export interface ProjectDashboardWeb {
  projectId: string;
  referenceDate: string;
  generatedAt: string;
  financialSummary: ProjectFinancialSummaryWeb;
  timelineSummary: ProjectTimelineSummaryWeb;
  costEstimateSummaries: CostEstimateSummaryWeb[];
  scheduleSummaries: ScheduleSummaryWeb[];
  projectAdditionalCosts: ProjectAdditionalCostsWeb;
  allCosts: TrackedCostWeb[];
}

// --- Request types ---

export interface CreateTrackedCostRequest {
  workItemLinkId?: string | null;
  costEstimateItemId?: string | null;
  workScheduleStageWorkId?: string | null;
  name: string;
  description?: string | null;
  net?: number | null;
  gross?: number | null;
  number?: string | null;
  contractor?: string | null;
  date?: string | null;
  newFiles?: File[];
}

export interface UpdateTrackedCostRequest {
  name: string;
  description?: string | null;
  net?: number | null;
  gross?: number | null;
  number?: string | null;
  contractor?: string | null;
  date?: string | null;
  newFiles?: File[];
  existingAttachmentIds?: string[];
}

export interface UpdateTrackerBudgetRequest {
  budgetNet?: number | null;
  budgetGross?: number | null;
}
