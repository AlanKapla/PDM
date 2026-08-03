import type {
  WorkScheduleAssigneeBusyPeriodWeb,
  WorkScheduleAssignmentConflictWeb,
  WorkScheduleStageWorkPeriodWeb,
} from "../types/workSchedule.types";

export interface DetectAssigneeConflictsInput {
  workId: string;
  workPeriods: Array<Pick<WorkScheduleStageWorkPeriodWeb, "startDate" | "endDate" | "isClosed">>;
  candidates: Array<{
    userId?: string | null;
    contractorId?: string | null;
    assigneeName: string;
    assignments: WorkScheduleAssigneeBusyPeriodWeb[];
  }>;
}

export function getOpenWorkPeriodsForConflictCheck(
  periods: Array<Pick<WorkScheduleStageWorkPeriodWeb, "startDate" | "endDate" | "isClosed">>
): Array<Pick<WorkScheduleStageWorkPeriodWeb, "startDate" | "endDate">> {
  return periods
    .filter((period) => !period.isClosed)
    .map((period) => ({
      startDate: period.startDate,
      endDate: period.endDate,
    }));
}

function toTime(value: string): number {
  return new Date(value).getTime();
}

function overlaps(aStart: string, aEnd: string, bStart: string, bEnd: string): boolean {
  return toTime(aStart) <= toTime(bEnd) && toTime(bStart) <= toTime(aEnd);
}

function maxDate(a: string, b: string): string {
  return toTime(a) > toTime(b) ? a : b;
}

function minDate(a: string, b: string): string {
  return toTime(a) < toTime(b) ? a : b;
}

/**
 * Wykrywa konflikty terminów lokalnie na podstawie busy periods z endpointu assignable-assignees.
 * Ignoruje przypisania do bieżącej pracy (`workId`).
 */
export function detectAssigneeConflicts(
  input: DetectAssigneeConflictsInput
): WorkScheduleAssignmentConflictWeb[] {
  const conflicts: WorkScheduleAssignmentConflictWeb[] = [];
  const openWorkPeriods = getOpenWorkPeriodsForConflictCheck(input.workPeriods);

  if (openWorkPeriods.length === 0) {
    return conflicts;
  }

  for (const candidate of input.candidates) {
    for (const assignment of candidate.assignments) {
      if (assignment.workId === input.workId) {
        continue;
      }

      for (const period of openWorkPeriods) {
        if (!overlaps(period.startDate, period.endDate, assignment.startDate, assignment.endDate)) {
          continue;
        }

        conflicts.push({
          userId: candidate.userId ?? null,
          contractorId: candidate.contractorId ?? null,
          assigneeName: candidate.assigneeName,
          conflictingWorkId: assignment.workId,
          conflictingWorkName: assignment.workName,
          conflictingWorkScheduleId: assignment.workScheduleId,
          conflictingWorkScheduleName: assignment.workScheduleName,
          conflictingProjectId: assignment.projectId,
          conflictingProjectName: assignment.projectName,
          overlapStart: maxDate(period.startDate, assignment.startDate),
          overlapEnd: minDate(period.endDate, assignment.endDate),
        });
      }
    }
  }

  const seen = new Set<string>();
  return conflicts.filter((c) => {
    const key = [
      c.userId ?? "",
      c.contractorId ?? "",
      c.conflictingWorkId,
      c.overlapStart,
      c.overlapEnd,
    ].join("|");
    if (seen.has(key)) {
      return false;
    }
    seen.add(key);
    return true;
  });
}

/** Wspólny format daty konfliktu (jak na zaplanowanych pracach). */
export function formatConflictDateLabel(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return date.toLocaleDateString("pl-PL", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

/** Przedział: jedna data albo „start – end”. */
export function formatConflictDateRange(start: string, end: string): string {
  const startKey = start.slice(0, 10);
  const endKey = end.slice(0, 10);
  const startLabel = formatConflictDateLabel(start);
  if (startKey === endKey) {
    return startLabel;
  }
  return `${startLabel} – ${formatConflictDateLabel(end)}`;
}

/**
 * Wspólny wiersz konfliktu: „Nazwa pracy: daty”.
 * Używany w modalu przypisań i na liście zaplanowanych prac.
 */
export function formatWorkConflictLine(workName: string, start: string, end: string): string {
  return `${workName}: ${formatConflictDateRange(start, end)}`;
}

export function formatAssigneeConflictLine(conflict: WorkScheduleAssignmentConflictWeb): string {
  return `${conflict.assigneeName} — ${formatWorkConflictLine(
    conflict.conflictingWorkName,
    conflict.overlapStart,
    conflict.overlapEnd
  )}`;
}

export function formatAssigneeConflictTooltip(
  conflicts: WorkScheduleAssignmentConflictWeb[]
): string {
  if (conflicts.length === 0) {
    return "";
  }

  const lines = conflicts.map((c) =>
    formatWorkConflictLine(c.conflictingWorkName, c.overlapStart, c.overlapEnd)
  );

  return `Już przypisany/a w nakładającym się terminie:\n${lines.join("\n")}`;
}

export function diffNewAssignees(
  selectedUserIds: string[],
  selectedContractorIds: string[],
  currentUserIds: string[],
  currentContractorIds: string[]
): { newUserIds: string[]; newContractorIds: string[] } {
  const currentUsers = new Set(currentUserIds);
  const currentContractors = new Set(currentContractorIds);
  return {
    newUserIds: selectedUserIds.filter((id) => !currentUsers.has(id)),
    newContractorIds: selectedContractorIds.filter((id) => !currentContractors.has(id)),
  };
}
