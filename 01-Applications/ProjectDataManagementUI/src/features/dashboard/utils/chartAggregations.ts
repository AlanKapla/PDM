import type {
  CostEstimateSummaryWeb,
  ScheduleStageWeb,
  ScheduleSummaryWeb,
  TrackedCostWeb,
} from '../types/projectDashboard.types';
import { TimelineStatus } from '../types/projectDashboard.types';

export interface ChartDataPoint {
  name: string;
  value: number;
  color?: string;
}

export interface TimeSeriesPoint {
  month: string;
  label: string;
  amount: number;
  cumulative: number;
}

export interface ComparisonBarPoint {
  id: string;
  name: string;
  budget: number;
  costs: number;
  isExceeded: boolean;
}

export interface DeviationBarPoint {
  id: string;
  name: string;
  deviation: number;
  deviationPercent: number | null;
}

export interface FlattenedStage {
  stageId: string;
  stageName: string;
  scheduleName: string;
  plannedStart: string | null;
  plannedEnd: string | null;
  timelineStatus: TimelineStatus;
  delayDays: number | null;
  progressPercent: number | null;
}

export interface SCurvePoint {
  date: string;
  label: string;
  planned: number;
  actual: number;
}

const SOURCE_TYPE_LABELS: Record<string, string> = {
  ProjectAdditional: 'Koszty główne',
  ScheduleWorkItem: 'Harmonogram',
  EstimateItem: 'Kosztorys',
  LinkedWorkItem: 'Powiązane',
};

export function topN<T>(items: T[], limit: number): T[] {
  return items.slice(0, limit);
}

export function pickCostAmount(cost: TrackedCostWeb): number {
  return cost.net ?? 0;
}

export function getCostDate(cost: TrackedCostWeb): string | null {
  return cost.date ?? cost.createdAt ?? null;
}

export function aggregateCostsByMonth(costs: TrackedCostWeb[]): TimeSeriesPoint[] {
  const buckets = new Map<string, number>();

  for (const cost of costs) {
    const dateStr = getCostDate(cost);
    if (!dateStr) {
      continue;
    }
    const date = new Date(dateStr);
    if (Number.isNaN(date.getTime())) {
      continue;
    }
    const key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
    const current = buckets.get(key) ?? 0;
    buckets.set(key, current + pickCostAmount(cost));
  }

  const sorted = Array.from(buckets.entries()).sort(([a], [b]) => a.localeCompare(b));
  let cumulative = 0;

  return sorted.map(([month, amount]) => {
    cumulative += amount;
    const [year, monthNum] = month.split('-');
    return {
      month,
      label: `${monthNum}.${year}`,
      amount,
      cumulative,
    };
  });
}

export function groupCostsBySourceType(costs: TrackedCostWeb[]): ChartDataPoint[] {
  const buckets = new Map<string, number>();

  for (const cost of costs) {
    const key = cost.sourceType ?? 'ProjectAdditional';
    const label = SOURCE_TYPE_LABELS[key] ?? key;
    const current = buckets.get(label) ?? 0;
    buckets.set(label, current + pickCostAmount(cost));
  }

  return Array.from(buckets.entries())
    .map(([name, value]) => ({ name, value }))
    .filter((item) => item.value > 0)
    .sort((a, b) => b.value - a.value);
}

export function groupCostsByContractor(
  costs: TrackedCostWeb[],
  limit: number
): ChartDataPoint[] {
  const buckets = new Map<string, number>();

  for (const cost of costs) {
    const name = cost.contractorName?.trim() || 'Brak wykonawcy';
    const current = buckets.get(name) ?? 0;
    buckets.set(name, current + pickCostAmount(cost));
  }

  return topN(
    Array.from(buckets.entries())
      .map(([name, value]) => ({ name, value }))
      .filter((item) => item.value > 0)
      .sort((a, b) => b.value - a.value),
    limit
  );
}

