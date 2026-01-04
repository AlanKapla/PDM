import { axiosClient } from "./axiosClient";
import type {
  CostEstimateListItem,
  CostEstimateDetails,
  CostEstimateDataModel,
  CostEstimateStatus,
} from "../types/costEstimate.types";

export interface CreateCostEstimateRequest {
  templateId: string;
  name: string;
  description?: string;
}

export interface UpdateCostEstimateRequest {
  name: string;
  description?: string;
  status: CostEstimateStatus;
  data: CostEstimateDataModel;
  totalNet?: number;
  totalGross?: number;
}

export const costEstimateApi = {
  /**
   * Get all cost estimates for project
   */
  getCostEstimates: async (tenantId: string, projectId: string): Promise<CostEstimateListItem[]> => {
    const response = await axiosClient.get<CostEstimateListItem[]>(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate`
    );
    return response.data;
  },

  /**
   * Get cost estimate details by ID
   */
  getCostEstimateDetails: async (
    tenantId: string,
    projectId: string,
    id: string
  ): Promise<CostEstimateDetails> => {
    const response = await axiosClient.get<CostEstimateDetails>(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate/${id}`
    );
    return response.data;
  },

  /**
   * Create new cost estimate based on template
   */
  createCostEstimate: async (
    tenantId: string,
    projectId: string,
    data: CreateCostEstimateRequest
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate`,
      data
    );
    return response.data;
  },

  /**
   * Update existing cost estimate
   */
  updateCostEstimate: async (
    tenantId: string,
    projectId: string,
    id: string,
    data: UpdateCostEstimateRequest
  ): Promise<void> => {
    await axiosClient.put(`/tenants/${tenantId}/project/${projectId}/cost-estimate/${id}`, data);
  },

  /**
   * Delete cost estimate (soft delete)
   */
  deleteCostEstimate: async (tenantId: string, projectId: string, id: string): Promise<void> => {
    await axiosClient.delete(`/tenants/${tenantId}/project/${projectId}/cost-estimate/${id}`);
  },

  /**
   * Copy cost estimate to other projects
   */
  copyCostEstimate: async (
    tenantId: string,
    projectId: string,
    id: string,
    targetProjectIds: string[]
  ): Promise<string[]> => {
    const response = await axiosClient.post<string[]>(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate/${id}/copy`,
      { targetProjectIds }
    );
    return response.data;
  },
};
