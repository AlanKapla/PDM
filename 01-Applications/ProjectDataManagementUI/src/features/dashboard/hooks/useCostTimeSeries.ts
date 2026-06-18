import { useMemo } from 'react';
import type { TrackedCostWeb } from '../types/projectDashboard.types';
import { aggregateCostsByMonth } from '../utils/chartAggregations';
import type { TimeSeriesPoint } from '../utils/chartAggregations';

export interface UseCostTimeSeriesResult {
  points: TimeSeriesPoint[];
  hasUndatedCosts: boolean;
}

export function useCostTimeSeries(costs: TrackedCostWeb[]): UseCostTimeSeriesResult {
  return useMemo(() => {
    const hasUndatedCosts = costs.some((cost) => !cost.date && !cost.createdAt);
    const points = aggregateCostsByMonth(costs);
    return { points, hasUndatedCosts };
  }, [costs]);
}
