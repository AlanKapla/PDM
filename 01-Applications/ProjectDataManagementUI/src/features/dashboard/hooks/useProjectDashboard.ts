import { useState, useEffect, useCallback, useRef } from 'react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { getApiErrorMessage } from '../../../utils/apiErrorUtils';
import { getProjectDashboard } from '../services/dashboardApi';

export interface UseProjectDashboardResult {
  data: ProjectDashboardWeb | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => void;
}

/**
 * Pobiera dane dashboardu projektu.
 * Endpoint: GET api/tenants/{tenantId}/projects/{projectId}/dashboard
 */
export function useProjectDashboard(
  tenantId: string | undefined,
  projectId: string | undefined
): UseProjectDashboardResult {
  const [data, setData] = useState<ProjectDashboardWeb | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  const fetchDashboard = useCallback(async () => {
    if (!tenantId || !projectId) return;
    abortControllerRef.current?.abort();
    const controller = new AbortController();
    abortControllerRef.current = controller;

    setIsLoading(true);
    setError(null);
    try {
      const result = await getProjectDashboard(tenantId, projectId, controller.signal);
      if (!controller.signal.aborted) {
        setData(result);
      }
    } catch (err) {
      if (!controller.signal.aborted) {
        setError(getApiErrorMessage(err));
      }
    } finally {
      if (!controller.signal.aborted) {
        setIsLoading(false);
      }
    }
  }, [tenantId, projectId]);

  useEffect(() => {
    fetchDashboard();
    return () => {
      abortControllerRef.current?.abort();
    };
  }, [fetchDashboard]);

  return { data, isLoading, error, refetch: fetchDashboard };
}

export default useProjectDashboard;
