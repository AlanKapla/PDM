import { useMutation, useQueryClient } from '@tanstack/react-query';
import { aiCostApi } from '../api/aiCostApi';
import { aiCostImportKeys } from './usePendingAICostImports';
import type { AICostImportBatchWeb, SubmitAICostImportBatchRequest } from '../types/ai.types';

interface UseAICostImportBatchParams {
  tenantId: string;
  projectId: string;
}

export function useAICostImportBatch({ tenantId, projectId }: UseAICostImportBatchParams) {
  const queryClient = useQueryClient();

  return useMutation<AICostImportBatchWeb, Error, SubmitAICostImportBatchRequest>({
    mutationFn: (data: SubmitAICostImportBatchRequest) =>
      aiCostApi.submitImportBatch(tenantId, projectId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: aiCostImportKeys.all,
      });
    },
  });
}
