import { axiosClient } from "./axiosClient";
import type {
  CostEstimateTemplateStructure,
  CostEstimateTemplateVersionStatus,
  CostEstimateTemplateVersionInfo,
  CostEstimateTemplateVersionHistoryItem,
  ApprovedTemplateVersionItem,
  GroupHeaderFieldDefinition,
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  GroupHeaderFieldWeb,
  SystemFieldWeb,
  CalculatedFieldWeb,
  GenericFieldWeb,
  NestedFieldWeb,
  SummaryConfigurationWeb,
  SummaryFieldWeb,
  UiConfigurationWeb,
  ColumnConfigurationWeb,
} from "../types/costEstimate.types";

// ===== INTERFACES FOR TEMPLATE VERSION STRUCTURE =====

export interface CostEstimateTemplateVersionStructure {
  versionId: string;
  versionNumber: number;
  versionName?: string;
  currencies: CurrencyWeb[];
  units: UnitWeb[];
  groupHeaderFields: GroupHeaderFieldWeb[];
  systemFields: SystemFieldWeb[];
  calculatedFields: CalculatedFieldWeb[];
  genericFields: GenericFieldWeb[];
  summaryConfiguration?: SummaryConfigurationWeb;
  uiConfiguration?: UiConfigurationWeb;
}

export interface CurrencyWeb {
  id: string;
  code: string;
  name: string;
  symbol?: string;
  isDefault: boolean;
  order: number;
}

export interface UnitWeb {
  id: string;
  code: string;
  name: string;
  symbol: string;
  category?: string;
  isDefault: boolean;
  order: number;
}

// ===== END OF TEMPLATE VERSION STRUCTURE INTERFACES =====


export interface CostEstimateTemplateListItem {
  id: string;
  name: string;
  description?: string;
  category?: string;
  createdAt: string;
  updatedAt?: string;
  latestVersionNumber?: number;
  latestVersionStatus?: CostEstimateTemplateVersionStatus;
  ownerId: string;
  ownerName: string;
}

export interface CostEstimateTemplateDetails {
  id: string;
  name: string;
  description?: string;
  category?: string;
  canAddGroups: boolean;
  canBranchGroups: boolean;
  maxGroupLevel?: number;
  autoNumberGroups: boolean;
  groupNumberFormat?: string;
  createdAt: string;
  updatedAt?: string;
  ownerId: string;
  ownerName: string;
  selectedVersion?: CostEstimateTemplateVersionInfo;
  versionStructure?: CostEstimateTemplateVersionStructure; // Pełna struktura wersji z backendu
}

/**
 * Request model dla tworzenia szablonu kosztorysu
 * Tworzy tylko minimalny szablon (nazwa, opis) i pierwszą wersję Draft
 * Cała struktura (pola, waluty, jednostki) jest dodawana przez UpdateCostEstimateTemplate
 */
export interface CreateCostEstimateTemplateRequest {
  name: string;
  description?: string;
}

// ===== DTOs dla update template (zgodne z backendem) =====

/**
 * FieldDefinitionDto dla GroupHeader pól
 * Backend oczekuje: fieldName (Guid), fieldType (enum int), label, isSortable, isFilterable
 */
export interface GroupHeaderFieldDto {
  fieldName: string;  // GUID w formacie UUID string (np. "a1b2c3d4-e5f6-...")
  fieldType: number;  // GroupHeaderFieldType enum value (0=GroupName, 1=GroupDescription, etc.)
  label: string;      // Etykieta wyświetlana w UI (customLabel z frontu)
  isSortable: boolean;
  isFilterable: boolean;
}

/**
 * FieldDefinitionDto dla System pól
 * Backend oczekuje: fieldName (Guid), fieldType, label, isSortable, isFilterable
 */
export interface SystemFieldDto {
  fieldName: string;   // GUID w formacie UUID string (auto-generowany przez frontend)
  fieldType: number;   // SystemFieldType enum value (0=Name, 1=Quantity, 2=Unit)
  label: string;       // Etykieta wyświetlana w UI
  isSortable: boolean;
  isFilterable: boolean;
}

/**
 * FieldDefinitionDto dla Calculated pól
 * Backend oczekuje: fieldName (Guid), fieldType, label, isSortable, isFilterable
 */
export interface CalculatedFieldDto {
  fieldName: string;   // GUID w formacie UUID string (auto-generowany przez frontend)
  fieldType: number;   // CalculatedFieldType enum value (0=UnitPriceNet, 1=VatRate, etc.)
  label: string;       // Etykieta wyświetlana w UI
  isSortable: boolean;
  isFilterable: boolean;
}

/**
 * FieldDefinitionDto dla Generic pól
 * Backend oczekuje: fieldName (Guid), fieldType, label, isSortable, isFilterable
 */
export interface GenericFieldDto {
  fieldName: string;    // GUID w formacie UUID string (auto-generowany przez frontend)
  fieldType: number;    // GenericFieldType enum value (0=Integer, 1=Decimal, 2=String, etc.)
  label: string;        // Etykieta wyświetlana w UI
  isSortable: boolean;
  isFilterable: boolean;
}

