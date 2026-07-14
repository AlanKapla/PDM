import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { QueryClient } from '@tanstack/react-query';
import { reportApiError } from '../../utils/apiErrorToastBridge';
import {
  costEstimateApi,
  getAdditionalFields,
  addAdditionalField,
  updateAdditionalField,
  deleteAdditionalField,
  reorderAdditionalFields,
} from '../../api/costEstimateApi';
import { ResourceScope } from '../../api/projectApi';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
  CostEstimateAdditionalFieldWeb,
  CostEstimateListItemWeb,
  AdditionalFieldType,
  ReorderItemDto,
  ReorderItemChildDto,
  ReorderGroupDto,
} from '../../types/costEstimate.types.new';

export const costEstimateKeys = {
  all: ['cost-estimate'] as const,
  lists: (tenantId: string, projectId: string) =>
    ['cost-estimate', tenantId, projectId, 'list'] as const,
  list: (tenantId: string, projectId: string, scope: ResourceScope) =>
    ['cost-estimate', tenantId, projectId, 'list', scope] as const,
  detail: (tenantId: string, projectId: string, estimateId: string) =>
    ['cost-estimate', tenantId, projectId, 'detail', estimateId] as const,
  additionalFields: (tenantId: string, projectId: string, estimateId: string) =>
    ['cost-estimate', tenantId, projectId, estimateId, 'additional-fields'] as const,
};

/** Invaliduje wszystkie listy kosztorysów projektu (Mine / All / Shared). */
export function invalidateCostEstimateLists(
  queryClient: QueryClient,
  tenantId: string,
  projectId: string,
): Promise<void> {
  return queryClient.invalidateQueries({
    queryKey: costEstimateKeys.lists(tenantId, projectId),
  });
}

export function useCostEstimatesByScope(
  tenantId: string | undefined,
  projectId: string | undefined,
  scope: ResourceScope,
  enabled: boolean = true,
) {
  return useQuery<CostEstimateListItemWeb[]>({
    queryKey: costEstimateKeys.list(tenantId ?? '', projectId ?? '', scope),
    queryFn: () => costEstimateApi.getCostEstimatesByScope(tenantId!, projectId!, scope),
    enabled: Boolean(tenantId && projectId && enabled),
    staleTime: 0,
  });
}

export function useCostEstimateDetails(
  tenantId: string | undefined,
  projectId: string | undefined,
  estimateId: string | undefined,
) {
  return useQuery<CostEstimateDetailsWeb>({
    queryKey: costEstimateKeys.detail(tenantId ?? '', projectId ?? '', estimateId ?? ''),
    queryFn: () =>
      costEstimateApi.getCostEstimateDetails(tenantId!, projectId!, estimateId!),
    enabled: Boolean(tenantId && projectId && estimateId),
  });
}

/**
 * Query: pobiera definicje pól dodatkowych kosztorysu.
 * Uwaga: additionalFields są już dostępne w details (CostEstimateDetailsWeb.additionalFields).
 * Używaj tego hooka tylko gdy potrzebujesz odświeżyć listę niezależnie od details.
 */
export function useAdditionalFields(
  tenantId: string | undefined,
  projectId: string | undefined,
  costEstimateId: string | undefined,
) {
  return useQuery<CostEstimateAdditionalFieldWeb[]>({
    queryKey: costEstimateKeys.additionalFields(
      tenantId ?? '',
      projectId ?? '',
      costEstimateId ?? '',
    ),
    queryFn: () => getAdditionalFields(tenantId!, projectId!, costEstimateId!),
    enabled: Boolean(tenantId && projectId && costEstimateId),
  });
}

/**
 * Mutation: dodaje nowe pole dodatkowe do schematu kosztorysu.
 * Po sukcesie invaliduje details (zawierające additionalFields).
 */
export function useAddAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: { name: string; fieldType: AdditionalFieldType; order?: number }) =>
      addAdditionalField(tenantId, projectId, costEstimateId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: costEstimateKeys.detail(tenantId, projectId, costEstimateId),
      });
      void queryClient.invalidateQueries({
        queryKey: costEstimateKeys.additionalFields(tenantId, projectId, costEstimateId),
      });
    },
  });
}

