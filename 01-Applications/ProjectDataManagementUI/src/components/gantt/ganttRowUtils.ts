import type { WorkScheduleStageWeb, WorkScheduleStageWorkWeb, WorkScheduleStageWorkPeriodWeb } from "../../types/workSchedule.types";
import type { GanttMode } from "./GanttContext";
import { G } from "./ganttTokens";

// ─── Flat-row model ───────────────────────────────────────────────────────────

export type FlatRowKind = "stageHeader" | "stageDetail" | "work" | "addWork";

export interface FlatRow {
  id: string;
  kind: FlatRowKind;
  stage: WorkScheduleStageWeb;
  work?: WorkScheduleStageWorkWeb;
  depth: number;
  height: number;
}

/** Buduje płaską listę wierszy dla lewego panelu i prawej siatki */
export function buildFlatRows(
  stages: WorkScheduleStageWeb[],
  expandedStages: Set<string>,
  mode: GanttMode,
  collapsedWorks: Set<string> = new Set(),
  depth = 0,
): FlatRow[] {
  const rows: FlatRow[] = [];
  const sorted = [...stages].sort((a, b) => a.order - b.order);

  for (const stage of sorted) {
    rows.push({ id: `sh-${stage.id}`, kind: "stageHeader", stage, depth, height: G.STAGE_ROW_H });
    rows.push({ id: `sd-${stage.id}`, kind: "stageDetail", stage, depth, height: G.STAGE_DETAIL_H });

    if (expandedStages.has(stage.id)) {
      const sortedWorks = [...(stage.works ?? [])].sort((a, b) => a.order - b.order);
      for (const work of sortedWorks) {
        const periodsCount = work.periods?.length ?? 0;
        const visiblePeriods = collapsedWorks.has(work.id) ? 0 : periodsCount;
        const workHeight = G.ROW_H + visiblePeriods * G.PERIOD_ROW_H;
        rows.push({ id: `w-${work.id}`, kind: "work", stage, work, depth, height: workHeight });
      }
      if (mode === "edit") {
        rows.push({ id: `aw-${stage.id}`, kind: "addWork", stage, depth, height: G.ADD_WORK_H });
      }
      for (const child of (stage.childStages ?? []).sort((a, b) => a.order - b.order)) {
        rows.push(...buildFlatRows([child], expandedStages, mode, collapsedWorks, depth + 1));
      }
    }
  }
  return rows;
}

// ─── Date utilities ───────────────────────────────────────────────────────────

export function toLocalDateStr(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

/** Buduje mapę dateStr → kolIndeks */
export function makeDateColMap(dates: Date[]): Record<string, number> {
  const map: Record<string, number> = {};
  dates.forEach((d, i) => { map[toLocalDateStr(d)] = i; });
  return map;
}

export function isWeekendDate(d: Date): boolean {
  const day = d.getDay();
  return day === 0 || day === 6;
}

export function isTodayDate(d: Date): boolean {
  const now = new Date();
  return (
    d.getDate() === now.getDate() &&
    d.getMonth() === now.getMonth() &&
    d.getFullYear() === now.getFullYear()
  );
}

// ─── Business logic helpers ───────────────────────────────────────────────────

/** Postęp etapu: { done, total } */
export function stageProgress(stage: WorkScheduleStageWeb) {
  const works = stage.works ?? [];
  return { done: works.filter(w => w.isClosed).length, total: works.length };
}

/** Stan checkboxa zakresu pracy */
export function workCheckState(
  work: WorkScheduleStageWorkWeb,
): "checked" | "indeterminate" | "unchecked" {
  const periods = work.periods ?? [];
  if (periods.length === 0) return work.isClosed ? "checked" : "unchecked";
  const closedCount = periods.filter(p => p.isClosed).length;
  if (closedCount === periods.length) return "checked";
  if (closedCount === 0) return "unchecked";
  return "indeterminate";
}

/** Czy zakres pracy jest w pełni zamknięty */
export function isWorkFullyClosed(work: WorkScheduleStageWorkWeb): boolean {
  return workCheckState(work) === "checked";
}

/** Formatuje datę do wyświetlenia w widoku */
export function fmtShortDate(s: string): string {
  const d = new Date(s.slice(0, 10) + "T00:00:00");
  return d.toLocaleDateString("pl-PL", { day: "numeric", month: "short" });
}

/** Formatuje datę do kompaktowego wyświetlenia: D.MM (np. 25.02) */
export function fmtCompactDate(s: string): string {
  const d = new Date(s.slice(0, 10) + "T00:00:00");
  const day = d.getDate();
  const month = String(d.getMonth() + 1).padStart(2, "0");
  return `${day}.${month}`;
}

/** Oblicza zakres dat etapu (min start, max end) ze wszystkich prac rekurencyjnie */
export function getStageRange(
  stage: WorkScheduleStageWeb,
): { start: string; end: string } | null {
  const getAllWorks = (s: WorkScheduleStageWeb): WorkScheduleStageWorkWeb[] => {
    const works = s.works ?? [];
    const childWorks = (s.childStages ?? []).flatMap(getAllWorks);
    return [...works, ...childWorks];
  };

  const allWorks = getAllWorks(stage);
  const starts: string[] = [];
  const ends: string[] = [];

  for (const w of allWorks) {
    const periods = w.periods ?? [];
    if (periods.length > 0) {
      starts.push(periods[0].startDate.slice(0, 10));
      ends.push(periods[periods.length - 1].endDate.slice(0, 10));
    }
  }

  if (!starts.length) return null;
  const minStart = starts.reduce((a, b) => (a < b ? a : b));
  const maxEnd = ends.reduce((a, b) => (a > b ? a : b));
  return { start: minStart, end: maxEnd };
}

/**
 * Grupuje okresy pracy w ciągłe bloki wizualne.
 * Okresy bez przerwy (lub z przerwą ≤ 1 dzień) są scalane w jedną grupę,
 * żeby wyświetlić je jako jeden pasek na osi czasu.
 */
export function groupConsecutivePeriods(
  periods: WorkScheduleStageWorkPeriodWeb[],
): WorkScheduleStageWorkPeriodWeb[][] {
  if (periods.length === 0) return [];

  const sorted = [...periods].sort((a, b) =>
    a.startDate.slice(0, 10).localeCompare(b.startDate.slice(0, 10)),
  );

  const groups: WorkScheduleStageWorkPeriodWeb[][] = [[sorted[0]]];

  for (let i = 1; i < sorted.length; i++) {
    const current = sorted[i];
    const lastGroup = groups[groups.length - 1];
    const lastPeriod = lastGroup[lastGroup.length - 1];

    const lastEnd = new Date(lastPeriod.endDate.slice(0, 10) + "T00:00:00");
    const currentStart = new Date(current.startDate.slice(0, 10) + "T00:00:00");
    const diffDays = Math.round(
      (currentStart.getTime() - lastEnd.getTime()) / (1000 * 60 * 60 * 24),
    );

    if (diffDays <= 1) {
      lastGroup.push(current);
    } else {
      groups.push([current]);
    }
  }

  return groups;
}
