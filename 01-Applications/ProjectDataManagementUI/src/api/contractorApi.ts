import { axiosClient } from './axiosClient';
import type {
  ContractorWeb,
  ContractorListItemWeb,
  CreateContractorRequest,
  UpdateContractorRequest,
} from '../types/contractor.types';

export const contractorApi = {
  getAll: async (tenantId: string, search?: string): Promise<ContractorWeb[]> => {
    const params = search ? { search } : undefined;
    const res = await axiosClient.get<ContractorWeb[]>(
      `/tenants/${tenantId}/contractors`,
      { params }
    );
    return res.data;
  },

  getById: async (tenantId: string, contractorId: string): Promise<ContractorWeb> => {
    const res = await axiosClient.get<ContractorWeb>(
      `/tenants/${tenantId}/contractors/${contractorId}`
    );
    return res.data;
  },

  create: async (tenantId: string, data: CreateContractorRequest): Promise<ContractorWeb> => {
    const res = await axiosClient.post<ContractorWeb>(
      `/tenants/${tenantId}/contractors`,
      data
    );
    return res.data;
  },

  update: async (
    tenantId: string,
    contractorId: string,
    data: UpdateContractorRequest
  ): Promise<ContractorWeb> => {
    const res = await axiosClient.put<ContractorWeb>(
      `/tenants/${tenantId}/contractors/${contractorId}`,
      data
    );
    return res.data;
  },

  delete: async (tenantId: string, contractorId: string): Promise<void> => {
    await axiosClient.delete(`/tenants/${tenantId}/contractors/${contractorId}`);
  },
};
