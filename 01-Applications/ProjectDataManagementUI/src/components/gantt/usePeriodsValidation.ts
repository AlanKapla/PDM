import { useCallback } from "react";
import { useGantt } from "./GanttContext";
import { WorkDependencyType } from "../../types/workSchedule.types";
import type {
  WorkScheduleDetailsWeb,
  WorkScheduleStageWeb,
  WorkScheduleStageWorkWeb,
} from "../../types/workSchedule.types";

export interface DependencyViolation {
  dependencyType: WorkDependencyType;
  predecessorName: string;
  requiredDate: Date;
  violatedField: "startDate" | "endDate";
  lagDays: number;
}

export interface DependencyWarning {
  successorName: string;
  willBeShiftedBy: number;
}

export interface PeriodsValidationResult {
  valid: boolean;
  errors: DependencyViolation[];
  warnings: DependencyWarning[];
}

function findWork(
  schedule: WorkScheduleDetailsWeb,
  workId: string,
): WorkScheduleStageWorkWeb | null {
  function searchStages(stages: WorkScheduleStageWeb[]): WorkScheduleStageWorkWeb | null {
    for (const stage of stages) {
      for (const work of stage.works) {
        if (work.id === workId) return work;
      }
      if (stage.childStages) {
        const found = searchStages(stage.childStages);
        if (found) return found;
      }
    }
    return null;
  }
  return searchStages(schedule.stages);
}

function getWorkDateRange(work: WorkScheduleStageWorkWeb): { start: Date; end: Date } | null {
  if (!work.periods || work.periods.length === 0) return null;
  const times = work.periods.flatMap(p => [
    new Date(p.startDate).getTime(),
    new Date(p.endDate).getTime(),
  ]);
  return {
    start: new Date(Math.min(...times.filter((_, i) => i % 2 === 0))),
    end: new Date(Math.max(...times.filter((_, i) => i % 2 === 1))),
  };
}

export function usePeriodsValidation() {
  const { schedule } = useGantt();

  const validate = useCallback(
    (
      workId: string,
      newPeriods: Array<{ startDate: string; endDate: string }>,
    ): PeriodsValidationResult => {
      if (!schedule || newPeriods.length === 0) {
        return { valid: true, errors: [], warnings: [] };
      }

      const startTimes = newPeriods.map(p => new Date(p.startDate).getTime());
      const endTimes = newPeriods.map(p => new Date(p.endDate).getTime());
      const newStart = new Date(Math.min(...startTimes));
      const newEnd = new Date(Math.max(...endTimes));

      const errors: DependencyViolation[] = [];
      const warnings: DependencyWarning[] = [];

      for (const dep of schedule.dependencies) {
        const lagMs = dep.lagDays * 86_400_000;

        if (dep.successorWorkId === workId) {
          // Bieżący zakres jest następnikiem — musi respektować daty poprzednika
          const pred = findWork(schedule, dep.predecessorWorkId);
          if (!pred) continue;
          const predRange = getWorkDateRange(pred);
          if (!predRange) continue;

          let required: Date;
          let violatedField: "startDate" | "endDate";
          let actual: Date;

          switch (dep.dependencyType) {
            case WorkDependencyType.FinishToStart:
              required = new Date(predRange.end.getTime() + lagMs);
              violatedField = "startDate";
              actual = newStart;
              break;
            case WorkDependencyType.StartToStart:
              required = new Date(predRange.start.getTime() + lagMs);
              violatedField = "startDate";
              actual = newStart;
              break;
            case WorkDependencyType.FinishToFinish:
              required = new Date(predRange.end.getTime() + lagMs);
              violatedField = "endDate";
              actual = newEnd;
              break;
            case WorkDependencyType.StartToFinish:
              required = new Date(predRange.start.getTime() + lagMs);
              violatedField = "endDate";
              actual = newEnd;
              break;
            default:
              continue;
          }

          if (actual < required) {
            errors.push({
              dependencyType: dep.dependencyType,
              predecessorName: pred.name,
              requiredDate: required,
              violatedField,
              lagDays: dep.lagDays,
            });
          }
        } else if (dep.predecessorWorkId === workId) {
          // Bieżący zakres jest poprzednikiem — sprawdź czy następnik zostanie naruszony
          const succ = findWork(schedule, dep.successorWorkId);
          if (!succ) continue;
          const succRange = getWorkDateRange(succ);
          if (!succRange) continue;

          let shiftMs = 0;

          switch (dep.dependencyType) {
            case WorkDependencyType.FinishToStart:
              shiftMs = newEnd.getTime() + lagMs - succRange.start.getTime();
              break;
            case WorkDependencyType.StartToStart:
              shiftMs = newStart.getTime() + lagMs - succRange.start.getTime();
              break;
            case WorkDependencyType.FinishToFinish:
              shiftMs = newEnd.getTime() + lagMs - succRange.end.getTime();
              break;
            case WorkDependencyType.StartToFinish:
              shiftMs = newStart.getTime() + lagMs - succRange.end.getTime();
              break;
          }

          if (shiftMs > 0) {
            warnings.push({
              successorName: succ.name,
              willBeShiftedBy: Math.ceil(shiftMs / 86_400_000),
            });
          }
        }
      }

      return { valid: errors.length === 0, errors, warnings };
    },
    [schedule],
  );

  return { validate };
}
