import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contractorApi } from '../../api/contractorApi';
import type {
  ContractorWeb,
  CreateContractorRequest,
  UpdateContractorRequest,
} from '../../types/contractor.types';

export const contractorKeys = {
  all: ['contractors'] as const,
  byTenant: (tenantId: string) => ['contractors', tenantId] as const,
  detail: (tenantId: string, id: string) => ['contractors', tenantId, id] as const,
};

export function useContractors(tenantId: string | undefined, search?: string) {
  return useQuery<ContractorWeb[]>({
    queryKey: [...contractorKeys.byTenant(tenantId ?? ''), search ?? ''],
    queryFn: () => contractorApi.getAll(tenantId!, search),
    enabled: Boolean(tenantId),
  });
}

export function useContractorDetails(
  tenantId: string | undefined,
  contractorId: string | undefined
) {
  return useQuery<ContractorWeb>({
    queryKey: contractorKeys.detail(tenantId ?? '', contractorId ?? ''),
    queryFn: () => contractorApi.getById(tenantId!, contractorId!),
    enabled: Boolean(tenantId && contractorId),
  });
}

export function useCreateContractor(tenantId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateContractorRequest) =>
      contractorApi.create(tenantId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: contractorKeys.byTenant(tenantId),
      });
    },
  });
}

export function useUpdateContractor(tenantId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ contractorId, data }: { contractorId: string; data: UpdateContractorRequest }) =>
      contractorApi.update(tenantId, contractorId, data),
    onSuccess: (_result, variables) => {
      queryClient.invalidateQueries({
        queryKey: contractorKeys.byTenant(tenantId),
      });
      queryClient.invalidateQueries({
        queryKey: contractorKeys.detail(tenantId, variables.contractorId),
      });
    },
  });
}

export function useDeleteContractor(tenantId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (contractorId: string) =>
      contractorApi.delete(tenantId, contractorId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: contractorKeys.byTenant(tenantId),
      });
    },
  });
}
