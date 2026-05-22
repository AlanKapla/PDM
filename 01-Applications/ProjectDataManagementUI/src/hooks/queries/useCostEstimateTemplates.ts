import { useQuery } from '@tanstack/react-query';
import { costEstimateTemplateApi } from '../../api/costEstimateTemplateApi';
import type {
  CostEstimateTemplateListItem,
  CostEstimateTemplateDetails,
  CostEstimateTemplateStructureWeb,
} from '../../api/costEstimateTemplateApi';
import type { DefaultCostEstimateTemplateListItemWeb } from '../../types/costEstimate.types';

export const templateKeys = {
  all: ['cost-estimate-templates'] as const,
  list: () => ['cost-estimate-templates', 'list'] as const,
  detail: (id: string) => ['cost-estimate-templates', 'detail', id] as const,
  defaults: () => ['cost-estimate-templates', 'defaults'] as const,
  default: (slug: string) =>
    ['cost-estimate-templates', 'default', slug] as const,
};

export function useCostEstimateTemplates() {
  return useQuery<CostEstimateTemplateListItem[]>({
    queryKey: templateKeys.list(),
    queryFn: () => costEstimateTemplateApi.getTemplates(),
  });
}

export function useCostEstimateTemplateDetails(id: string | undefined) {
  return useQuery<CostEstimateTemplateDetails>({
    queryKey: templateKeys.detail(id ?? ''),
    queryFn: () => costEstimateTemplateApi.getTemplateDetails(id!),
    enabled: Boolean(id),
  });
}

export function useDefaultTemplates() {
  return useQuery<DefaultCostEstimateTemplateListItemWeb[]>({
    queryKey: templateKeys.defaults(),
    queryFn: () => costEstimateTemplateApi.getDefaultTemplates(),
  });
}

export function useDefaultTemplate(slug: string | undefined) {
  return useQuery<CostEstimateTemplateStructureWeb>({
    queryKey: templateKeys.default(slug ?? ''),
    queryFn: () => costEstimateTemplateApi.getDefaultTemplate(slug!),
    enabled: Boolean(slug),
  });
}
