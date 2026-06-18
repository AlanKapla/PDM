import { useMemo } from 'react';
import type { ScheduleSummaryWeb } from '../types/projectDashboard.types';
import {
  buildDelayedStages,
  buildSCurveData,
  flattenAllStages,
} from '../utils/chartAggregations';
import type { FlattenedStage, SCurvePoint } from '../utils/chartAggregations';

export interface UseFlattenedStagesResult {
  stages: FlattenedStage[];
  delayedStages: FlattenedStage[];
  sCurvePoints: SCurvePoint[];
}

export function useFlattenedStages(
  summaries: ScheduleSummaryWeb[]
): UseFlattenedStagesResult {
  return useMemo(() => {
    const stages = flattenAllStages(summaries);
    const delayedStages = buildDelayedStages(stages);
    const sCurvePoints = buildSCurveData(stages);
    return { stages, delayedStages, sCurvePoints };
  }, [summaries]);
}
