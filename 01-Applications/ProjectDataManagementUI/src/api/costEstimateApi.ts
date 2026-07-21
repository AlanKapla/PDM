import axios, { type AxiosError, type AxiosResponse } from "axios";
import { axiosClient } from "./axiosClient";
import type {
  CostEstimateListItemWeb,
  CostEstimateDetailsWeb,
  CreateCostEstimateDto,
  UpdateCostEstimateDto,
  CostEstimateGroupDto,
  AddGroupRequestDto,
  AddItemRequestDto,
  ReorderGroupsRequestDto,
  ReorderItemsRequestDto,
  ReorderItemChildrenRequestDto,
  MoveItemRequestDto,
  CostEstimateAdditionalFieldWeb,
  AdditionalFieldType,
  CostEstimateExportFile,
  CostEstimateExportFormat,
} from "../types/costEstimate.types.new";
import type { ApiExceptionResponse } from "../types/apiError.types";
import { parseContentDispositionFileName } from "../utils/downloadBlob";

// Import ResourceScope from projectApi
import { ResourceScope } from "./projectApi";

const EXPORT_TIMEOUT_MS = 120_000;

/**
 * Gdy responseType=blob, błędy 4xx/5xx przychodzą jako Blob z JSON.
 * Parsujemy je z powrotem do obiektu, żeby handleApiError działał normalnie.
 */
async function rethrowBlobApiError(error: unknown): Promise<never> {
  if (!axios.isAxiosError(error)) {
    throw error;
  }

  const axiosError: AxiosError<unknown> = error;
  const data: unknown = axiosError.response?.data;

  if (data instanceof Blob) {
    try {
      const text: string = await data.text();
      const parsed: unknown = JSON.parse(text);
      if (
        axiosError.response &&
        parsed !== null &&
        typeof parsed === "object" &&
        "error" in parsed
      ) {
        axiosError.response.data = parsed as ApiExceptionResponse;
      }
    } catch {
      // Zostaw oryginalny Blob — handleApiError użyje status HTTP.
    }
  }

  throw axiosError;
}

async function exportCostEstimateFile(
  tenantId: string,
  projectId: string,
  id: string,
  format: CostEstimateExportFormat
): Promise<CostEstimateExportFile> {
  const extension: string = format === "xlsx" ? "xlsx" : "pdf";
  const fallbackFileName: string = `kosztorys_${id}.${extension}`;
  const defaultContentType: string =
    format === "xlsx"
      ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
      : "application/pdf";

  try {
    const response: AxiosResponse<Blob> = await axiosClient.get<Blob>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${id}/export/${format}`,
      {
        responseType: "blob",
        timeout: EXPORT_TIMEOUT_MS,
      }
    );

    const contentDisposition: string | undefined =
      (response.headers["content-disposition"] as string | undefined) ??
      (response.headers["Content-Disposition"] as string | undefined);
    const parsedName: string | null =
      parseContentDispositionFileName(contentDisposition);
    const contentTypeHeader: string | undefined =
      (response.headers["content-type"] as string | undefined) ??
      (response.headers["Content-Type"] as string | undefined);

    return {
      blob: response.data,
      fileName: parsedName ?? fallbackFileName,
      contentType: contentTypeHeader ?? defaultContentType,
    };
  } catch (error: unknown) {
    return rethrowBlobApiError(error);
  }
}

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
   * Create new cost estimate with default schema.
   */
  createCostEstimate: async (
    tenantId: string,
    projectId: string,
    data: CreateCostEstimateDto
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate`,
      data
    );
    return response.data;
  },

  /**
   * Update cost estimate metadata (name and description).
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
   * Reorder child items (options or components) within a parent item
   */
  reorderItemChildren: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    parentItemId: string,
    data: ReorderItemChildrenRequestDto
  ): Promise<void> => {
    await axiosClient.put(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${parentItemId}/children/reorder`,
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
  // EXPORT
  // ============================================================

  /**
   * Eksportuje kosztorys do pliku XLSX (blob + nazwa z Content-Disposition).
   */
  exportXlsx: async (
    tenantId: string,
    projectId: string,
    id: string
  ): Promise<CostEstimateExportFile> => {
    return exportCostEstimateFile(tenantId, projectId, id, "xlsx");
  },

  /**
   * Eksportuje kosztorys do pliku PDF (blob + nazwa z Content-Disposition).
   */
  exportPdf: async (
    tenantId: string,
    projectId: string,
    id: string
  ): Promise<CostEstimateExportFile> => {
    return exportCostEstimateFile(tenantId, projectId, id, "pdf");
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

// ========== ADDITIONAL FIELDS SCHEMA ==========

/**
 * Get all additional field definitions for a cost estimate
 */
export async function getAdditionalFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string
): Promise<CostEstimateAdditionalFieldWeb[]> {
  const { data } = await axiosClient.get<CostEstimateAdditionalFieldWeb[]>(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields`
  );
  return data;
}

