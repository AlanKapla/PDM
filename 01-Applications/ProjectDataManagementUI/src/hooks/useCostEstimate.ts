import { useState, useEffect, useCallback } from 'react';
import { costEstimateApiNew } from '../api/costEstimateApiNew';
import type {
  CostEstimateDetailsWeb,
  CostEstimateListItemWeb,
} from '../types/costEstimate.types.new';
import { ResourceScope } from '../api/projectApi';

/**
 * Hook for loading cost estimate details
 */
export function useCostEstimateDetails(
  tenantId: string,
  projectId: string,
  costEstimateId: string
) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [details, setDetails] = useState<CostEstimateDetailsWeb | null>(null);

  const loadDetails = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const data = await costEstimateApiNew.getCostEstimateDetails(
        tenantId,
        projectId,
        costEstimateId
      );
      
      setDetails(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas ładowania kosztorysu');
      console.error('Error loading cost estimate details:', err);
    } finally {
      setLoading(false);
    }
  }, [tenantId, projectId, costEstimateId]);

  useEffect(() => {
    loadDetails();
  }, [loadDetails]);

  return {
    loading,
    error,
    details,
    reload: loadDetails,
  };
}

/**
 * Hook for loading cost estimate list
 */
export function useCostEstimateList(
  tenantId: string,
  projectId: string,
  scope: ResourceScope = ResourceScope.Mine
) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [items, setItems] = useState<CostEstimateListItemWeb[]>([]);

  const loadList = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const data = await costEstimateApiNew.getCostEstimatesByScope(
        tenantId,
        projectId,
        scope
      );
      
      setItems(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas ładowania listy kosztorysów');
      console.error('Error loading cost estimate list:', err);
    } finally {
      setLoading(false);
    }
  }, [tenantId, projectId, scope]);

  useEffect(() => {
    loadList();
  }, [loadList]);

  return {
    loading,
    error,
    items,
    reload: loadList,
  };
}
