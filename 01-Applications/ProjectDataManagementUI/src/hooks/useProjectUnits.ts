import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { projectApi } from '../api/projectApi';

export function useProjectUnits(tenantId: string, projectId: string) {
  return useQuery({
    queryKey: ['projectUnits', tenantId, projectId],
    queryFn: () => projectApi.getProjectUnits(tenantId, projectId),
    staleTime: 5 * 60 * 1000,
    enabled: !!tenantId && !!projectId,
  });
}

export function useAddProjectUnit(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: { code: string; name: string; symbol?: string }) =>
      projectApi.addProjectUnit(tenantId, projectId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectUnits', tenantId, projectId] });
    },
  });
}

export function useUpdateProjectUnit(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      unitId,
      data,
    }: {
      unitId: string;
      data: { code: string; name: string; symbol?: string; order: number };
    }) => projectApi.updateProjectUnit(tenantId, projectId, unitId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectUnits', tenantId, projectId] });
    },
  });
}

export function useDeleteProjectUnit(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (unitId: string) =>
      projectApi.deleteProjectUnit(tenantId, projectId, unitId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectUnits', tenantId, projectId] });
    },
  });
}

export function useReorderProjectUnits(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (unitIds: string[]) =>
      projectApi.reorderProjectUnits(tenantId, projectId, unitIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectUnits', tenantId, projectId] });
    },
  });
}
