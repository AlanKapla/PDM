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
  DefaultCostEstimateTemplateListItemWeb,
  CreateCostEstimateTemplateFromDefaultRequest,
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

  // ===== DOMYŚLNE SZABLONY (SYSTEMOWE) =====

  /**
   * Pobiera listę wszystkich dostępnych szablonów domyślnych
   */
  getDefaultTemplates: async (): Promise<DefaultCostEstimateTemplateListItemWeb[]> => {
    const response = await axiosClient.get<DefaultCostEstimateTemplateListItemWeb[]>(
      "/cost-estimate-template/defaults"
    );
    return response.data;
  },

  /**
   * Pobiera pełną strukturę szablonu domyślnego
   * @param slug - Identyfikator szablonu, np. "basic-cost-estimate"
   */
  getDefaultTemplate: async (slug: string): Promise<CostEstimateTemplateStructureWeb> => {
    const response = await axiosClient.get<CostEstimateTemplateStructureWeb>(
      `/cost-estimate-template/defaults/${slug}`
    );
    return response.data;
  },

  /**
   * Tworzy nowy szablon użytkownika z pełną strukturą skopiowaną z domyślnego szablonu
   * Nowe GUIDy pól generowane są po stronie serwera
   * @returns GUID nowego szablonu
   */
  createFromDefault: async (data: CreateCostEstimateTemplateFromDefaultRequest): Promise<string> => {
    const response = await axiosClient.post<string>(
      "/cost-estimate-template/from-default",
      data
    );
    return response.data;
  },

  /**
   * Usuwa szablon (soft delete)
   * Tylko właściciel może usunąć szablon
   * Istniejące kosztorysy korzystające z szablonu nadal działają
   */
  deleteTemplate: async (id: string): Promise<void> => {
    await axiosClient.delete(`/cost-estimate-template/${id}`);
  },

  /**
   * Duplikuje szablon z pełną strukturą (pola, waluty, jednostki)
   * Nowe GUIDy pól generowane są po stronie serwera
   * Tylko właściciel szablonu może go duplikować
   * @param id ID szablonu źródłowego
   * @param data Nazwa i opis nowego szablonu
   * @returns GUID nowego szablonu
   */
  duplicateTemplate: async (id: string, data: DuplicateCostEstimateTemplateRequest): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/cost-estimate-template/${id}/duplicate`,
      data
    );
    return response.data;
  },
};

/**
 * Request model dla duplikowania szablonu kosztorysu
 */
export interface DuplicateCostEstimateTemplateRequest {
  name: string;
  description?: string;
}
