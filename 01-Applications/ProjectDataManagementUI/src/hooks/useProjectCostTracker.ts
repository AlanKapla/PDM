import { useState, useEffect, useCallback } from "react";
import { costTrackerApi } from "../api/costTrackerApi";
import type { CostTrackerDetailsWeb } from "../types/costTracker.types";

export function useProjectCostTracker(tenantId: string, projectId: string) {
  const [data, setData] = useState<CostTrackerDetailsWeb | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    if (!tenantId || !projectId) return;
    try {
      setIsLoading(true);
      setError(null);
      const result = await costTrackerApi.getByProject(tenantId, projectId);
      setData(result);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Błąd podczas ładowania danych trackera budżetu"
      );
    } finally {
      setIsLoading(false);
    }
  }, [tenantId, projectId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, isLoading, error, refetch: fetchData };
}