export interface NestedFieldsDto {
  calculatedFields?: CalculatedFieldDto[];
  genericFields?: GenericFieldDto[];
  minItems?: number;
  maxItems?: number;
  isSelectableCollection?: boolean;
  enableCalculatedFieldsSummation?: boolean;
  summableCalculatedFields?: string[];  // Tablica GUIDów pól (UUID strings)
  uiConfiguration?: {
    columnLayout?: string[];  // Tablica GUIDów pól (UUID strings)
    columnWidths?: Record<string, string>;  // GUID pola -> szerokość
  };
}

export interface UpdateCostEstimateTemplateRequest {
  templateId: string;
  currentVersionId: string;
  name: string;
  description?: string;
  category?: string;
  canAddGroups: boolean;
  canBranchGroups: boolean;
  maxGroupLevel?: number;
  autoNumberGroups: boolean;
  groupNumberFormat?: string;
  updateStructure: boolean;
  currencies?: Array<{
    code: string;
    name: string;
    symbol?: string;
    isDefault: boolean;
    order: number;
  }>;
  units?: Array<{
    code: string;
    name: string;
    symbol: string;
    category?: string;
    isDefault: boolean;
    order: number;
  }>;
  groupHeaderFields?: GroupHeaderFieldDto[];
  systemFields?: SystemFieldDto[];
  calculatedFields?: CalculatedFieldDto[];
  genericFields?: GenericFieldDto[];
  summaryConfiguration?: {
    groupSummaryFields: string[];  // Tablica GUIDów pół (UUID strings)
    totalSummaryFields: string[];  // Tablica GUIDów pół (UUID strings)
    showGroupSummary: boolean;
    showTotalSummary: boolean;
  };
  uiConfiguration?: {
    columnLayout?: string[];  // Tablica GUIDów pół (UUID strings) - kolejność kolumn
    columnWidths?: Record<string, string>;  // GUID pola -> szerokość (np. "150px")
  };
}

export const costEstimateTemplateApi = {
  /**
   * Get all templates for current user
   */
  getTemplates: async (): Promise<CostEstimateTemplateListItem[]> => {
    const response = await axiosClient.get<CostEstimateTemplateListItem[]>(
      "/cost-estimate-template"
    );
    return response.data;
  },

  /**
   * Get template details by ID
   * @param id - Template ID
   * @param versionId - Optional version ID. If null, returns latest version
   */
  getTemplateDetails: async (id: string, versionId?: string): Promise<CostEstimateTemplateDetails> => {
    const url = versionId 
      ? `/cost-estimate-template/${id}?versionId=${versionId}`
      : `/cost-estimate-template/${id}`;
    
    const response = await axiosClient.get<CostEstimateTemplateDetails>(url);
    return response.data;
  },

  /**
   * Create new template
   */
  createTemplate: async (data: CreateCostEstimateTemplateRequest): Promise<string> => {
    const response = await axiosClient.post<string>("/cost-estimate-template", data);
    return response.data;
  },

  /**
   * Update existing template
   */
  updateTemplate: async (id: string, data: UpdateCostEstimateTemplateRequest): Promise<void> => {
    await axiosClient.put(`/cost-estimate-template/${id}`, data);
  },

  /**
   * Get version history for template
   */
  getTemplateVersionHistory: async (
    id: string
  ): Promise<CostEstimateTemplateVersionHistoryItem[]> => {
    const response = await axiosClient.get<CostEstimateTemplateVersionHistoryItem[]>(
      `/cost-estimate-template/${id}/versions`
    );
    return response.data;
  },

  /**
   * Get approved versions for template (for cost estimate creation)
   */
  getApprovedVersions: async (
    id: string
  ): Promise<ApprovedTemplateVersionItem[]> => {
    const response = await axiosClient.get<ApprovedTemplateVersionItem[]>(
      `/cost-estimate-template/${id}/approved-versions`
    );
    return response.data;
  },

  /**
   * Get all approved versions from all templates (for cost estimate creation)
   */
  getAllApprovedVersions: async (): Promise<ApprovedTemplateVersionItem[]> => {
    const response = await axiosClient.get<ApprovedTemplateVersionItem[]>(
      "/cost-estimate-template/approved-versions"
    );
    return response.data;
  },

  /**
   * Approve specific version of template
   */
  approveVersion: async (templateId: string, versionId: string): Promise<void> => {
    await axiosClient.post(`/cost-estimate-template/${templateId}/versions/${versionId}/approve`);
  },

  /**
   * Delete draft version of template
   */
  deleteVersionDraft: async (templateId: string, versionId: string): Promise<void> => {
    await axiosClient.delete(`/cost-estimate-template/${templateId}/versions/${versionId}`);
  },

  /**
   * Get full template version structure
   */
  getTemplateVersionStructure: async (
    templateId: string,
    versionId: string
  ): Promise<CostEstimateTemplateVersionStructure> => {
    const response = await axiosClient.get<CostEstimateTemplateVersionStructure>(
      `/cost-estimate-template/${templateId}/versions/${versionId}/structure`
    );
    return response.data;
  },
};
