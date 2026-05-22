import { useQuery } from '@tanstack/react-query';
import { costTrackerApi } from '../../api/costTrackerApi';
import type {
  CostTrackerDetailsWeb,
  CostEstimateSummaryWeb,
  TrackedCostWeb,
  CostLinkOptionsWeb,
} from '../../types/costTracker.types';

export const costTrackerKeys = {
  all: ['cost-tracker'] as const,
  byProject: (tenantId: string, projectId: string) =>
    ['cost-tracker', tenantId, projectId, 'by-project'] as const,
  byEstimate: (tenantId: string, projectId: string, estimateId: string) =>
    ['cost-tracker', tenantId, projectId, 'by-estimate', estimateId] as const,
  costs: (tenantId: string, projectId: string) =>
    ['cost-tracker', tenantId, projectId, 'costs'] as const,
  cost: (tenantId: string, projectId: string, costId: string) =>
    ['cost-tracker', tenantId, projectId, 'costs', costId] as const,
  itemCosts: (
    tenantId: string,
    projectId: string,
    estimateId: string,
    itemId: string
  ) =>
    ['cost-tracker', tenantId, projectId, 'by-estimate', estimateId, 'items', itemId] as const,
  linkOptions: (tenantId: string, projectId: string) =>
    ['cost-tracker', tenantId, projectId, 'link-options'] as const,
};

export function useCostTrackerByProject(
  tenantId: string | undefined,
  projectId: string | undefined
) {
  return useQuery<CostTrackerDetailsWeb>({
    queryKey: costTrackerKeys.byProject(tenantId ?? '', projectId ?? ''),
    queryFn: () => costTrackerApi.getByProject(tenantId!, projectId!),
    enabled: Boolean(tenantId && projectId),
  });
}

export function useCostTrackerByEstimate(
  tenantId: string | undefined,
  projectId: string | undefined,
  estimateId: string | undefined
) {
  return useQuery<CostEstimateSummaryWeb>({
    queryKey: costTrackerKeys.byEstimate(
      tenantId ?? '', projectId ?? '', estimateId ?? ''
    ),
    queryFn: () =>
      costTrackerApi.getByEstimate(tenantId!, projectId!, estimateId!),
    enabled: Boolean(tenantId && projectId && estimateId),
  });
}

export function useCostTrackerCosts(
  tenantId: string | undefined,
  projectId: string | undefined
) {
  return useQuery<TrackedCostWeb[]>({
    queryKey: costTrackerKeys.costs(tenantId ?? '', projectId ?? ''),
    queryFn: () => costTrackerApi.getCosts(tenantId!, projectId!),
    enabled: Boolean(tenantId && projectId),
  });
}

export function useCostTrackerItemCosts(
  tenantId: string | undefined,
  projectId: string | undefined,
  estimateId: string | undefined,
  itemId: string | undefined
) {
  return useQuery<TrackedCostWeb[]>({
    queryKey: costTrackerKeys.itemCosts(
      tenantId ?? '', projectId ?? '', estimateId ?? '', itemId ?? ''
    ),
    queryFn: () =>
      costTrackerApi.getItemCosts(tenantId!, projectId!, estimateId!, itemId!),
    enabled: Boolean(tenantId && projectId && estimateId && itemId),
  });
}

export function useCostLinkOptions(
  tenantId: string | undefined,
  projectId: string | undefined,
  enabled: boolean
) {
  return useQuery<CostLinkOptionsWeb>({
    queryKey: costTrackerKeys.linkOptions(tenantId ?? '', projectId ?? ''),
    queryFn: () => costTrackerApi.getLinkOptions(tenantId!, projectId!),
    enabled: Boolean(enabled && tenantId && projectId),
    staleTime: 1000 * 60 * 2,
  });
}