/**
 * Mutation: aktualizuje definicję pola dodatkowego.
 */
export function useUpdateAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (params: {
      fieldId: string;
      data: { name?: string; fieldType?: AdditionalFieldType; order?: number };
    }) => updateAdditionalField(tenantId, projectId, costEstimateId, params.fieldId, params.data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: costEstimateKeys.detail(tenantId, projectId, costEstimateId),
      });
      void queryClient.invalidateQueries({
        queryKey: costEstimateKeys.additionalFields(tenantId, projectId, costEstimateId),
      });
    },
  });
}

/**
 * Mutation: usuwa pole dodatkowe wraz ze wszystkimi wartościami.
 */
export function useDeleteAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (fieldId: string) =>
      deleteAdditionalField(tenantId, projectId, costEstimateId, fieldId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: costEstimateKeys.detail(tenantId, projectId, costEstimateId),
      });
      void queryClient.invalidateQueries({
        queryKey: costEstimateKeys.additionalFields(tenantId, projectId, costEstimateId),
      });
    },
  });
}

/**
 * Mutation: zmienia kolejność pól dodatkowych.
 */
export function useReorderAdditionalFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (fieldIds: string[]) =>
      reorderAdditionalFields(tenantId, projectId, costEstimateId, fieldIds),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: costEstimateKeys.detail(tenantId, projectId, costEstimateId),
      });
      void queryClient.invalidateQueries({
        queryKey: costEstimateKeys.additionalFields(tenantId, projectId, costEstimateId),
      });
    },
  });
}

/**
 * Mutation: zmienia kolejność pozycji w grupie.
 * Optimistic update: natychmiast aktualizuje kolejność w cache, potem refetch w tle.
 */
export function useReorderCostEstimateItems(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
) {
  const queryClient = useQueryClient();
  const queryKey = costEstimateKeys.detail(tenantId, projectId, costEstimateId);

  return useMutation({
    mutationFn: (params: { groupId: string; items: ReorderItemDto[] }) =>
      costEstimateApi.reorderItems(tenantId, projectId, costEstimateId, params.groupId, {
        costEstimateId,
        items: params.items,
      }),
    onMutate: async (params) => {
      // Anuluj wychodzące refetchy by nie nadpisały naszego optimistic update
      await queryClient.cancelQueries({ queryKey });

      // Snapshot poprzedniego stanu
      const previousDetails = queryClient.getQueryData<CostEstimateDetailsWeb>(queryKey);

      // Optimistic update w cache
      queryClient.setQueryData<CostEstimateDetailsWeb | undefined>(queryKey, (old) => {
        if (!old) return old;
        return {
          ...old,
          rootGroups: old.rootGroups.map((group) => {
            if (group.id !== params.groupId) return group;
            // Przypisz nowe order do pozycji w tej grupie
            const itemOrderMap = new Map(params.items.map((i) => [i.itemId, i.order]));
            return {
              ...group,
              items: group.items
                .map((item) => ({
                  ...item,
                  order: itemOrderMap.get(item.id) ?? item.order,
                }))
                .sort((a, b) => a.order - b.order),
            };
          }),
        };
      });

      return { previousDetails };
    },
    onError: (err, _params, context) => {
      if (context?.previousDetails) {
        queryClient.setQueryData(queryKey, context.previousDetails);
      }
      reportApiError(err);
    },
    onSettled: () => {
      // Refetch w tle by zsynchronizować z backendem
      void queryClient.invalidateQueries({ queryKey });
    },
  });
}

/**
 * Mutation: zmienia kolejność elementów potomnych (opcji/komponentów) w pozycji nadrzędnej.
 * Optimistic update: natychmiast aktualizuje kolejność w cache, potem refetch w tle.
 */
