import { useState, useCallback } from 'react';
import type {
  TrackedCostWeb,
  CreateTrackedCostRequest,
  UpdateTrackedCostRequest,
  UpdateTrackerBudgetRequest,
} from '../types/projectDashboard.types';
import {
  createTrackedCost,
  updateTrackedCost,
  deleteTrackedCost,
  updateTrackerBudget,
} from '../services/dashboardApi';
import { getApiErrorMessage } from '../../../utils/apiErrorUtils';

export interface UseTrackedCostMutationsParams {
  tenantId: string;
  projectId: string;
  onSuccess?: () => void;
}

export interface UseTrackedCostMutationsResult {
  createCost: (
    data: CreateTrackedCostRequest
  ) => Promise<TrackedCostWeb>;
  updateCost: (
    costId: string,
    data: UpdateTrackedCostRequest
  ) => Promise<TrackedCostWeb>;
  deleteCost: (costId: string) => Promise<void>;
  updateBudget: (
    data: UpdateTrackerBudgetRequest
  ) => Promise<void>;
  isLoading: boolean;
  error: string | null;
}

/**
 * Mutacje dla kosztów śledzonych i budżetu.
 * Po każdej udanej operacji wywołuje callback onSuccess (np. refetch dashboardu).
 */
export function useTrackedCostMutations({
  tenantId,
  projectId,
  onSuccess,
}: UseTrackedCostMutationsParams): UseTrackedCostMutationsResult {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createCost = useCallback(
    async (
      data: CreateTrackedCostRequest
    ): Promise<TrackedCostWeb> => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await createTrackedCost(tenantId, projectId, data);
        onSuccess?.();
        return result;
      } catch (err) {
        const msg = getApiErrorMessage(err);
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  const updateCost = useCallback(
    async (
      costId: string,
      data: UpdateTrackedCostRequest
    ): Promise<TrackedCostWeb> => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await updateTrackedCost(tenantId, projectId, costId, data);
        onSuccess?.();
        return result;
      } catch (err) {
        const msg = getApiErrorMessage(err);
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  const deleteCost = useCallback(
    async (costId: string): Promise<void> => {
      setIsLoading(true);
      setError(null);
      try {
        await deleteTrackedCost(tenantId, projectId, costId);
        onSuccess?.();
      } catch (err) {
        const msg = getApiErrorMessage(err);
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  const updateBudget = useCallback(
    async (
      data: UpdateTrackerBudgetRequest
    ): Promise<void> => {
      setIsLoading(true);
      setError(null);
      try {
        await updateTrackerBudget(tenantId, projectId, data);
        onSuccess?.();
      } catch (err) {
        const msg = getApiErrorMessage(err);
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  return { createCost, updateCost, deleteCost, updateBudget, isLoading, error };
}

export default useTrackedCostMutations;
