import { axiosClient } from "./axiosClient";
import type {
  CostEstimateListItemWeb,
  CostEstimateDetailsWeb,
  CreateCostEstimateWithDataDto,
  UpdateCostEstimateDto,
  CostEstimateGroupDto,
} from "../types/costEstimate.types.new";

// Import ResourceScope from projectApi
import { ResourceScope } from "./projectApi";

// Helper to convert enum to route string
const resourceScopeToRoute = (scope: ResourceScope): string => {
  switch (scope) {
    case ResourceScope.All:
      return "all";
    case ResourceScope.Mine:
      return "mine";
    case ResourceScope.Shared:
      return "shared";
    default:
      return "mine";
  }
};

export const costEstimateApiNew = {
  /**
   * Get field type configurations (by scope)
   */
  getFieldTypeConfigurations: async (): Promise<Record<number, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>> => {
    const response = await axiosClient.get<Record<number, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>>(
      `/cost-estimate-template/field-type-configurations`
    );
    return response.data;
  },
  /**
   * Get cost estimates list by scope
   */
  getCostEstimatesByScope: async (
    tenantId: string,
    projectId: string,
    scope: ResourceScope
  ): Promise<CostEstimateListItemWeb[]> => {
    const scopeRoute = resourceScopeToRoute(scope);
    const response = await axiosClient.get<CostEstimateListItemWeb[]>(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate/${scopeRoute}`
    );
    return response.data;
  },

  /**
   * Get cost estimate details with full hierarchy
   */
  getCostEstimateDetails: async (
    tenantId: string,
    projectId: string,
    id: string
  ): Promise<CostEstimateDetailsWeb> => {
    const response = await axiosClient.get<CostEstimateDetailsWeb>(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate/details/${id}`
    );
    return response.data;
  },

  /**
   * Create new cost estimate
   * Can create empty cost estimate (rootGroups = null/empty) or with full hierarchy
   */
  createCostEstimate: async (
    tenantId: string,
    projectId: string,
    data: CreateCostEstimateWithDataDto
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate`,
      data
    );
    return response.data;
  },

  /**
   * Create empty cost estimate (shortcut)
   */
  createEmptyCostEstimate: async (
    tenantId: string,
    projectId: string,
    templateId: string,
    selectedCurrencyId: string,
    name: string,
    description?: string
  ): Promise<string> => {
    return costEstimateApiNew.createCostEstimate(tenantId, projectId, {
      templateId,
      selectedCurrencyId,
      name,
      description,
      rootGroups: undefined,  // Empty cost estimate
    });
  },

  /**
   * Create cost estimate with data
   */
  createCostEstimateWithData: async (
    tenantId: string,
    projectId: string,
    templateId: string,
    selectedCurrencyId: string,
    name: string,
    rootGroups: CostEstimateGroupDto[],
    description?: string
  ): Promise<string> => {
    return costEstimateApiNew.createCostEstimate(tenantId, projectId, {
      templateId,
      selectedCurrencyId,
      name,
      description,
      rootGroups,
    });
  },

  /**
   * Update existing cost estimate with full hierarchy
   * Groups/items with id will be updated, without id will be created, missing will be deleted
   */
  updateCostEstimate: async (
    tenantId: string,
    projectId: string,
    id: string,
    data: UpdateCostEstimateDto
  ): Promise<void> => {
    await axiosClient.put(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate/${id}`,
      data
    );
  },

  /**
   * Delete cost estimate (soft delete)
   */
  deleteCostEstimate: async (
    tenantId: string,
    projectId: string,
    id: string
  ): Promise<void> => {
    await axiosClient.delete(
      `/tenants/${tenantId}/project/${projectId}/cost-estimate/${id}`
    );
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

/**
 * Example usage:
 * 
 * // Create empty cost estimate
 * const id = await costEstimateApiNew.createEmptyCostEstimate(
 *   tenantId, projectId, templateId, currencyId, "My Cost Estimate"
 * );
 * 
 * // Get details with hierarchy
 * const details = await costEstimateApiNew.getCostEstimateDetails(tenantId, projectId, id);
 * 
 * // Edit and update
 * const updatedDto = convertDetailsWebToUpdateDto(details);
 * // ... modify updatedDto.rootGroups ...
 * await costEstimateApiNew.updateCostEstimate(tenantId, projectId, id, updatedDto);
 */