export function useReorderCostEstimateItemChildren(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
) {
  const queryClient = useQueryClient();
  const queryKey = costEstimateKeys.detail(tenantId, projectId, costEstimateId);

  return useMutation({
    mutationFn: (params: { parentItemId: string; items: ReorderItemChildDto[] }) =>
      costEstimateApi.reorderItemChildren(tenantId, projectId, costEstimateId, params.parentItemId, {
        costEstimateId,
        items: params.items,
      }),
    onMutate: async (params) => {
      await queryClient.cancelQueries({ queryKey });

      const previousDetails = queryClient.getQueryData<CostEstimateDetailsWeb>(queryKey);

      queryClient.setQueryData<CostEstimateDetailsWeb | undefined>(queryKey, (old) => {
        if (!old) return old;
        const itemOrderMap = new Map(params.items.map((i) => [i.itemId, i.order]));

        const updateChildrenRecursive = (items: CostEstimateItemWeb[]): CostEstimateItemWeb[] =>
          items.map((item) => {
            const updatedItem = { ...item };
            // Zaktualizuj order dla opcji
            if (updatedItem.options) {
              updatedItem.options = updatedItem.options
                .map((opt) => ({
                  ...opt,
                  order: itemOrderMap.get(opt.id) ?? opt.order,
                }))
                .sort((a, b) => a.order - b.order);
            }
            // Zaktualizuj order dla komponentów
            if (updatedItem.components) {
              updatedItem.components = updatedItem.components
                .map((comp) => ({
                  ...comp,
                  order: itemOrderMap.get(comp.id) ?? comp.order,
                }))
                .sort((a, b) => a.order - b.order);
            }
            // Rekurencja dla dzieci (opcji mogących mieć komponenty itd.)
            if (updatedItem.options) {
              updatedItem.options = updateChildrenRecursive(updatedItem.options);
            }
            if (updatedItem.components) {
              updatedItem.components = updateChildrenRecursive(updatedItem.components);
            }
            return updatedItem;
          });

        return {
          ...old,
          rootGroups: old.rootGroups.map((group) => ({
            ...group,
            items: updateChildrenRecursive(group.items),
          })),
        };
      });

      return { previousDetails };
    },
    onError: (err, _params, context) => {
      if (context?.previousDetails) {
        queryClient.setQueryData(queryKey, context.previousDetails);
      }
      reportApiError(err);
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey });
    },
  });
}

/**
 * Mutation: zmienia kolejność grup.
 * Optimistic update: natychmiast aktualizuje kolejność w cache, potem refetch w tle.
 */
export function useReorderCostEstimateGroups(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
) {
  const queryClient = useQueryClient();
  const queryKey = costEstimateKeys.detail(tenantId, projectId, costEstimateId);

  return useMutation({
    mutationFn: (groups: ReorderGroupDto[]) =>
      costEstimateApi.reorderGroups(tenantId, projectId, costEstimateId, {
        costEstimateId,
        groups,
      }),
    onMutate: async (groups) => {
      await queryClient.cancelQueries({ queryKey });

      const previousDetails = queryClient.getQueryData<CostEstimateDetailsWeb>(queryKey);

      const groupOrderMap = new Map(groups.map((g) => [g.groupId, g.order]));
      const parentGroupMap = new Map(groups.map((g) => [g.groupId, g.parentGroupId]));

      const reorderGroupsRecursive = (grps: CostEstimateGroupWeb[], parentId: string | null): CostEstimateGroupWeb[] =>
        grps
          .map((group) => {
            const newOrder = groupOrderMap.get(group.id) ?? group.order;
            const newParentId = parentGroupMap.has(group.id)
              ? (parentGroupMap.get(group.id) ?? undefined)
              : group.parentGroupId;
            return {
              ...group,
              order: newOrder,
              parentGroupId: newParentId,
              childGroups: reorderGroupsRecursive(group.childGroups ?? [], group.id),
            };
          })
          .sort((a, b) => a.order - b.order);

      queryClient.setQueryData<CostEstimateDetailsWeb | undefined>(queryKey, (old) => {
        if (!old) return old;
        return {
          ...old,
          rootGroups: reorderGroupsRecursive(old.rootGroups, null),
        };
      });

      return { previousDetails };
    },
    onError: (err, _params, context) => {
      if (context?.previousDetails) {
        queryClient.setQueryData(queryKey, context.previousDetails);
      }
      reportApiError(err);
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey });
    },
  });
}
