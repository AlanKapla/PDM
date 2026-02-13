import { axiosClient } from "./axiosClient";
import type {
  CostEstimateTemplateStructure,
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

// ===== INTERFACES FOR TEMPLATE STRUCTURE =====

/**
 * Struktura szablonu - bez wersjonowania (refactoring)
 */
export interface CostEstimateTemplateStructureWeb {
  templateId: string;
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
  ownerId: string;
  ownerName: string;
}

export interface CostEstimateTemplateVersionWeb {
  id: string;
  versionNumber: number;
  status: number; // 0=Draft, 1=Approved
  templateStructure: CostEstimateTemplateStructure;
  createdAt: string;
  updatedAt?: string;
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
  structure?: CostEstimateTemplateStructureWeb;
  /** Aktualnie wybrana wersja szablonu (jeśli backend wspiera wersjonowanie) */
  selectedVersion?: CostEstimateTemplateVersionWeb;
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
 * Backend oczekuje: fieldName (Guid), fieldType (enum int), label, isSortable, isFilterable, isVisible
 */
export interface GroupHeaderFieldDto {
  fieldName: string;  // GUID w formacie UUID string (np. "a1b2c3d4-e5f6-...")
  fieldType: number;  // GroupHeaderFieldType enum value (0=GroupName, 1=GroupDescription, etc.)
  label: string;      // Etykieta wyświetlana w UI (customLabel z frontu)
  isSortable: boolean;
  isFilterable: boolean;
  isVisible?: boolean; // Czy pole jest widoczne w UI (domyślnie true)
}

/**
 * FieldDefinitionDto dla System pól
 * Backend oczekuje: fieldName (Guid), fieldType, label, isSortable, isFilterable, isVisible
 */
export interface SystemFieldDto {
  fieldName: string;   // GUID w formacie UUID string (auto-generowany przez frontend)
  fieldType: number;   // SystemFieldType enum value (0=Name, 1=Quantity, 2=Unit)
  label: string;       // Etykieta wyświetlana w UI
  isSortable: boolean;
  isFilterable: boolean;
  isVisible?: boolean; // Czy pole jest widoczne w UI (domyślnie true)
}

/**
 * FieldDefinitionDto dla Calculated pól
 * Backend oczekuje: fieldName (Guid), fieldType, label, isSortable, isFilterable, isVisible
 */
export interface CalculatedFieldDto {
  fieldName: string;   // GUID w formacie UUID string (auto-generowany przez frontend)
  fieldType: number;   // CalculatedFieldType enum value (0=UnitPriceNet, 1=VatRate, etc.)
  label: string;       // Etykieta wyświetlana w UI
  isSortable: boolean;
  isFilterable: boolean;
  isVisible?: boolean; // Czy pole jest widoczne w UI (domyślnie true)
  sumInGroup?: boolean;
  sumInTotal?: boolean;
}

/**
 * FieldDefinitionDto dla Generic pól
 * Backend oczekuje: fieldName (Guid), fieldType, label, isSortable, isFilterable, isVisible
 */
export interface GenericFieldDto {
  fieldName: string;    // GUID w formacie UUID string (auto-generowany przez frontend)
  fieldType: number;    // GenericFieldType enum value (0=Integer, 1=Decimal, 2=String, etc.)
  label: string;        // Etykieta wyświetlana w UI
  isSortable: boolean;
  isFilterable: boolean;
  isVisible?: boolean;  // Czy pole jest widoczne w UI (domyślnie true)
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
   */
  getTemplateDetails: async (id: string): Promise<CostEstimateTemplateDetails> => {
    const response = await axiosClient.get<CostEstimateTemplateDetails>(
      `/cost-estimate-template/${id}`
    );
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
   * Approve a template version
   */
  approveVersion: async (templateId: string, versionId: string): Promise<void> => {
    await axiosClient.post(`/cost-estimate-template/${templateId}/versions/${versionId}/approve`);
  },
};
