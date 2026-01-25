// Existing types from costEstimate.types.ts
export interface CostEstimateTemplate {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  itemsCount: number;
}

import type { CostEstimateTemplateVersionStructure } from './costEstimate.types';

export enum CostEstimateStatus {
  Draft = 0,
  InProgress = 1,
  ReadyForReview = 2,
  Approved = 3,
  Rejected = 4,
  Archived = 5,
}

export enum CostEstimateTemplateVersionStatus {
  Draft = 0,
  Approved = 1,
}

// ========== FIELD TYPE CONFIGURATION (NEW) ==========

export interface CostEstimateFieldTypeConfigWeb {
  fieldType: number;        // FieldType enum as int
  fieldScope: number;       // FieldScope enum as int
  namePl: string;           // Localized name (PL)
  valueTypeName: string;    // e.g., "Integer", "Decimal", "String", "Date", "Boolean"
  isNumeric: boolean;
  isText: boolean;
  isDate: boolean;
  isBoolean: boolean;
  isCollection: boolean;    // Option/collection type
}

export interface FieldDefinitionWeb {
  id: string;
  fieldName: string;
  label: string;
  isSortable: boolean;
  isFilterable: boolean;
  fieldTypeConfig: CostEstimateFieldTypeConfigWeb;
  childFields?: FieldDefinitionWeb[] | null;
}

// ========== NEW MUTATION DTOs ==========

/**
 * DTO dla tworzenia/edycji wartości pola grupy
 */
export interface CostEstimateGroupFieldValueDto {
  fieldDefinitionId: string;
  value?: string;
}

/**
 * DTO dla tworzenia/edycji wartości pola pozycji
 * Używa pojedynczego FieldDefinitionId wskazującego na CostEstimateTemplateFieldDefinitionBase
 */
export interface CostEstimateFieldValueDto {
  fieldDefinitionId: string;
  value?: string;
}

/**
 * DTO dla tworzenia/edycji pozycji kosztorysu
 * Może zawierać kolekcję Options jeśli ma pole ItemSystemOptions
 */
export interface CostEstimateItemDto {
  id?: string;  // null/undefined dla nowych pozycji
  parentItemId?: string;  // ID pozycji nadrzędnej (jeśli to opcja)
  order: number;
  fieldValues: CostEstimateFieldValueDto[];
  options?: CostEstimateItemDto[];  // Kolekcja opcji - max 1 poziom zagnieżdżenia!
}

/**
 * DTO dla tworzenia/edycji grupy kosztorysu (rekurencyjna struktura)
 */
export interface CostEstimateGroupDto {
  id?: string;  // null/undefined dla nowych grup
  parentGroupId?: string;
  level: number;
  order: number;
  fieldValues: CostEstimateGroupFieldValueDto[];
  items: CostEstimateItemDto[];
  childGroups: CostEstimateGroupDto[];
}

/**
 * DTO dla tworzenia kosztorysu z pełną strukturą
 */
export interface CreateCostEstimateWithDataDto {
  templateId: string;
  templateVersionId: string;
  selectedCurrencyId: string;  // Wybrana waluta z dostępnych w template
  name: string;
  description?: string;
  rootGroups?: CostEstimateGroupDto[];  // null lub pusta = pusty kosztorys
}

/**
 * DTO dla aktualizacji kosztorysu z pełną strukturą
 */
export interface UpdateCostEstimateDto {
  name: string;
  description?: string;
  status: CostEstimateStatus;
  rootGroups: CostEstimateGroupDto[];
}

// ========== RESPONSE DTOs ==========

/**
 * Wartość pola grupy kosztorysu (z serwera)
 */
export interface CostEstimateGroupFieldValueWeb {
  id: string;
  fieldDefinitionId: string;
  fieldType: number;      // FieldType enum jako int (kompatybilność JSON)
  fieldScope: number;     // FieldScope enum jako int (zawsze Group dla grupy)
  fieldLabel?: string;
  value?: string;
}

/**
 * Wartość pola pozycji kosztorysu (z serwera)
 * Używa pojedynczego FieldDefinitionId wskazującego na CostEstimateTemplateFieldDefinitionBase
 */
export interface CostEstimateItemFieldValueWeb {
  id: string;
  fieldDefinitionId: string;
  fieldType: number;      // FieldType enum jako int (kompatybilność JSON)
  fieldScope: number;     // FieldScope enum jako int (ItemSystem/ItemCalculated/ItemGeneric)
  fieldName?: string;
  fieldLabel?: string;
  value?: string;
}

/**
 * Pozycja kosztorysu (z serwera)
 * Może zawierać kolekcję Options jeśli ma pole ItemSystemOptions
 */
export interface CostEstimateItemWeb {
  id: string;
  groupId: string;
  parentItemId?: string;  // ID pozycji nadrzędnej (jeśli to opcja)
  order: number;
  fieldValues: CostEstimateItemFieldValueWeb[];
  options?: CostEstimateItemWeb[];  // Kolekcja opcji (zagnieżdżonych pozycji)
  createdAt: string;
  updatedAt?: string;
}

/**
 * Grupa kosztorysu (z serwera) - rekurencyjna struktura
 */
export interface CostEstimateGroupWeb {
  id: string;
  parentGroupId?: string;
  level: number;
  order: number;
  fieldValues: CostEstimateGroupFieldValueWeb[];
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  lastCalculatedAt?: string;
  childGroups: CostEstimateGroupWeb[];
  items: CostEstimateItemWeb[];
  createdAt: string;
  updatedAt?: string;
}

/**
 * Szczegóły kosztorysu z pełną hierarchią
 */
