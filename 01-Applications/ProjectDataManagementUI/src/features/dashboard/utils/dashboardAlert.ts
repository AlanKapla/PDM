import {
  TimelineStatus,
  type ProjectDashboardWeb,
  type CostEstimateSummaryWeb,
  type ScheduleSummaryWeb,
  type ScheduleStageWeb,
  type TrackerGroupWeb,
  type WorkItemLinkWeb,
} from '../types/projectDashboard.types';

export interface DashboardAlert {
  status: 'warning' | 'error';
  message: string;
}

function isActiveTimelineStatus(status: TimelineStatus): boolean {
  return (
    status === TimelineStatus.Completed ||
    status === TimelineStatus.CompletedLate ||
    status === TimelineStatus.InProgress
  );
}

function collectEstimateItems(groups: TrackerGroupWeb[]): WorkItemLinkWeb[] {
  const items: WorkItemLinkWeb[] = [];
  groups.forEach((group) => {
    items.push(...group.items);
    if (group.childGroups.length > 0) {
      items.push(...collectEstimateItems(group.childGroups));
    }
  });
  return items;
}

function collectScheduleWorkItems(stages: ScheduleStageWeb[]): WorkItemLinkWeb[] {
  const items: WorkItemLinkWeb[] = [];
  stages.forEach((stage) => {
    items.push(...stage.workItems);
    if (stage.childStages.length > 0) {
      items.push(...collectScheduleWorkItems(stage.childStages));
    }
  });
  return items;
}

function collectAllWorkItems(data: ProjectDashboardWeb): WorkItemLinkWeb[] {
  const estimateItems = data.costEstimateSummaries.flatMap((summary) =>
    collectEstimateItems(summary.groups)
  );
  const scheduleItems = data.scheduleSummaries.flatMap((summary) =>
    collectScheduleWorkItems(summary.stages)
  );
  return [...estimateItems, ...scheduleItems];
}

function findMissingCostWork(items: WorkItemLinkWeb[]): WorkItemLinkWeb | null {
  return (
    items.find(
      (item) => isActiveTimelineStatus(item.timelineStatus) && item.costCount === 0
    ) ?? null
  );
}

function findOverBudgetEstimate(
  summaries: CostEstimateSummaryWeb[]
): CostEstimateSummaryWeb | null {
  return summaries.find((summary) => summary.isBudgetExceeded) ?? null;
}

function findDelayedSchedule(
  summaries: ScheduleSummaryWeb[]
): ScheduleSummaryWeb | null {
  return (
    summaries.find(
      (summary) =>
        summary.timeline?.isDelayed === true ||
        summary.timelineStatus === TimelineStatus.Delayed
    ) ?? null
  );
}

/**
 * Wyznacza pojedynczy alert dashboardu na podstawie realnych odchyleń.
 * Zwraca null gdy nic nie wymaga uwagi (brak alertu = wszystko w porządku).
 * Priorytet: brak kosztu na pracy w toku/zakończonej → przekroczony budżet → opóźnienie.
 */
export function computeDashboardAlert(data: ProjectDashboardWeb): DashboardAlert | null {
  const missingCostWork = findMissingCostWork(collectAllWorkItems(data));
  if (missingCostWork) {
    const isCompleted =
      missingCostWork.timelineStatus === TimelineStatus.Completed ||
      missingCostWork.timelineStatus === TimelineStatus.CompletedLate;
    const prefix = isCompleted ? 'Zakończona praca' : 'Trwająca praca';
    return {
      status: 'warning',
      message: `${prefix} „${missingCostWork.displayName}” nie ma przypisanego kosztu.`,
    };
  }

  const overBudget = findOverBudgetEstimate(data.costEstimateSummaries);
  if (overBudget) {
    return {
      status: 'error',
      message: `Kosztorys „${overBudget.costEstimateName}” przekracza budżet.`,
    };
  }

  const delayed = findDelayedSchedule(data.scheduleSummaries);
  if (delayed) {
    return {
      status: 'error',
      message: `Harmonogram „${delayed.workScheduleName}” jest opóźniony.`,
    };
  }

  return null;
}