export function buildEstimateComparison(
  summaries: CostEstimateSummaryWeb[]
): ComparisonBarPoint[] {
  return summaries.map((summary) => ({
    id: summary.costEstimateId,
    name: summary.costEstimateName,
    budget: summary.budgetNet ?? 0,
    costs: summary.costsNet ?? 0,
    isExceeded: summary.isBudgetExceeded,
  }));
}

export function buildEstimateDeviations(
  summaries: CostEstimateSummaryWeb[]
): DeviationBarPoint[] {
  return summaries
    .map((summary) => ({
      id: summary.costEstimateId,
      name: summary.costEstimateName,
      deviation: summary.deviationNet ?? 0,
      deviationPercent: summary.deviationPercent,
    }))
    .sort((a, b) => a.deviation - b.deviation);
}

export function buildScheduleCosts(summaries: ScheduleSummaryWeb[]): ChartDataPoint[] {
  return summaries
    .map((summary) => ({
      name: summary.workScheduleName,
      value: summary.totalCostsNet ?? 0,
    }))
    .filter((item) => item.value > 0)
    .sort((a, b) => b.value - a.value);
}

export function buildScheduleProgress(
  summaries: ScheduleSummaryWeb[]
): ChartDataPoint[] {
  return summaries.map((summary) => ({
    name: summary.workScheduleName,
    value: summary.timeline?.progressPercent ?? 0,
  }));
}

function flattenStagesRecursive(
  stages: ScheduleStageWeb[],
  scheduleName: string,
  result: FlattenedStage[]
): void {
  for (const stage of stages) {
    result.push({
      stageId: stage.stageId,
      stageName: stage.stageName,
      scheduleName,
      plannedStart: stage.timeline?.plannedStart ?? null,
      plannedEnd: stage.timeline?.plannedEnd ?? null,
      timelineStatus: stage.timelineStatus,
      delayDays: stage.timeline?.delayDays ?? null,
      progressPercent: stage.timeline?.progressPercent ?? null,
    });
    if (stage.childStages?.length) {
      flattenStagesRecursive(stage.childStages, scheduleName, result);
    }
  }
}

export function flattenAllStages(summaries: ScheduleSummaryWeb[]): FlattenedStage[] {
  const result: FlattenedStage[] = [];
  for (const summary of summaries) {
    flattenStagesRecursive(summary.stages ?? [], summary.workScheduleName, result);
  }
  return result;
}

export function buildDelayedStages(stages: FlattenedStage[]): FlattenedStage[] {
  return stages.filter(
    (stage) =>
      stage.timelineStatus === TimelineStatus.Delayed ||
      (stage.delayDays != null && stage.delayDays > 0)
  );
}

export function buildSCurveData(stages: FlattenedStage[]): SCurvePoint[] {
  const dated = stages
    .filter((stage) => stage.plannedEnd != null)
    .sort((a, b) => {
      const aTime = new Date(a.plannedEnd ?? '').getTime();
      const bTime = new Date(b.plannedEnd ?? '').getTime();
      return aTime - bTime;
    });

  if (dated.length === 0) {
    return [];
  }

  const total = dated.length;
  let completed = 0;

  return dated.map((stage, index) => {
    const isDone =
      stage.timelineStatus === TimelineStatus.Completed ||
      stage.timelineStatus === TimelineStatus.CompletedLate;
    if (isDone) {
      completed += 1;
    }
    const planned = ((index + 1) / total) * 100;
    const actual = (completed / total) * 100;
    const date = stage.plannedEnd ?? '';
    const parsed = new Date(date);
    const label = Number.isNaN(parsed.getTime())
      ? `Etap ${index + 1}`
      : `${String(parsed.getDate()).padStart(2, '0')}.${String(parsed.getMonth() + 1).padStart(2, '0')}`;

    return {
      date,
      label,
      planned: Math.round(planned),
      actual: Math.round(actual),
    };
  });
}

export function sourceTypeLabel(sourceType: string): string {
  return SOURCE_TYPE_LABELS[sourceType] ?? sourceType;
}
