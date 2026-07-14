import { useMemo } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { aiCostApi } from '../api/aiCostApi';
import type {
  AICostImportItemWeb,
  CostDocumentType,
  PendingAICostImportCountWeb,
  UpdateAICostImportItemRequest,
} from '../types/ai.types';

export const aiCostImportKeys = {
  all: ['ai-cost-import'] as const,
  pending: (tenantId: string, projectId: string) =>
    ['ai-cost-import', tenantId, projectId, 'pending'] as const,
  pendingItem: (tenantId: string, projectId: string, itemId: string) =>
    ['ai-cost-import', tenantId, projectId, 'pending', itemId] as const,
  count: (tenantId: string, projectId: string) =>
    ['ai-cost-import', tenantId, projectId, 'count'] as const,
};

function invalidateAICostImportQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  tenantId: string,
  projectId: string
): void {
  queryClient.invalidateQueries({ queryKey: aiCostImportKeys.pending(tenantId, projectId) });
  queryClient.invalidateQueries({ queryKey: aiCostImportKeys.count(tenantId, projectId) });
}

export function usePendingAICostImportItems(
  tenantId: string | undefined,
  projectId: string | undefined
) {
  return useQuery<AICostImportItemWeb[]>({
    queryKey: aiCostImportKeys.pending(tenantId ?? '', projectId ?? ''),
    queryFn: () => aiCostApi.getPendingImportItems(tenantId!, projectId!),
    enabled: Boolean(tenantId && projectId),
  });
}

export function usePendingAICostImportItem(
  tenantId: string | undefined,
  projectId: string | undefined,
  itemId: string | undefined
) {
  return useQuery<AICostImportItemWeb>({
    queryKey: aiCostImportKeys.pendingItem(tenantId ?? '', projectId ?? '', itemId ?? ''),
    queryFn: () => aiCostApi.getPendingImportItem(tenantId!, projectId!, itemId!),
    enabled: Boolean(tenantId && projectId && itemId),
  });
}

export function usePendingAICostImportCount(
  tenantId: string | undefined,
  projectId: string | undefined
) {
  return useQuery<PendingAICostImportCountWeb>({
    queryKey: aiCostImportKeys.count(tenantId ?? '', projectId ?? ''),
    queryFn: () => aiCostApi.getPendingImportCount(tenantId!, projectId!),
    enabled: Boolean(tenantId && projectId),
    staleTime: 30_000,
  });
}

export function usePendingAICostImportCountByType(
  tenantId: string | undefined,
  projectId: string | undefined,
  costDocumentType: CostDocumentType
): PendingAICostImportCountWeb {
  const { data: items = [] } = usePendingAICostImportItems(tenantId, projectId);

  return useMemo(() => {
    const filtered = items.filter((item) => item.costDocumentType === costDocumentType);
    return {
      pendingCount: filtered.filter((item) => item.status === 'Pending').length,
      errorCount: filtered.filter((item) => item.status === 'ErrorNeedsReview').length,
      duplicateCount: filtered.filter((item) => item.status === 'DuplicateDetected').length,
    };
  }, [items, costDocumentType]);
}

export function useUpdatePendingAICostImportItem(
  tenantId: string,
  projectId: string
) {
  const queryClient = useQueryClient();

  return useMutation<
    AICostImportItemWeb,
    Error,
    { itemId: string; data: UpdateAICostImportItemRequest }
  >({
    mutationFn: ({ itemId, data }) =>
      aiCostApi.updatePendingImportItem(tenantId, projectId, itemId, data),
    onSuccess: (_result, variables) => {
      invalidateAICostImportQueries(queryClient, tenantId, projectId);
      queryClient.invalidateQueries({
        queryKey: aiCostImportKeys.pendingItem(tenantId, projectId, variables.itemId),
      });
    },
  });
}

export function useAcceptPendingAICostImportItem(
  tenantId: string,
  projectId: string
) {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: (itemId: string) =>
      aiCostApi.acceptPendingImportItem(tenantId, projectId, itemId),
    onSuccess: () => {
      invalidateAICostImportQueries(queryClient, tenantId, projectId);
    },
  });
}

export function useAcceptAllPendingAICostImportItems(
  tenantId: string,
  projectId: string
) {
  const queryClient = useQueryClient();

  return useMutation<void, Error, void>({
    mutationFn: () => aiCostApi.acceptAllPendingImportItems(tenantId, projectId),
    onSuccess: () => {
      invalidateAICostImportQueries(queryClient, tenantId, projectId);
    },
  });
}

export function useRejectPendingAICostImportItem(
  tenantId: string,
  projectId: string
) {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: (itemId: string) =>
      aiCostApi.rejectPendingImportItem(tenantId, projectId, itemId),
    onSuccess: () => {
      invalidateAICostImportQueries(queryClient, tenantId, projectId);
    },
  });
}
