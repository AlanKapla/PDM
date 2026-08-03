import { useCallback, useState } from "react";
import type {
  WorkScheduleAssigneeBusyPeriodWeb,
  WorkScheduleAssignmentConflictWeb,
  WorkScheduleStageWorkPeriodWeb,
} from "../types/workSchedule.types";
import { detectAssigneeConflicts, diffNewAssignees } from "../utils/detectAssigneeConflicts";

export { diffNewAssignees };

export interface AssigneeConflictCandidate {
  userId?: string | null;
  contractorId?: string | null;
  assigneeName: string;
  assignments: WorkScheduleAssigneeBusyPeriodWeb[];
}

export interface UseAssignmentConflictCheckResult {
  conflicts: WorkScheduleAssignmentConflictWeb[];
  isChecking: boolean;
  checkConflicts: (
    candidates: AssigneeConflictCandidate[],
    workId: string,
    workPeriods: Array<Pick<WorkScheduleStageWorkPeriodWeb, "startDate" | "endDate" | "isClosed">>
  ) => WorkScheduleAssignmentConflictWeb[];
  clearConflicts: () => void;
}

/** Lokalna weryfikacja konfliktów na podstawie assignments z assignable-assignees. */
export function useAssignmentConflictCheck(): UseAssignmentConflictCheckResult {
  const [conflicts, setConflicts] = useState<WorkScheduleAssignmentConflictWeb[]>([]);

  const checkConflicts = useCallback(
    (
      candidates: AssigneeConflictCandidate[],
      workId: string,
      workPeriods: Array<Pick<WorkScheduleStageWorkPeriodWeb, "startDate" | "endDate" | "isClosed">>
    ): WorkScheduleAssignmentConflictWeb[] => {
      const next = detectAssigneeConflicts({ workId, workPeriods, candidates });
      setConflicts(next);
      return next;
    },
    []
  );

  const clearConflicts = useCallback(() => {
    setConflicts([]);
  }, []);

  return { conflicts, isChecking: false, checkConflicts, clearConflicts };
}
