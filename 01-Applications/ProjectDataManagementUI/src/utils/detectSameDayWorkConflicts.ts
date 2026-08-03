import type { UserAssignedWorkWeb } from "../types/workSchedule.types";
import { formatConflictDateRange, formatWorkConflictLine } from "./detectAssigneeConflicts";

export interface SameDayWorkConflict {
  workName: string;
  rangeStartKey: string;
  rangeEndKey: string;
  rangeLabel: string;
  lineLabel: string;
}

function toDateOnly(value: string): Date {
  const date = new Date(value);
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function formatDateKey(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function dateFromKey(dateKey: string): Date {
  const [y, m, d] = dateKey.split("-").map(Number);
  return new Date(y, m - 1, d);
}

function eachDayInclusive(start: Date, end: Date): Date[] {
  const days: Date[] = [];
  const cursor = new Date(start);
  while (cursor <= end) {
    days.push(new Date(cursor));
    cursor.setDate(cursor.getDate() + 1);
  }
  return days;
}

function nextDayKey(dateKey: string): string {
  const date = dateFromKey(dateKey);
  date.setDate(date.getDate() + 1);
  return formatDateKey(date);
}

function mergeConsecutiveDays(dateKeys: string[]): Array<{ rangeStartKey: string; rangeEndKey: string }> {
  if (dateKeys.length === 0) {
    return [];
  }

  const sorted = [...dateKeys].sort((a, b) => a.localeCompare(b));
  const ranges: Array<{ rangeStartKey: string; rangeEndKey: string }> = [];
  let rangeStartKey = sorted[0];
  let rangeEndKey = sorted[0];

  for (let i = 1; i < sorted.length; i += 1) {
    const dateKey = sorted[i];
    if (nextDayKey(rangeEndKey) === dateKey) {
      rangeEndKey = dateKey;
      continue;
    }

    ranges.push({ rangeStartKey, rangeEndKey });
    rangeStartKey = dateKey;
    rangeEndKey = dateKey;
  }

  ranges.push({ rangeStartKey, rangeEndKey });
  return ranges;
}

/**
 * Wykrywa przedziały dat, w których użytkownik ma więcej niż jedną otwartą pracę
 * z nakładającymi się okresami. Każda praca zwracana jest osobno z własnym przedziałem.
 */
export function detectSameDayWorkConflicts(
  works: UserAssignedWorkWeb[]
): SameDayWorkConflict[] {
  const openWorks = works.filter((w) => !w.isClosed);
  const dayToWorks = new Map<string, Set<string>>();

  for (const work of openWorks) {
    const openPeriods = (work.periods ?? []).filter((p) => !p.isClosed);
    for (const period of openPeriods) {
      const start = toDateOnly(period.startDate);
      const end = toDateOnly(period.endDate);
      if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end < start) {
        continue;
      }

      for (const day of eachDayInclusive(start, end)) {
        const key = formatDateKey(day);
        const names = dayToWorks.get(key) ?? new Set<string>();
        names.add(work.workName);
        dayToWorks.set(key, names);
      }
    }
  }

  const conflictDaysByWork = new Map<string, string[]>();
  for (const [dateKey, names] of dayToWorks.entries()) {
    if (names.size < 2) {
      continue;
    }

    for (const workName of names) {
      const days = conflictDaysByWork.get(workName) ?? [];
      days.push(dateKey);
      conflictDaysByWork.set(workName, days);
    }
  }

  const conflicts: SameDayWorkConflict[] = [];
  for (const [workName, dateKeys] of conflictDaysByWork.entries()) {
    for (const range of mergeConsecutiveDays(dateKeys)) {
      const rangeLabel = formatConflictDateRange(range.rangeStartKey, range.rangeEndKey);
      conflicts.push({
        workName,
        rangeStartKey: range.rangeStartKey,
        rangeEndKey: range.rangeEndKey,
        rangeLabel,
        lineLabel: formatWorkConflictLine(workName, range.rangeStartKey, range.rangeEndKey),
      });
    }
  }

  return conflicts.sort((a, b) => {
    const startCmp = a.rangeStartKey.localeCompare(b.rangeStartKey);
    if (startCmp !== 0) {
      return startCmp;
    }
    const endCmp = a.rangeEndKey.localeCompare(b.rangeEndKey);
    if (endCmp !== 0) {
      return endCmp;
    }
    return a.workName.localeCompare(b.workName, "pl");
  });
}
