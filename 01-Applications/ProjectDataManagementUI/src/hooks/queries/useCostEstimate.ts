import { useQuery } from '@tanstack/react-query';
import { costEstimateApi } from '../../api/costEstimateApi';
import type { CostEstimateDetailsWeb } from '../../types/costEstimate.types.new';

export const costEstimateKeys = {
  all: ['cost-estimate'] as const,
  detail: (tenantId: string, projectId: string, estimateId: string) =>
    ['cost-estimate', tenantId, projectId, 'detail', estimateId] as const,
};

export function useCostEstimateDetails(
  tenantId: string | undefined,
  projectId: string | undefined,
  estimateId: string | undefined
) {
  return useQuery<CostEstimateDetailsWeb>({
    queryKey: costEstimateKeys.detail(
      tenantId ?? '', projectId ?? '', estimateId ?? ''
    ),
    queryFn: () =>
      costEstimateApi.getCostEstimateDetails(tenantId!, projectId!, estimateId!),
    enabled: Boolean(tenantId && projectId && estimateId),
  });
}