/**
 * Add a new additional field definition to a cost estimate schema.
 * @returns Created field definition ID
 */
export async function addAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  data: { name: string; fieldType: AdditionalFieldType; order?: number }
): Promise<string> {
  const { data: id } = await axiosClient.post<string>(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields`,
    data
  );
  return id;
}

/**
 * Update an existing additional field definition
 */
export async function updateAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  fieldId: string,
  data: { name?: string; fieldType?: AdditionalFieldType; order?: number }
): Promise<void> {
  await axiosClient.put(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields/${fieldId}`,
    data
  );
}

/**
 * Delete an additional field definition and all its values
 */
export async function deleteAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  fieldId: string
): Promise<void> {
  await axiosClient.delete(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields/${fieldId}`
  );
}

/**
 * Reorder additional field definitions
 * @param fieldIds - Array of field definition IDs in new order
 */
export async function reorderAdditionalFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  fieldIds: string[]
): Promise<void> {
  await axiosClient.post(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields/reorder`,
    { fieldIds }
  );
}

// ========== ADDITIONAL FIELD VALUES ==========

/**
 * Upsert (create or update) an additional field value on a group.
 * @returns Created/updated field value ID
 */
export async function upsertGroupAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  groupId: string,
  data: {
    additionalFieldId: string;
    stringValue?: string | null;
    decimalValue?: number | null;
    boolValue?: boolean | null;
    dateTimeValue?: string | null;
  }
): Promise<string> {
  const { data: id } = await axiosClient.patch<string>(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups/${groupId}/additional-fields`,
    data
  );
  return id;
}

/**
 * Upsert (create or update) an additional field value on an item.
 * @returns Created/updated field value ID
 */
export async function upsertItemAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  data: {
    additionalFieldId: string;
    stringValue?: string | null;
    decimalValue?: number | null;
    boolValue?: boolean | null;
    dateTimeValue?: string | null;
  }
): Promise<string> {
  const { data: id } = await axiosClient.patch<string>(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/additional-fields`,
    data
  );
  return id;
}

// ========== BASE FIELD UPDATES ==========

/**
 * Update base fields of an item (name, quantity, unit, pricing and derived values).
 * Triggers server-side recalculation when a financial field changes.
 */
export async function updateItemBaseFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  data: {
    name?: string;
    quantity?: number | null;
    unit?: string | null;
    unitPriceNet?: number | null;
    vatRate?: number | null;
    netValue?: number | null;
    grossValue?: number | null;
    vatValue?: number | null;
    unitPriceGross?: number | null;
    clearName?: boolean;
    clearQuantity?: boolean;
    clearUnit?: boolean;
    clearUnitPriceNet?: boolean;
    clearVatRate?: boolean;
    clearNetValue?: boolean;
    clearGrossValue?: boolean;
    clearVatValue?: boolean;
    clearUnitPriceGross?: boolean;
    isSelected?: boolean | null;
    isStageWork?: boolean | null;
  }
): Promise<void> {
  await axiosClient.patch(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}`,
    data
  );
}

/**
 * Update base fields of a group (name).
 */
export async function updateGroupBaseFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  groupId: string,
  data: { name?: string; clearName?: boolean }
): Promise<void> {
  await axiosClient.patch(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups/${groupId}`,
    data
  );
}

/**
 * Set the isSelected flag on an item (for option items).
 */
export async function setItemIsSelected(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  isSelected: boolean
): Promise<void> {
  await axiosClient.patch(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/select`,
    { isSelected }
  );
}

// ========== FILE OPERATIONS ==========

/**
 * Upload files to an item (append strategy).
 * @param files Array of files to upload (PDF/JPG, max 50 MB each, max 10 files)
 * @returns Array of created file IDs
 */
export async function uploadItemFiles(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  files: File[]
): Promise<string[]> {
  const formData = new FormData();
  files.forEach((file) => formData.append('files', file));
  const { data: ids } = await axiosClient.post<string[]>(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/item-files`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );
  return ids;
}

/**
 * Delete a single file from an item.
 */
export async function deleteItemFile(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  fileId: string
): Promise<void> {
  await axiosClient.delete(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/item-files/${fileId}`
  );
}

/**
 * Replace all files on an item (replace strategy — removes existing files).
 * Sending empty files array removes all files.
 * @returns Array of created file IDs
 */
export async function replaceItemFiles(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  files: File[]
): Promise<string[]> {
  const formData = new FormData();
  files.forEach((file) => formData.append('files', file));
  const { data: ids } = await axiosClient.put<string[]>(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/item-files`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );
  return ids;
}
