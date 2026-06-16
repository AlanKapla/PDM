export enum CostEstimateStatus {
  Draft = 0,
  InProgress = 1,
  ReadyForReview = 2,
  Approved = 3,
  Rejected = 4,
  Archived = 5,
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
  ReadOnly = 1,
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
 * Może zawierać kolekcję Options jeśli relationType=1
 * Może zawierać kolekcję Components - wtedy NIE MOŻE mieć FieldValues!
 */
export interface CostEstimateItemDto {
  id?: string;              // null/undefined dla nowych pozycji
  parentItemId?: string;    // ID pozycji nadrzędnej (jeśli to opcja lub komponent)
  relationType: number;     // ItemRelationType: None=0, Option=1, Component=2
  order: number;
  name?: string;            // NOWE — direct property
  quantity?: number;        // NOWE — direct property
  unit?: string;            // NOWE
  unitPriceNet?: number;    // NOWE
  vatRate?: number;         // NOWE
  additionalFieldValues: CostEstimateAdditionalFieldValueDto[]; // NOWE
  options?: CostEstimateItemDto[];     // Kolekcja opcji - max 1 poziom zagnieżdżenia!
  components?: CostEstimateItemDto[];  // Kolekcja komponentów - jeśli są, FieldValues musi być puste!
  /**
   * @deprecated Użyj bezpośrednich właściwości i additionalFieldValues
   */
  fieldValues?: CostEstimateFieldValueDto[];
}

/**
 * DTO dla tworzenia/edycji grupy kosztorysu (rekurencyjna struktura)
 */
export interface CostEstimateGroupDto {
  id?: string;              // null/undefined dla nowych grup
  parentGroupId?: string;
  level: number;
  order: number;
  name?: string;            // NOWE — direct property
  additionalFieldValues: CostEstimateAdditionalFieldValueDto[]; // NOWE
  items: CostEstimateItemDto[];
  childGroups: CostEstimateGroupDto[];
  /**
   * @deprecated Użyj name (direct property) i additionalFieldValues
   */
  fieldValues?: CostEstimateGroupFieldValueDto[];
}

/**
 * DTO dla tworzenia kosztorysu z pełną strukturą.
 * Waluta nie jest wysyłana — backend pobiera ją z ProjectCurrency projektu.
 */
export interface CreateCostEstimateDto {
  name: string;
  description?: string;
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

// ========== ADDITIONAL FIELDS (NEW ARCHITECTURE) ==========

export enum AdditionalFieldType {
  String = 0,
  Decimal = 1,
  Boolean = 2,
  DateTime = 3,
}

/** Typ kolumny w schemacie kosztorysu — pola dodatkowe (0–9) i podstawowe (100+). */
export enum CostEstimateFieldType {
  Text = 0,
  Number = 1,
  Boolean = 2,
  Date = 3,
  Select = 4,

  Name = 100,
  Quantity = 101,
  Unit = 102,
  UnitPriceNet = 103,
  VatRate = 104,
  UnitPriceGross = 105,
  NetValue = 106,
  GrossValue = 107,
  VatValue = 108,
  IsSelected = 109,
  IsStageWork = 110,
  Files = 111,
  Actions = 112,
  ItemSystemOptions = 113,
}

/**
 * Wpis schematu kolumn kosztorysu (pola podstawowe i dodatkowe).
 */
export interface CostEstimateFieldSchemaWeb {
  id: string;
  costEstimateId: string;
  fieldName: string;
  fieldKey: string;
  fieldType: CostEstimateFieldType;
  isBasicField: boolean;
  isAdditionalField: boolean;
  order: number;
  createdAt: string;
  updatedAt?: string;
}

/**
 * Definicja pola dodatkowego w kosztorysie
 */
export interface CostEstimateAdditionalFieldWeb {
  id: string;
  costEstimateId: string;
  name: string;           // "Kod CPV", "Uwagi"
  fieldType: AdditionalFieldType;
  order: number;
  createdAt: string;
  updatedAt?: string;
}

/**
 * Wartość pola dodatkowego (wspólna dla grup i pozycji)
 */
export interface CostEstimateAdditionalFieldValueWeb {
  id: string;
  additionalFieldId: string;
  stringValue?: string;
  decimalValue?: number;
  boolValue?: boolean;
  dateTimeValue?: string; // ISO 8601
}

/**
 * Plik na pozycji (zastępuje CostEstimateFieldFileWeb)
 */
export interface CostEstimateItemFileWeb {
  id: string;
  itemId: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  order: number;
  sasUriPreview: string | null;
  sasUriDownload: string | null;
  createdAt: string;
}

/**
 * DTO dla wartości pola dodatkowego
 */
export interface CostEstimateAdditionalFieldValueDto {
  id?: string;
  additionalFieldId: string;
  stringValue?: string;
  decimalValue?: number;
  boolValue?: boolean;
  dateTimeValue?: string;
}

// ========== RESPONSE DTOs ==========

/**
 * Plik dołączony do pola kosztorysu typu ItemSystemFiles (fieldType = 105)
 * @deprecated Użyj CostEstimateItemFileWeb
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
 * @deprecated Zastąpione przez bezpośrednie właściwości na encjach i CostEstimateAdditionalFieldValueWeb
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
 * Może zawierać kolekcję Options jeśli relationType=1
 * Może zawierać kolekcję Components - pozycja składa się z komponentów (relationType=2)
 */
export interface CostEstimateItemWeb {
  id: string;
  groupId: string;
  parentItemId?: string;      // ID pozycji nadrzędnej (jeśli to opcja lub komponent)
  relationType: number;       // ItemRelationType: None=0, Option=1, Component=2 — wymagane
  order: number;
  name: string;               // NOWE — direct property
  quantity?: number;          // NOWE — direct property
  unit?: string;              // NOWE
  unitPriceNet?: number;      // NOWE
  vatRate?: number;           // NOWE
  unitPriceGross?: number;    // NOWE
  netValue?: number;          // Obliczona wartość netto (z komponentów lub pól)
  grossValue?: number;        // Obliczona wartość brutto
  vatValue?: number;          // Obliczona wartość VAT
  isSelected: boolean;        // NOWE — default true
  isStageWork: boolean;       // NOWE — default false
  additionalFieldValues: CostEstimateAdditionalFieldValueWeb[]; // NOWE
  options?: CostEstimateItemWeb[];      // Kolekcja opcji (zagnieżdżonych pozycji)
  components?: CostEstimateItemWeb[];   // Kolekcja komponentów (składników pozycji)
  files?: CostEstimateItemFileWeb[];    // NOWE — pliki na pozycji
  /**
   * @deprecated Użyj bezpośrednich właściwości (name, quantity, unit itd.) oraz additionalFieldValues
   */
  fieldValues?: CostEstimateFieldValueWeb[];
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
  name: string;                            // NOWE — zamiast FieldValues
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  additionalFieldValues: CostEstimateAdditionalFieldValueWeb[]; // NOWE
  lastCalculatedAt?: string;
  childGroups: CostEstimateGroupWeb[];
  items: CostEstimateItemWeb[];
  createdAt: string;
  updatedAt?: string;
  /**
   * @deprecated Użyj name (direct property) i additionalFieldValues
   */
  fieldValues?: CostEstimateFieldValueWeb[];
}

/**
 * Szczegóły kosztorysu z pełną hierarchią
 */
export interface CostEstimateDetailsWeb {
  id: string;
  tenantId: string;
  projectId: string;
  selectedCurrencyCode: string;
  selectedCurrencySymbol?: string;
  name: string;
  description?: string;
  status: CostEstimateStatus;
  rootGroups: CostEstimateGroupWeb[];
  fieldSchemas: CostEstimateFieldSchemaWeb[];
  additionalFields: CostEstimateAdditionalFieldWeb[];
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  createdAt: string;
  updatedAt?: string;
  lastCalculatedAt?: string;
  ownerId: string;
  ownerName: string;
  /**
   * @deprecated Zastąpione przez additionalFields. Zachowane dla kompatybilności wstecznej.
   */
  schema?: CostEstimateSchemaWeb;
  /** ID powiązanego harmonogramu (jeśli istnieje) */
  workScheduleId?: string;
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
  /** Kod waluty projektu (np. "PLN", "EUR") */
  currencyCode?: string;
  /** Symbol waluty projektu (np. "zł", "€") */
  currencySymbol?: string;
}

// ========== COMPUTED FLAGS ==========

/**
 * Flagi blokowania pól finansowych — obliczane client-side na podstawie zasad kalkulacji
 */
export interface ComputedFlags {
  netValueComputed: boolean;
  vatValueComputed: boolean;
  grossValueComputed: boolean;
  unitPriceGrossComputed: boolean;
  financialFieldsLockedByComponents: boolean;
  financialFieldsLockedByOptions: boolean;
}

// ========== HELPER FUNCTIONS ==========

/**
 * Sprawdza czy ID jest tymczasowe (nowy element)
 */
export function isTemporaryId(id?: string): boolean {
  return !id || id.startsWith('temp_') || id.startsWith('calc_') || id.startsWith('calc_opt_');
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
 * DTO dla zmiany kolejności elementów potomnych (opcji/komponentów) w pozycji nadrzędnej
 */
export interface ReorderItemChildDto {
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
 * Request body dla reorder elementów potomnych (opcji/komponentów)
 */
export interface ReorderItemChildrenRequestDto {
  costEstimateId: string;
  items: ReorderItemChildDto[];
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
 * @deprecated Używaj bezpośrednich właściwości na encjach
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
 * @deprecated Używaj bezpośrednich właściwości na encjach
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
 * @deprecated Używaj bezpośrednich właściwości na encjach
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
 * @deprecated Używaj bezpośrednich właściwości na encjach i CostEstimateAdditionalFieldValueDto
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
 * @deprecated Używaj bezpośrednich właściwości i additionalFieldValues
 */
export function convertItemWebToDto(item: CostEstimateItemWeb): CostEstimateItemDto {
  return {
    id: isTemporaryId(item.id) ? undefined : item.id,
    parentItemId: isTemporaryId(item.parentItemId) ? undefined : item.parentItemId,
    relationType: item.relationType ?? 0,
    order: item.order,
    name: item.name,
    quantity: item.quantity,
    unit: item.unit,
    unitPriceNet: item.unitPriceNet,
    vatRate: item.vatRate,
    additionalFieldValues: (item.additionalFieldValues ?? []).map(fv => ({
      id: fv.id,
      additionalFieldId: fv.additionalFieldId,
      stringValue: fv.stringValue,
      decimalValue: fv.decimalValue,
      boolValue: fv.boolValue,
      dateTimeValue: fv.dateTimeValue,
    })),
    options: item.options?.map(convertItemWebToDto),
    components: item.components?.map(convertItemWebToDto),
  };
}

/**
 * Konwertuje grupę z serwera na DTO dla edycji.
 * Pomija pola bez wartości.
 * @deprecated Używaj bezpośrednich właściwości i additionalFieldValues
 */
export function convertGroupWebToDto(group: CostEstimateGroupWeb): CostEstimateGroupDto {
  return {
    id: isTemporaryId(group.id) ? undefined : group.id,
    parentGroupId: isTemporaryId(group.parentGroupId) ? undefined : group.parentGroupId,
    level: group.level,
    order: group.order,
    name: group.name,
    additionalFieldValues: (group.additionalFieldValues ?? []).map(fv => ({
      id: fv.id,
      additionalFieldId: fv.additionalFieldId,
      stringValue: fv.stringValue,
      decimalValue: fv.decimalValue,
      boolValue: fv.boolValue,
      dateTimeValue: fv.dateTimeValue,
    })),
    items: (group.items || []).map(convertItemWebToDto),
    childGroups: (group.childGroups || []).map(convertGroupWebToDto),
  };
}

/**
 * Konwertuje szczegóły kosztorysu na DTO dla edycji
 * @deprecated Używaj bezpośrednich właściwości i additionalFieldValues
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
    name: '',
    additionalFieldValues: [],
    items: [],
    childGroups: [],
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
    name: '',
    additionalFieldValues: [],
    options: undefined,
  };
}

// ========== AI COST ESTIMATE GENERATION ==========

/**
 * Dane wejściowe od użytkownika — opis inwestycji.
 * Mapuje się na AICostEstimateRequestWeb po stronie API.
 */
export interface AICostEstimateRequestDto {
  /** Co budujesz? (wolny tekst) */
  investmentType: string;
  /** Stan wykończenia */
  finishingStandard?: string;
  /** Szacowany budżet brutto w PLN */
  budget?: number;
  /** Powierzchnia/zakres */
  area?: number;
  /** Jednostka powierzchni (m², mb, szt) */
  areaUnit?: string;
  /** Lokalizacja inwestycji */
  location?: string;
  /** Rok ukończenia */
  completionYear?: number;
  /** Dodatkowe wymagania */
  additionalRequirements?: string;
}

/**
 * Wartość pola wygenerowana przez AI.
 */
export interface AIFieldValueDto {
  fieldDefinitionId: string;
  decimalValue?: number;
  stringValue?: string;
  boolValue?: boolean;
  dateTimeValue?: string;
}

/**
 * Pozycja kosztorysowa w podglądzie AI.
 */
export interface AIItemPreviewDto {
  tempId: string;
  name: string;
  order: number;
  fieldValues: AIFieldValueDto[];
}

/**
 * Grupa kosztorysowa w podglądzie AI.
 */
export interface AIGroupPreviewDto {
  tempId: string;
  parentTempId?: string | null;
  name: string;
  order: number;
  fieldValues: AIFieldValueDto[];
  items: AIItemPreviewDto[];
  children?: AIGroupPreviewDto[];
}

/**
 * Podgląd kosztorysu wygenerowanego przez AI.
 * NIE jest zapisany w bazie danych — służy do prezentacji i zatwierdzenia przez użytkownika.
 */
export interface AICostEstimatePreviewDto {
  suggestedName: string;
  suggestedDescription?: string | null;
  groups: AIGroupPreviewDto[];
  warnings: string[];
}

/**
 * Żądanie zapisu zatwierdzonego podglądu AI.
 */
export interface CreateCostEstimateFromAIPreviewDto {
  name: string;
  description?: string;
  preview: AICostEstimatePreviewDto;
}

// ========== SCHEMA-BASED STRUCTURE (DEPRECATED) ==========

/**
 * Definicja pola w schemacie kosztorysu (backend: CostEstimateFieldDefinitionWeb)
 * @deprecated Zastąpione przez CostEstimateAdditionalFieldWeb
 */
export interface CostEstimateFieldDefinitionWeb {
  id: string;
  fieldName: string;               // Guid as string - fixed Guid for default fields
  fieldScope: number;              // FieldScope enum (Group=0, ItemSystem=1, ItemCalculated=2, ItemGeneric=3)
  fieldType: number;               // FieldType enum (0-99=Group, 100-199=ItemSystem, 200-299=ItemCalculated, 300-399=ItemGeneric)
  label: string;                   // User-visible label
  isSortable: boolean;
  isFilterable: boolean;
  isVisible: boolean;              // Show/hide in UI
  isReadonly: boolean;             // Calculated fields are readonly
  parentFieldId: string | null;    // For nested fields (e.g., options under main field)
  order: number;                   // 0-based display order
  isUserDefined: boolean;          // true = user added field (can delete), false = system field
  canRename: boolean;              // All fields can be renamed (label change)
  canDelete: boolean;              // Only user-defined fields can be deleted
  childFields: CostEstimateFieldDefinitionWeb[] | null;  // Nested fields (for collections)
}

/**
 * Schema kosztorysu — zbiór definicji pól (backend: CostEstimateSchemaWeb)
 * @deprecated Zastąpione przez CostEstimateAdditionalFieldWeb[] w CostEstimateDetailsWeb.additionalFields
 */
export interface CostEstimateSchemaWeb {
  id: string;
  costEstimateId: string;
  fieldDefinitions: CostEstimateFieldDefinitionWeb[];
  createdAt: string;               // ISO 8601
  updatedAt: string | null;        // ISO 8601
}

// CostEstimateDetailsWebWithSchema removed - CostEstimateDetailsWeb now uses schema directly

// ========== FIELD VALUE TYPE HELPER ==========

/**
 * Określa typ wartości pola na podstawie fieldType (FieldType enum).
 * Używane do autosave - mówi jakiego pola w DTO użyć (stringValue/decimalValue/boolValue/dateTimeValue).
 * 
 * FieldType ranges:
 * - GroupHeader: 0-99 (0=Name string, 3=StartDate date, 4=EndDate date, 8=Budget numeric)
 * - ItemSystem: 100-199 (100=Name string, 101=Quantity numeric, 102=Unit string, 104=Selected boolean, 105=Files string, 107=IsWorkScope boolean)
 * - ItemCalculated: 200-299 (all numeric)
 * - ItemGeneric: 300-399 (300=Integer numeric, 301=Decimal numeric, 302=String string, 303=Boolean boolean, 304=Date date, 305=DateTime date)
 * @deprecated Używaj AdditionalFieldType zamiast FieldType
 */
export type FieldValueType = 'string' | 'numeric' | 'boolean' | 'date';

/**
 * @deprecated Używaj AdditionalFieldType
 */
export function getFieldValueTypeFromFieldType(fieldType: number): FieldValueType {
  // GroupHeader: Budget = 8 is numeric
  if (fieldType === 8) return 'numeric';
  // GroupHeader: StartDate = 3, EndDate = 4 are dates
  if (fieldType === 3 || fieldType === 4) return 'date';
  
  // ItemSystem: Quantity = 101 is numeric
  if (fieldType === 101) return 'numeric';
  // ItemSystem: Selected = 104, IsWorkScope = 107 are booleans
  if (fieldType === 104 || fieldType === 107) return 'boolean';
  
  // ItemCalculated: 200-299 all numeric
  if (fieldType >= 200 && fieldType <= 299) return 'numeric';
  
  // ItemGeneric:
  if (fieldType === 300 || fieldType === 301) return 'numeric';  // Integer, Decimal
  if (fieldType === 303) return 'boolean';  // Boolean
  if (fieldType === 304 || fieldType === 305) return 'date';  // Date, DateTime
  
  // Everything else is string
  return 'string';
}

/**
 * Determines value type from a field definition (supports both old FieldDefinitionWeb with fieldTypeConfig 
 * and new CostEstimateFieldDefinitionWeb with just fieldType).
 */
export function getFieldValueType(fieldDef: { 
  fieldType?: number; 
  fieldTypeConfig?: { isNumeric?: boolean; isBoolean?: boolean; isDate?: boolean; isText?: boolean } 
}): FieldValueType {
  const cfg = fieldDef?.fieldTypeConfig;
  
  // Prefer fieldTypeConfig if available
  if (cfg) {
    if (cfg.isNumeric) return 'numeric';
    if (cfg.isBoolean) return 'boolean';
    if (cfg.isDate) return 'date';
    return 'string';
  }
  
  // Fallback to fieldType
  const ft = fieldDef?.fieldType;
  if (ft === undefined) return 'string';
  return getFieldValueTypeFromFieldType(ft);
}
