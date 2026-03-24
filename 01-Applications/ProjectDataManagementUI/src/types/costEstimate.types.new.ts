import type { CostEstimateTemplateStructureWeb } from './costEstimate.types';

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

export enum CostEstimateStatus {
  Draft = 0,
  InProgress = 1,
  ReadyForReview = 2,
  Approved = 3,
  Rejected = 4,
  Archived = 5,
}

// ========== FIELD TYPE CONFIGURATION (NEW) ==========

export interface CostEstimateFieldTypeConfigWeb {
  fieldType: number;        // FieldType enum as int
  fieldScope: number;       // FieldScope enum as int
  namePl: string;           // Localized name (PL)
  valueTypeName: string;    // e.g., "Integer", "Decimal", "String", "Date", "Boolean", "file"
  isNumeric: boolean;
  isText: boolean;
  isDate: boolean;
  isBoolean: boolean;
  isCollection: boolean;    // Option/collection type
  isFile?: boolean;         // File upload field (ItemSystemFiles = 105) - default false
}

export interface FieldDefinitionWeb {
  id: string;
  fieldName: string;
  label: string;
  isSortable: boolean;
  isFilterable: boolean;
  /** Pole oznaczone jako tylko do odczytu — Restricted users nie mogą go edytować */
  isReadOnly?: boolean;
  fieldTypeConfig: CostEstimateFieldTypeConfigWeb;
  childFields?: FieldDefinitionWeb[] | null;
}

// ========== NEW MUTATION DTOs ==========

/**
 * DTO dla tworzenia/edycji wartości pola (wspólny dla grup i pozycji)
 * Używa pojedynczego FieldDefinitionId wskazującego na definicję pola w szablonie
 * Wartość zapisywana w odpowiednim polu typowanym w zależności od FieldType
 */
export interface CostEstimateFieldValueDto {
  fieldDefinitionId: string;
  stringValue?: string;
  decimalValue?: number;
  boolValue?: boolean;
  dateTimeValue?: string;  // ISO 8601 format
  /** Alias dla stringValue — używany w edytorze jako generyczna wartość tekstowa */
  value?: string;
}

// Alias dla kompatybilności - grupy używają tego samego DTO
export type CostEstimateGroupFieldValueDto = CostEstimateFieldValueDto;

/**
 * Poziom dostępu do kosztorysu
 * Full=3 (owner/admin), Restricted=2 (shared user), None=0 (brak dostępu)
 */
export enum CostEstimateAccessLevel {
  None = 0,
  // ReadOnly = 1 — martwy kod, nigdy nie przyznawany
  Restricted = 2,
  Full = 3,
}

/**
 * Wpis udostępnienia kosztorysu użytkownikowi
 */
export interface CostEstimateShareWeb {
  userId: string;
  fullName: string;
  email: string;
  sharedAt: string; // ISO 8601
}

/**
 * Typ relacji pozycji do pozycji nadrzędnej
 */
export enum ItemRelationType {
  None = 0,
  Option = 1,
  Component = 2,
}

/**
 * DTO dla tworzenia/edycji pozycji kosztorysu
 * Może zawierać kolekcję Options jeśli ma pole ItemSystemOptions
 * Może zawierać kolekcję Components - wtedy NIE MOŻE mieć FieldValues!
 */
