import { axiosClient } from "./axiosClient";
import type {
  CostEstimateListItemWeb,
  CostEstimateDetailsWeb,
  CreateCostEstimateWithDataDto,
  UpdateCostEstimateDto,
  CostEstimateGroupDto,
  AddGroupRequestDto,
  AddItemRequestDto,
  ReorderGroupsRequestDto,
  ReorderItemsRequestDto,
  MoveItemRequestDto,
  UpsertFieldValueRequestDto,
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

export const costEstimateApi = {
  /**
   * Get field type configurations
   */
  getFieldTypeConfigurations: async (): Promise<Record<string, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>> => {
    const response = await axiosClient.get<Record<string, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>>(
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
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${scopeRoute}`
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
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/details/${id}`
    );
    return response.data;
  },

  /**
   * Create new cost estimate.
   * Can create empty cost estimate (rootGroups = null/empty) or with full hierarchy.
   */
  createCostEstimate: async (
    tenantId: string,
    projectId: string,
    data: CreateCostEstimateWithDataDto
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate`,
      data
    );
    return response.data;
  },

  /**
   * Create empty cost estimate (shortcut).
   * Waluta jest brana z ProjectCurrency projektu (backend).
   */
  createEmptyCostEstimate: async (
    tenantId: string,
    projectId: string,
    templateId: string,
    name: string,
    description?: string
  ): Promise<string> => {
    return costEstimateApi.createCostEstimate(tenantId, projectId, {
      templateId,
      name,
      description,
      rootGroups: undefined,
    });
  },

  /**
   * Create cost estimate with data.
   * Waluta jest brana z ProjectCurrency projektu (backend).
   */
  createCostEstimateWithData: async (
    tenantId: string,
    projectId: string,
    templateId: string,
    name: string,
    rootGroups: CostEstimateGroupDto[],
    description?: string
  ): Promise<string> => {
    return costEstimateApi.createCostEstimate(tenantId, projectId, {
      templateId,
      name,
      description,
      rootGroups,
    });
  },

  /**
   * Update existing cost estimate with full hierarchy.
   * Groups/items with id will be updated, without id will be created, missing will be deleted.
   */
  updateCostEstimate: async (
    tenantId: string,
    projectId: string,
    id: string,
    data: UpdateCostEstimateDto
  ): Promise<void> => {
    await axiosClient.put(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${id}`,
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
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${id}`
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
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${id}/copy`,
      { targetProjectIds }
    );
    return response.data;
  },

  // ============================================================
  // GROUP OPERATIONS
  // ============================================================

  /**
   * Add a new group to a cost estimate.
   * @returns Created group ID
   */
  addGroup: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    data: AddGroupRequestDto
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups`,
      data
    );
    return response.data;
  },

  /**
   * Delete a group from cost estimate
   */
  deleteGroup: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    groupId: string
  ): Promise<void> => {
    await axiosClient.delete(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups/${groupId}`
    );
  },

  /**
   * Update a group field value.
   * Does NOT trigger automatic recalculation — call recalculate() separately if needed.
   */
  upsertGroupField: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    groupId: string,
    data: UpsertFieldValueRequestDto
  ): Promise<string> => {
    const response = await axiosClient.patch<string>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups/${groupId}/fields`,
      data
    );
    return response.data;
  },

  /**
   * Reorder groups within cost estimate.
   * Supports moving groups between different parents via parentGroupId.
   */
  reorderGroups: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    data: ReorderGroupsRequestDto
  ): Promise<void> => {
    await axiosClient.put(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups/reorder`,
      data
    );
  },

  // ============================================================
  // ITEM OPERATIONS
  // ============================================================

  /**
   * Add a new item to a group in cost estimate.
   * @returns Created item ID
   */
  addItem: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    data: AddItemRequestDto
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items`,
      data
    );
    return response.data;
  },

  /**
   * Delete an item from cost estimate
   */
  deleteItem: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    itemId: string
  ): Promise<void> => {
    await axiosClient.delete(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}`
    );
  },

  /**
   * Update an item field value.
   * Does NOT trigger automatic recalculation — call recalculate() separately if needed.
   */
  upsertItemField: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    itemId: string,
    data: UpsertFieldValueRequestDto
  ): Promise<string> => {
    const response = await axiosClient.patch<string>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/fields`,
      data
    );
    return response.data;
  },

  /**
   * Reorder items within a group
   */
  reorderItems: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    groupId: string,
    data: ReorderItemsRequestDto
  ): Promise<void> => {
    await axiosClient.put(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups/${groupId}/items/reorder`,
      data
    );
  },

  /**
   * Move an item to a different group.
   * Options and components are moved together with the parent item.
   * Only main items (RelationType=None) can be moved.
   */
  moveItem: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    itemId: string,
    data: MoveItemRequestDto
  ): Promise<void> => {
    await axiosClient.patch(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/move`,
      data
    );
  },

  // ============================================================
  // SHARING
  // ============================================================

  /**
   * Dodaje uzytkownikow do udostepnienia kosztorysu (POST).
   * Nie usuwa istniejacych — tylko dodaje nowych.
   * Policy: ProjectResourcesShare
   */
  shareCostEstimate: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    userIds: string[]
  ): Promise<void> => {
    await axiosClient.post(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/shares`,
      { userIds }
    );
  },

  /**
   * Zastepuje pelna liste udostepnien kosztorysu (PUT).
   * Uzytkownicy spoza listy traca dostep.
   * Policy: ProjectResourcesShare
   */
  updateCostEstimateShares: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    userIds: string[]
  ): Promise<void> => {
    await axiosClient.put(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/shares`,
      { userIds }
    );
  },

  // ============================================================
  // CALCULATION
  // ============================================================

  /**
   * Recalculate all calculated fields in cost estimate.
   * Must be called explicitly after modifying field values that affect calculations.
   */
  recalculate: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string
  ): Promise<void> => {
    await axiosClient.post(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/recalculate`
    );
  },

  // ============================================================
  // FILE OPERATIONS
  // ============================================================

  /**
   * Upload files to a cost estimate field of type ItemSystemFiles (fieldType = 105).
   * Replace All strategy — sending empty files array removes all existing files.
   * Backend automatically creates FieldValue if it does not exist.
   *
   * @param files Array of files to upload (PDF/JPG, max 50 MB each, max 10 files)
   * @returns Array of created file IDs (empty array if files were cleared)
   */
  uploadCostEstimateItemFiles: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    itemId: string,
    fieldDefinitionId: string,
    files: File[]
  ): Promise<string[]> => {
    const formData = new FormData();
    formData.append('fieldDefinitionId', fieldDefinitionId);
    files.forEach(file => formData.append('files', file));

    const response = await axiosClient.post<string[]>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/files`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response.data;
  },

  // ============================================================
  // AI GENERATION
  // ============================================================

  /**
   * Generuje podgląd kosztorysu przez AI.
   * NIE zapisuje do bazy danych — zwraca podgląd do zatwierdzenia.
   */
  generateAIPreview: async (
    tenantId: string,
    projectId: string,
    request: import('../types/costEstimate.types.new').AICostEstimateRequestDto
  ): Promise<import('../types/costEstimate.types.new').AICostEstimatePreviewDto> => {
    const response = await axiosClient.post<import('../types/costEstimate.types.new').AICostEstimatePreviewDto>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/generate-ai-preview`,
      request
    );
    return response.data;
  },

  /**
   * Zapisuje kosztorys zatwierdzony przez użytkownika z podglądu AI.
   * Zwraca ID nowo utworzonego kosztorysu.
   */
  createFromAIPreview: async (
    tenantId: string,
    projectId: string,
    body: import('../types/costEstimate.types.new').CreateCostEstimateFromAIPreviewDto
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/create-from-ai-preview`,
      body
    );
    return response.data;
  },
};
