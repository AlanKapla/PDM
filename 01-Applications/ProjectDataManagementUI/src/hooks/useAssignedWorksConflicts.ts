import { useMemo } from "react";
import { useMyAssignedWorks } from "./queries";
import { flattenWorks } from "../utils/myWorksTree";
import { detectSameDayWorkConflicts } from "../utils/detectSameDayWorkConflicts";

export interface UseAssignedWorksConflictsResult {
  hasConflicts: boolean;
  conflictCount: number;
  isLoading: boolean;
}

/** Czy użytkownik ma nakładające się otwarte prace w tych samych dniach. */
export function useAssignedWorksConflicts(): UseAssignedWorksConflictsResult {
  const { data = [], isLoading } = useMyAssignedWorks();

  const conflicts = useMemo(() => {
    const flatWorks = flattenWorks(data);
    return detectSameDayWorkConflicts(flatWorks);
  }, [data]);

  return {
    hasConflicts: conflicts.length > 0,
    conflictCount: conflicts.length,
    isLoading,
  };
}