export interface CostEstimateItemDto {
  id?: string;  // null/undefined dla nowych pozycji
  parentItemId?: string;  // ID pozycji nadrzędnej (jeśli to opcja lub komponent)
  relationType: number;   // ItemRelationType: None=0, Option=1, Component=2
  order: number;
  fieldValues: CostEstimateFieldValueDto[];
  options?: CostEstimateItemDto[];     // Kolekcja opcji - max 1 poziom zagnieżdżenia!
  components?: CostEstimateItemDto[];  // Kolekcja komponentów - jeśli są, FieldValues musi być puste!
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
 * Plik dołączony do pola kosztorysu typu ItemSystemFiles (fieldType = 105)
 */
export interface CostEstimateFieldFileWeb {
  id: string;                     // GUID pliku
  originalFileName: string;       // Oryginalna nazwa pliku (np. "faktura.pdf")
  contentType: string;            // MIME type ("application/pdf" | "image/jpeg")
  fileSize: number;               // Rozmiar w bajtach
  order: number;                  // Kolejność w kolekcji
  sasUriPreview: string | null;   // SAS URI do podglądu (inline) — otwórz w nowej karcie / <img> / <iframe>
  sasUriDownload: string | null;  // SAS URI do pobrania (attachment) — wymusza download
  createdAt: string;              // ISO 8601 datetime
}

/**
 * Wartość pola w kosztorysie (wspólna dla grup i pozycji)
 * Wartość zwracana w odpowiednim polu typowanym w zależności od FieldType
 */
export interface CostEstimateFieldValueWeb {
  id: string;
  fieldDefinitionId: string;
  fieldType: number;      // FieldType enum jako int (kompatybilność JSON)
  fieldScope: number;     // FieldScope enum jako int (Group/ItemSystem/ItemCalculated/ItemGeneric)
  fieldName?: string;     // GUID pola (dla pozycji) - opcjonalny
  fieldLabel?: string;
  stringValue?: string;
  decimalValue?: number;
  boolValue?: boolean;
  dateTimeValue?: string; // ISO 8601 format
  files?: CostEstimateFieldFileWeb[] | null;  // Pliki - tylko dla fieldType === 105 (ItemSystemFiles)
}

// Aliasy dla kompatybilności wstecznej
export type CostEstimateGroupFieldValueWeb = CostEstimateFieldValueWeb;
export type CostEstimateItemFieldValueWeb = CostEstimateFieldValueWeb;

/**
 * Pozycja kosztorysu (z serwera)
 * Może zawierać kolekcję Options jeśli ma pole ItemSystemOptions
 * Może zawierać kolekcję Components - pozycja składa się z komponentów
 */
export interface CostEstimateItemWeb {
  id: string;
  groupId: string;
  parentItemId?: string;     // ID pozycji nadrzędnej (jeśli to opcja lub komponent)
  relationType?: number;     // ItemRelationType: None=0, Option=1, Component=2
  order: number;
  netValue?: number;         // Obliczona wartość netto (z komponentów lub pól)
  grossValue?: number;       // Obliczona wartość brutto
  vatValue?: number;         // Obliczona wartość VAT
  fieldValues: CostEstimateFieldValueWeb[];
  options?: CostEstimateItemWeb[];      // Kolekcja opcji (zagnieżdżonych pozycji)
  components?: CostEstimateItemWeb[];   // Kolekcja komponentów (składników pozycji)
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
  fieldValues: CostEstimateFieldValueWeb[];
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
  templateVersionNumber?: number;
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
  templateStructure: CostEstimateTemplateStructureWeb;
  /** Poziom dostępu bieżącego użytkownika do kosztorysu */
  accessLevel: CostEstimateAccessLevel;
  /** Lista userów, którym kosztorys jest udostępniony */
  sharedWithUsers: CostEstimateShareWeb[];
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
  /** Czy kosztorys jest udostępniony bieżącemu userowi przez innego */
  isSharedWithMe: boolean;
  /** Czy bieżący user udostępnił ten kosztorys innym */
  isSharedByMe: boolean;
  /** Lista userów, którym kosztorys jest udostępniony (widoczna dla ownera/admina) */
  sharedWithUsers: CostEstimateShareWeb[];
}

// ========== HELPER FUNCTIONS ==========

/**
 * Sprawdza czy ID jest tymczasowe (nowy element)
 */
export function isTemporaryId(id?: string): boolean {
  return !id || id.startsWith('temp_');
}

// ========== OPERATION RESULT DTOs ==========

/**
 * Wynik dodania grupy — zwracany z POST /details/{id}/groups
 */
export interface AddCostEstimateGroupResultWeb {
  groupId: string;
  fieldValues: CostEstimateFieldValueWeb[];
}

/**
 * Wynik dodania pozycji — zwracany z POST /details/{id}/items
 */
export interface AddCostEstimateItemResultWeb {
  itemId: string;
  fieldValues: CostEstimateFieldValueWeb[];
}

/**
 * DTO dla zmiany kolejności grup — obsługuje też przenoszenie między parentami
 */
export interface ReorderGroupDto {
  groupId: string;
  parentGroupId: string | null;  // null = root level, guid = podgrupa
  order: number;
}

/**
 * DTO dla zmiany kolejności pozycji w grupie
 */
export interface ReorderItemDto {
  itemId: string;
  order: number;
}

/**
 * Request body dla reorder grup
 */
export interface ReorderGroupsRequestDto {
  costEstimateId: string;
  groups: ReorderGroupDto[];
}

/**
 * Request body dla reorder pozycji
 */
export interface ReorderItemsRequestDto {
  costEstimateId: string;
  items: ReorderItemDto[];
}

/**
 * Request body dla dodania grupy — costEstimateId idzie z route URL
 */
export interface AddGroupRequestDto {
  parentGroupId: string | null;
  order: number;
}

/**
 * Request body dla dodania pozycji — costEstimateId idzie z route URL
 */
export interface AddItemRequestDto {
  groupId: string;
  order: number;
  relationType: number;  // ItemRelationType: None=0, Option=1, Component=2
  parentItemId?: string; // Dla opcji/komponentów
}

/**
 * Request body dla przenoszenia pozycji między grupami
 * PATCH /{id}/items/{itemId}/move
 */
export interface MoveItemRequestDto {
  costEstimateId: string;
  itemId: string;
  targetGroupId: string;
}

/**
 * Request body dla upsert pola (autosave) — PATCH /groups/{groupId}/fields lub /items/{itemId}/fields
 * fieldValueId=null → tworzenie nowej wartości (wymagane fieldDefinitionId)
 * fieldValueId=guid → aktualizacja istniejącej wartości
 */
export interface UpsertFieldValueRequestDto {
  fieldValueId: string | null;
  fieldDefinitionId: string | null;
  stringValue?: string | null;
  decimalValue?: number | null;
  boolValue?: boolean | null;
  dateTimeValue?: string | null;
}

/** @deprecated Użyj UpsertFieldValueRequestDto */
export type UpdateFieldValueRequestDto = UpsertFieldValueRequestDto;

/**
 * Pobiera wartość z typowanego pola CostEstimateFieldValueWeb jako string
 * Używane do wyświetlania i edycji
 */
export function getFieldValueAsString(fieldValue: CostEstimateFieldValueWeb | undefined): string | undefined {
  if (!fieldValue) return undefined;
  
  if (fieldValue.stringValue !== undefined && fieldValue.stringValue !== null) {
    return fieldValue.stringValue;
  }
  if (fieldValue.decimalValue !== undefined && fieldValue.decimalValue !== null) {
    return String(fieldValue.decimalValue);
  }
  if (fieldValue.boolValue !== undefined && fieldValue.boolValue !== null) {
    return String(fieldValue.boolValue);
  }
  if (fieldValue.dateTimeValue !== undefined && fieldValue.dateTimeValue !== null) {
    return fieldValue.dateTimeValue;
  }
  
  return undefined;
}

/**
 * Pobiera wartość z typowanego pola jako number
 */
export function getFieldValueAsNumber(fieldValue: CostEstimateFieldValueWeb | undefined): number {
  if (!fieldValue) return 0;
  
  if (fieldValue.decimalValue !== undefined && fieldValue.decimalValue !== null) {
    return fieldValue.decimalValue;
  }
  if (fieldValue.stringValue !== undefined && fieldValue.stringValue !== null) {
    return parseFloat(fieldValue.stringValue) || 0;
  }
  
  return 0;
}

/**
 * Pobiera wartość z typowanego pola jako boolean
 */
export function getFieldValueAsBoolean(fieldValue: CostEstimateFieldValueWeb | undefined): boolean {
  if (!fieldValue) return false;
  
  if (fieldValue.boolValue !== undefined && fieldValue.boolValue !== null) {
    return fieldValue.boolValue;
  }
  if (fieldValue.stringValue !== undefined && fieldValue.stringValue !== null) {
    return fieldValue.stringValue === 'true' || fieldValue.stringValue === '1';
  }
  
  return false;
}

/**
 * Konwertuje wartość CostEstimateFieldValueWeb na DTO dla edycji
 */
export function convertFieldValueWebToDto(fv: CostEstimateFieldValueWeb): CostEstimateFieldValueDto {
  return {
    fieldDefinitionId: fv.fieldDefinitionId,
    stringValue: fv.stringValue,
    decimalValue: fv.decimalValue,
    boolValue: fv.boolValue,
    dateTimeValue: fv.dateTimeValue
  };
}

/**
 * Sprawdza czy wartość pola jest pusta (wszystkie pola wartościowe undefined/null/puste).
 * Pola bez wartości nie powinny być wysyłane w requestach.
 * UWAGA: Pola typu pliki (fieldType = 105) są obsługiwane przez osobny endpoint upload,
 * więc mogą być traktowane jako puste jeśli nie mają wartości.
 */
function isFieldValueEmpty(fv: CostEstimateFieldValueWeb): boolean {
  // Pola plików sprawdzamy też na obecność plików
  if (fv.fieldType === 105) {
    return !fv.files || fv.files.length === 0;
  }
  
  return (
    (fv.stringValue === undefined || fv.stringValue === null || fv.stringValue === '') &&
    (fv.decimalValue === undefined || fv.decimalValue === null) &&
    (fv.boolValue === undefined || fv.boolValue === null) &&
    (fv.dateTimeValue === undefined || fv.dateTimeValue === null || fv.dateTimeValue === '')
  );
}

/**
 * Konwertuje pozycję z serwera na DTO dla edycji.
 * Pomija pola bez wartości (po co je wysyłać?).
 * Gdy pozycja ma komponenty — pomija pola kalkulowane (fieldScope === 2),
 * bo backend sam je wyliczy jako sumę z komponentów.
 */
export function convertItemWebToDto(item: CostEstimateItemWeb): CostEstimateItemDto {
  const hasComponents = (item.components?.length ?? 0) > 0;

  const fieldValues = item.fieldValues
    .filter(fv => {
      // Nie wysyłaj pól bez wartości
      if (isFieldValueEmpty(fv)) return false;
      // Nie wysyłaj pól kalkulowanych gdy pozycja ma komponenty
      if (hasComponents && fv.fieldScope === 2) return false;
      return true;
    })
    .map(convertFieldValueWebToDto);

  return {
    id: isTemporaryId(item.id) ? undefined : item.id,
    parentItemId: isTemporaryId(item.parentItemId) ? undefined : item.parentItemId,
    relationType: item.relationType ?? 0,
    order: item.order,
    fieldValues,
    options: item.options?.map(convertItemWebToDto),
    components: item.components?.map(convertItemWebToDto),
  };
}

/**
 * Konwertuje grupę z serwera na DTO dla edycji.
 * Pomija pola bez wartości.
 */
export function convertGroupWebToDto(group: CostEstimateGroupWeb): CostEstimateGroupDto {
  return {
    id: isTemporaryId(group.id) ? undefined : group.id,
    parentGroupId: isTemporaryId(group.parentGroupId) ? undefined : group.parentGroupId,
    level: group.level,
    order: group.order,
    fieldValues: group.fieldValues
      .filter(fv => !isFieldValueEmpty(fv))
      .map(convertFieldValueWebToDto),
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
    relationType: 0, // None
    fieldValues: [],
    options: undefined
  };
}

// ========== EXISTING TYPES (for compatibility) ==========
// Keep existing types from the original file for backward compatibility
// NOTE: Template versioning has been removed in the refactoring

export enum CalculatedFieldType {
  UnitPriceNet = 0,
  VatRate = 1,
  UnitPriceGross = 2,
  ValueNet = 3,
  ValueGross = 4,
  UnitVat = 5,
  TotalVat = 6,
  Discount = 7,
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
