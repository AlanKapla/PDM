import { useCostTrackerByProject } from './queries';

/**
 * @deprecated Użyj useCostTrackerByProject z hooks/queries
 * Wrapper zachowany dla kompatybilności wstecznej.
 */
export function useProjectCostTracker(tenantId: string, projectId: string) {
  const query = useCostTrackerByProject(tenantId, projectId);
  return {
    data: query.data ?? null,
    isLoading: query.isLoading,
    error: query.error
      ? (query.error instanceof Error
          ? query.error.message
          : "Błąd podczas ładowania danych trackera budżetu")
      : null,
    refetch: query.refetch,
  };
}
