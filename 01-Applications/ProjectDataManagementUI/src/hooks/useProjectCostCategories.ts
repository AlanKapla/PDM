import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { projectApi } from '../api/projectApi';
import type { UpsertProjectCostCategoryDto } from '../api/projectApi';

export function useProjectCostCategories(tenantId: string, projectId: string) {
  return useQuery({
    queryKey: ['projectCostCategories', tenantId, projectId],
    queryFn: () => projectApi.getProjectCostCategories(tenantId, projectId),
    staleTime: 5 * 60 * 1000,
    enabled: !!tenantId && !!projectId,
  });
}

export function useAddProjectCostCategory(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: { name: string; code?: string; color?: string }) =>
      projectApi.addProjectCostCategory(tenantId, projectId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectCostCategories', tenantId, projectId] });
    },
  });
}

export function useUpdateProjectCostCategory(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      categoryId,
      data,
    }: {
      categoryId: string;
      data: UpsertProjectCostCategoryDto;
    }) => projectApi.updateProjectCostCategory(tenantId, projectId, categoryId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectCostCategories', tenantId, projectId] });
    },
  });
}

export function useDeleteProjectCostCategory(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (categoryId: string) =>
      projectApi.deleteProjectCostCategory(tenantId, projectId, categoryId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectCostCategories', tenantId, projectId] });
    },
  });
}

export function useReorderProjectCostCategories(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (categoryIds: string[]) =>
      projectApi.reorderProjectCostCategories(tenantId, projectId, categoryIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectCostCategories', tenantId, projectId] });
    },
  });
}