export interface CostEstimateDetailsWeb {
  id: string;
  tenantId: string;
  projectId: string;
  templateId: string;
  templateName: string;
  templateVersionId: string;
  templateVersionNumber: number;
  selectedCurrencyId: string;
  selectedCurrencyCode: string;
  selectedCurrencySymbol?: string;
  name: string;
  description?: string;
  status: CostEstimateStatus;
  rootGroups: CostEstimateGroupWeb[];
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  createdAt: string;
  updatedAt?: string;
  lastCalculatedAt?: string;
  ownerId: string;
  ownerName: string;
  templateStructure: CostEstimateTemplateVersionStructure;
}

/**
 * Element listy kosztorysów
 */
export interface CostEstimateListItemWeb {
  id: string;
  tenantId: string;
  projectId: string;
  templateId: string;
  templateName: string;
  name: string;
  description?: string;
  status: CostEstimateStatus;
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  createdAt: string;
  updatedAt?: string;
  ownerId: string;
  ownerName: string;
}

// ========== HELPER FUNCTIONS ==========

/**
 * Sprawdza czy ID jest tymczasowe (nowy element)
 */
export function isTemporaryId(id?: string): boolean {
  return !id || id.startsWith('temp_');
}

/**
 * Konwertuje pozycję z serwera na DTO dla edycji
 */
export function convertItemWebToDto(item: CostEstimateItemWeb): CostEstimateItemDto {
  return {
    id: isTemporaryId(item.id) ? undefined : item.id,
    // Jeśli parentItemId jest tymczasowe (nowy parent), wysyłamy null - backend sam ustawi relację
    parentItemId: isTemporaryId(item.parentItemId) ? undefined : item.parentItemId,
    order: item.order,
    fieldValues: item.fieldValues.map(fv => ({
      fieldDefinitionId: fv.fieldDefinitionId,
      value: fv.value
    })),
    options: item.options?.map(convertItemWebToDto)
  };
}

/**
 * Konwertuje grupę z serwera na DTO dla edycji
 */
export function convertGroupWebToDto(group: CostEstimateGroupWeb): CostEstimateGroupDto {
  return {
    id: isTemporaryId(group.id) ? undefined : group.id,
    parentGroupId: group.parentGroupId,
    level: group.level,
    order: group.order,
    fieldValues: group.fieldValues.map(fv => ({
      fieldDefinitionId: fv.fieldDefinitionId,
      value: fv.value
    })),
    items: (group.items || []).map(convertItemWebToDto),
    childGroups: (group.childGroups || []).map(convertGroupWebToDto)
  };
}

/**
 * Konwertuje szczegóły kosztorysu na DTO dla edycji
 */
export function convertDetailsWebToUpdateDto(details: CostEstimateDetailsWeb): UpdateCostEstimateDto {
  return {
    name: details.name,
    description: details.description,
    status: details.status,
    rootGroups: details.rootGroups.map(convertGroupWebToDto)
  };
}

/**
 * Tworzy pustą grupę dla nowego kosztorysu
 */
export function createEmptyGroup(
  level: number = 0,
  order: number = 0,
  parentGroupId?: string
): CostEstimateGroupDto {
  return {
    id: undefined,
    parentGroupId,
    level,
    order,
    fieldValues: [],
    items: [],
    childGroups: []
  };
}

/**
 * Tworzy pustą pozycję dla grupy
 */
export function createEmptyItem(order: number = 0, parentItemId?: string): CostEstimateItemDto {
  return {
    id: undefined,
    parentItemId,
    order,
    fieldValues: [],
    options: undefined
  };
}

// ========== EXISTING TYPES (for compatibility) ==========
// Keep existing types from the original file for backward compatibility

export interface CostEstimateTemplateVersionInfo {
  id: string;
  versionNumber: number;
  status: CostEstimateTemplateVersionStatus;
  templateStructure: CostEstimateTemplateStructure;
  createdAt: string;
  approvedAt?: string;
}

export interface CostEstimateTemplateVersionHistoryItem {
  id: string;
  templateId: string;
  versionNumber: number;
  status: CostEstimateTemplateVersionStatus;
  createdAt: string;
  approvedAt?: string;
}

export interface ApprovedTemplateVersionItem {
  versionId: string;
  templateId: string;
  templateName: string;
  templateCurrency?: string;
  versionNumber: number;
  templateStructure: CostEstimateTemplateStructure;
  approvedAt: string;
  approvedByUserName?: string;
}

export enum CalculatedFieldType {
  UnitPriceNet = 0,
  VatRate = 1,
  UnitPriceGross = 2,
  ValueNet = 3,
  ValueGross = 4,
  UnitVat = 5,
  TotalVat = 6,
}

export enum GenericFieldType {
  Integer = 0,
  Decimal = 1,
  String = 2,
  Boolean = 3,
  Date = 4,
  DateTime = 5,
}

export enum GroupHeaderFieldType {
  GroupName = 0,
  GroupDescription = 1,
  GroupNumber = 2,
  StartDate = 3,
  EndDate = 4,
  Status = 5,
  Notes = 6,
  Responsible = 7,
  Budget = 8,
  Priority = 9,
}

export interface CostEstimateTemplateStructure {
  canAddGroups: boolean;
  canBranchGroups: boolean;
  maxGroupLevel?: number;
  groupDefinition: Record<string, unknown>;  // simplified - use Record instead of any
  workScopeFieldsDefinition: Record<string, unknown>;  // simplified
  summaryConfiguration?: Record<string, unknown>;
  uiConfiguration?: Record<string, unknown>;
}
