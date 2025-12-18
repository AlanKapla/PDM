import { axiosClient } from "./axiosClient";
import type { CostEstimateTemplateStructure } from "../types/costEstimate.types";

export interface CostEstimateTemplateListItem {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt?: string;
  ownerId: string;
  ownerName: string;
}

export interface CostEstimateTemplateDetails {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt?: string;
  ownerId: string;
  ownerName: string;
  templateStructure: CostEstimateTemplateStructure;
}

export interface CreateCostEstimateTemplateRequest {
  name: string;
  description?: string;
  templateStructure: CostEstimateTemplateStructure;
}

export interface UpdateCostEstimateTemplateRequest {
  templateId: string;
  name: string;
  description?: string;
  templateStructure: CostEstimateTemplateStructure;
}

export const costEstimateTemplateApi = {
  /**
   * Get all templates for current user
   */
  getTemplates: async (): Promise<CostEstimateTemplateListItem[]> => {
    const response = await axiosClient.get<CostEstimateTemplateListItem[]>(
      "/CostEstimateTemplate"
    );
    return response.data;
  },

  /**
   * Get template details by ID
   */
  getTemplateDetails: async (id: string): Promise<CostEstimateTemplateDetails> => {
    const response = await axiosClient.get<CostEstimateTemplateDetails>(
      `/CostEstimateTemplate/${id}`
    );
    return response.data;
  },

  /**
   * Create new template
   */
  createTemplate: async (data: CreateCostEstimateTemplateRequest): Promise<string> => {
    const response = await axiosClient.post<string>("/CostEstimateTemplate", data);
    return response.data;
  },

  /**
   * Update existing template
   */
  updateTemplate: async (id: string, data: UpdateCostEstimateTemplateRequest): Promise<void> => {
    await axiosClient.put(`/CostEstimateTemplate/${id}`, data);
  },

  /**
   * Delete template (soft delete)
   */
  deleteTemplate: async (id: string): Promise<void> => {
    await axiosClient.delete(`/CostEstimateTemplate/${id}`);
  },
};
