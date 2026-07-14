/**
 * Hook do zarządzania stanem isSelected pozycji kosztorysu (opcje).
 *
 * Używa optimistic update: zmienia stan w cache natychmiast,
 * wysyła request do API, w razie błędu robi rollback.
 */

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { setItemIsSelected } from '../api/costEstimateApi';
import { costEstimateKeys } from './queries/useCostEstimate';
import type { CostEstimateDetailsWeb, CostEstimateItemWeb } from '../types/costEstimate.types.new';

export interface UseItemSelectionParams {
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  onSuccess?: () => void;
  onError?: (error: Error) => void;
}

export interface UseItemSelectionReturn {
  setSelected: (itemId: string, isSelected: boolean) => Promise<void>;
  isPending: boolean;
}

/**
 * Rekurencyjnie aktualizuje flagę isSelected na pozycji w drzewie.
 * Zwraca nowe drzewo (immutable update).
 */
function updateItemIsSelectedInDetails(
  details: CostEstimateDetailsWeb,
  itemId: string,
  isSelected: boolean,
): CostEstimateDetailsWeb {
  function updateItem(item: CostEstimateItemWeb): CostEstimateItemWeb {
    if (item.id === itemId) {
      return { ...item, isSelected };
    }
    const updatedOptions = item.options?.map(updateItem);
    const updatedComponents = item.components?.map(updateItem);
    if (updatedOptions === item.options && updatedComponents === item.components) {
      return item;
    }
    return { ...item, options: updatedOptions, components: updatedComponents };
  }

  return {
    ...details,
    rootGroups: details.rootGroups.map((group) => ({
      ...group,
      items: group.items.map(updateItem),
      childGroups: group.childGroups.map((childGroup) => ({
        ...childGroup,
        items: childGroup.items.map(updateItem),
      })),
    })),
  };
}

export function useItemSelection(params: UseItemSelectionParams): UseItemSelectionReturn {
  const { tenantId, projectId, costEstimateId, onSuccess, onError } = params;
  const queryClient = useQueryClient();

  const detailQueryKey = costEstimateKeys.detail(tenantId, projectId, costEstimateId);

  const mutation = useMutation({
    mutationFn: ({ itemId, isSelected }: { itemId: string; isSelected: boolean }) =>
      setItemIsSelected(tenantId, projectId, costEstimateId, itemId, isSelected),

    onMutate: async ({ itemId, isSelected }) => {
      // Anuluj in-flight refetchy żeby nie nadpisały optimistic update
      await queryClient.cancelQueries({ queryKey: detailQueryKey });

      // Zapisz poprzedni stan do rollbacku
      const previousDetails = queryClient.getQueryData<CostEstimateDetailsWeb>(detailQueryKey);

      // Optimistic update
      if (previousDetails) {
        const updatedDetails = updateItemIsSelectedInDetails(previousDetails, itemId, isSelected);
        queryClient.setQueryData<CostEstimateDetailsWeb>(detailQueryKey, updatedDetails);
      }

      return { previousDetails };
    },

    onError: (error, _variables, context) => {
      // Rollback do poprzedniego stanu
      if (context?.previousDetails) {
        queryClient.setQueryData<CostEstimateDetailsWeb>(detailQueryKey, context.previousDetails);
      }
      onError?.(error instanceof Error ? error : new Error(String(error)));
    },

    onSuccess: () => {
      // Odśwież dane z serwera po sukcesie
      void queryClient.invalidateQueries({ queryKey: detailQueryKey });
      onSuccess?.();
    },
  });

  const setSelected = async (itemId: string, isSelected: boolean): Promise<void> => {
    await mutation.mutateAsync({ itemId, isSelected });
  };

  return {
    setSelected,
    isPending: mutation.isPending,
  };
}
