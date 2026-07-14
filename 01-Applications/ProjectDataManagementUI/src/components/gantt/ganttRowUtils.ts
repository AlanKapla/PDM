import type { WorkScheduleStageWeb, WorkScheduleStageWorkWeb, WorkScheduleStageWorkPeriodWeb } from "../../types/workSchedule.types";
import { formatDateShortLocal } from "../../utils/dateTimeUtils";
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

/** Czy etap lub jego potomkowie pasują do zapytania (nazwa etapu lub pracy). */
export function stageMatchesSearch(stage: WorkScheduleStageWeb, query: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) {
    return true;
  }
  if ((stage.name ?? '').toLowerCase().includes(q)) {
    return true;
  }
  const workMatch = (stage.works ?? []).some((w) => (w.name ?? '').toLowerCase().includes(q));
  if (workMatch) {
    return true;
  }
  return (stage.childStages ?? []).some((child) => stageMatchesSearch(child, query));
}

/** Filtruje drzewo etapów po nazwie etapu lub pracy (case-insensitive). */
export function filterStagesBySearch(
  stages: WorkScheduleStageWeb[],
  query: string,
): WorkScheduleStageWeb[] {
  const q = query.trim().toLowerCase();
  if (!q) {
    return stages;
  }

  const filterRecursive = (stageList: WorkScheduleStageWeb[]): WorkScheduleStageWeb[] => {
    const result: WorkScheduleStageWeb[] = [];
    for (const stage of stageList) {
      const stageNameMatch = (stage.name ?? '').toLowerCase().includes(q);
      const filteredChildren = filterRecursive(stage.childStages ?? []);
      const matchingWorks = stageNameMatch
        ? (stage.works ?? [])
        : (stage.works ?? []).filter((w) => (w.name ?? '').toLowerCase().includes(q));
      const hasMatchingChildren = filteredChildren.length > 0;
      const hasMatchingWorks = matchingWorks.length > 0;

      if (stageNameMatch || hasMatchingWorks || hasMatchingChildren) {
        result.push({
          ...stage,
          works: matchingWorks,
          childStages: stageNameMatch ? (stage.childStages ?? []) : filteredChildren,
        });
      }
    }
    return result;
  };

  return filterRecursive(stages);
}

/** Zwraca ID etapów do auto-rozwinięcia przy aktywnym wyszukiwaniu. */
export function collectExpandableStageIdsForSearch(
  stages: WorkScheduleStageWeb[],
  query: string,
): string[] {
  const q = query.trim().toLowerCase();
  if (!q) {
    return [];
  }

  const ids: string[] = [];

  const walk = (stageList: WorkScheduleStageWeb[]): boolean => {
    let subtreeHasMatch = false;
    for (const stage of stageList) {
      const nameMatch = (stage.name ?? '').toLowerCase().includes(q);
      const workMatch = (stage.works ?? []).some((w) => (w.name ?? '').toLowerCase().includes(q));
      const childMatch = walk(stage.childStages ?? []);
      if (nameMatch || workMatch || childMatch) {
        ids.push(stage.id);
        subtreeHasMatch = true;
      }
    }
    return subtreeHasMatch;
  };

  walk(stages);
  return ids;
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

/** Formatuje datę do kompaktowego wyświetlenia: DD.MM.YYYY (np. 25.02.2026) */
export function fmtCompactDate(s: string): string {
  return formatDateShortLocal(s.slice(0, 10));
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
