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
  structure?: CostEstimateTemplateStructureWeb; // Struktura szablonu (bez wersjonowania)
}

/**
 * Struktura szablonu - bez wersjonowania (refactoring)
 */
export interface CostEstimateTemplateStructureWeb {
  templateId: string;
  /** Maksymalny dozwolony poziom zagnieżdżenia grup (null/undefined = bez limitu) */
  maxGroupLevel?: number | null;
  currencies: TemplateCurrencyWeb[];
  units: TemplateUnitWeb[];
  groupHeaderFields: GroupHeaderFieldWeb[];
  systemFields: SystemFieldWeb[];
  calculatedFields: CalculatedFieldWeb[];
  genericFields: GenericFieldWeb[];
  summaryConfiguration?: SummaryConfigurationWeb;
  uiConfiguration?: UiConfigurationWeb;
}

export interface GroupHeaderFieldWeb {
  id: string;
  fieldName: string; // GUID pola - używane w UiConfiguration i SummaryConfiguration
  fieldType: GroupHeaderFieldType;
  customLabel?: string;
  isRequired: boolean;
  isVisible: boolean;
  order: number;
  defaultValue?: string;
  allowedValues?: string[];
  placeholder?: string;
  isReadonly: boolean;
  isSortable?: boolean;
  isFilterable?: boolean;
  displayFormat?: string;
  helpText?: string;
  helpUrl?: string;
  icon?: string;
  color?: string;
  fieldTypeConfig?: import('./costEstimate.types.new').CostEstimateFieldTypeConfigWeb; // New config from BE
}

export interface SystemFieldWeb {
  id: string;
  fieldName: string; // Nazwa pola używana w UiConfiguration i SummaryConfiguration
  fieldType: number; // FieldType enum value (100=ItemSystemName, 101=ItemSystemQuantity, 102=ItemSystemUnit)
  label: string;
  description?: string;
  isRequired: boolean;
  isVisible: boolean;
  isReadonly?: boolean;
  isSortable?: boolean;
  isFilterable?: boolean;
  order: number;
  defaultValue?: string;
  helpText?: string;
  helpUrl?: string;
  fieldTypeConfig?: import('./costEstimate.types.new').CostEstimateFieldTypeConfigWeb; // New config from BE
  childFields?: SystemFieldWeb[]; // For Options field
}

export interface CalculatedFieldWeb {
  id: string;
  fieldName: string; // Nazwa pola używana w UiConfiguration i SummaryConfiguration
  fieldType: number; // FieldType enum value (200=UnitPriceNet, 201=VatRate, 203=ValueNet, etc.)
  label: string;
  description?: string;
  unit?: string;
  displayFormat?: string;
  isSortable: boolean;
  isFilterable: boolean;
  isSummable: boolean;
  summaryScope?: SummaryScope;
  sumInGroup?: boolean; // Sumowanie w grupie
  sumInTotal?: boolean; // Sumowanie w podsumowaniu całkowitym
  isAutoCalculated: boolean;
  calculationFormula?: string;
  isReadonly: boolean;
  isRequired: boolean;
  isVisible: boolean;
  order: number;
  defaultValue?: string;
  minValue?: number; // Nie ma w DTO backendu - może być deprecated
  maxValue?: number; // Nie ma w DTO backendu - może być deprecated
  helpText?: string;
  helpUrl?: string;
  groupName?: string; // Backend: GroupName
  icon?: string; // Backend: Icon
  color?: string; // Backend: Color
  tags?: string[]; // Backend: Tags
  metadata?: string; // Backend: Metadata
  fieldTypeConfig?: import('./costEstimate.types.new').CostEstimateFieldTypeConfigWeb; // New config from BE
}

export interface GenericFieldWeb {
  id: string;
  fieldName: string; // Nazwa pola używana w UiConfiguration i SummaryConfiguration
  fieldType: number; // FieldType enum value (300=Integer, 301=Decimal, 302=String, etc.)
  label: string;
  description?: string;
  displayFormat?: string;
  isSortable: boolean;
  isFilterable: boolean;
  isReadonly?: boolean;
  minValue?: number;
  maxValue?: number;
  minLength?: number;
  maxLength?: number;
  pattern?: string;
  isRequired: boolean;
  isVisible: boolean;
  order: number;
  defaultValue?: string;
  allowedValues?: string[];
  placeholder?: string;
  helpText?: string;
  helpUrl?: string;
  nestedFields?: NestedFieldWeb;
  fieldTypeConfig?: import('./costEstimate.types.new').CostEstimateFieldTypeConfigWeb; // New config from BE
}

export interface NestedFieldWeb {
  calculatedFields?: CalculatedFieldWeb[];
  genericFields?: GenericFieldWeb[];
  minItems?: number;
  maxItems?: number;
  isSelectableCollection?: boolean;
  enableCalculatedFieldsSummation?: boolean;
  summableCalculatedFields?: string[];
  uiConfiguration?: {
    columns?: ColumnConfigurationWeb[];
  };
}

export interface SummaryConfigurationWeb {
  showGroupSummary: boolean;
  showTotalSummary: boolean;
  groupSummaryFields: SummaryFieldWeb[];
  totalSummaryFields: SummaryFieldWeb[];
}

export interface SummaryFieldWeb {
  fieldId: string;        // GUID
  fieldName: string;      // GUID pola
  fieldType: number;      // Typ pola w ramach swojego scope (SystemFieldType/CalculatedFieldType/etc.)
  fieldLabel: string;     // Etykieta wyświetlana
  fieldSource: number;    // FieldScope enum (0=GroupHeader, 1=System, 2=Calculated, 3=Generic)
  order: number;          // Kolejność
}

export interface UiConfigurationWeb {
  columns: ColumnConfigurationWeb[];
}

// DTO do command update - tylko kolejność kolumn
export interface UiConfigurationDto {
  columnLayout?: string[];  // Lista GUID-ów pól określająca kolejność kolumn
}

export interface ColumnConfigurationWeb {
  fieldId: string;        // GUID
  fieldName: string;      // GUID pola
  fieldType: number;      // Typ pola w ramach swojego scope
  fieldLabel: string;     // Etykieta wyświetlana
  fieldScope: number;     // FieldScope enum (0=GroupHeader, 1=System, 2=Calculated, 3=Generic)
  order: number;          // Kolejność
  isVisible?: boolean;    // Czy kolumna jest widoczna w UI
}

export interface TemplateCurrencyWeb {
  id: string;
  code: string;
  name: string;
  symbol?: string;
  isDefault: boolean;
  order: number;
}

export interface TemplateUnitWeb {
  id: string;
  code: string;
  name: string;
  symbol: string;
  category?: string;
  isDefault: boolean;
  order: number;
}

export interface CostEstimateListItem {
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
  createdAt: string;
  updatedAt?: string;
  ownerId: string;
  ownerName: string;
}

export interface CostEstimateDetails {
  id: string;
  tenantId: string;
  projectId: string;
  templateId: string;
  templateName: string;
  templateStructure: CostEstimateTemplateStructure;
  selectedCurrencyId: string;
  selectedCurrencyCode: string;
  selectedCurrencySymbol?: string;
  name: string;
  description?: string;
  status: CostEstimateStatus;
  rootGroups: CostEstimateGroup[];
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  createdAt: string;
  updatedAt?: string;
  lastCalculatedAt?: string;
  ownerId: string;
  ownerName: string;
}

export interface CostEstimateTemplateDto {
  id: string;
  name: string;
  description?: string;
  currency?: string;
  templateVersionNumber?: number;
  templateStructure: CostEstimateTemplateStructure;
  createdAt: string;
  updatedAt?: string;
  ownerId: string;
  ownerName: string;
}

export interface CostEstimateDataModel {
  groups: CostEstimateGroup[];
  totals?: Record<string, number>;
  metadata?: CostEstimateMetadata;
}

export interface CostEstimateGroup {
  id: string;
  parentId?: string;
  level: number;
  number?: string;
  order: number;
  headerValues: Record<string, any>;
  workScopes: CostEstimateWorkScope[];
  subGroups?: CostEstimateGroup[];
  groupTotals?: Record<string, number>; // DEPRECATED - użyj totalNet/totalGross/totalVat lub summaryTotals
  // Sumy obliczone przez backend (stara struktura - do usunięcia)
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  // Nowa struktura - sumy jako słownik gdzie klucz = fieldName (GUID) z groupSummaryFields
  summaryTotals?: Record<string, number>;
  lastCalculatedAt?: string;
}

export interface CostEstimateWorkScope {
  id: string;
  order: number;
  assignedUserId?: string;
  calculatedFieldValues: Record<string, any>;
  genericFieldValues: Record<string, any>;
  collectionFieldValues?: Record<string, CostEstimateCollectionItem[]>;
  lockedFields?: string[]; // Pola zablokowane przed auto-kalkulacją (np. skopiowane z kolekcji)
}

export interface CostEstimateCollectionItem {
  id: string;
  isSelected?: boolean;
  calculatedFieldValues?: Record<string, any>;
  genericFieldValues?: Record<string, any>;
}

export interface CostEstimateMetadata {
  lastModified: string;
  lastModifiedBy?: string;
  schemaVersion: number;
  additionalInfo?: Record<string, string>;
  groupCustomizations?: Record<string, GroupUiCustomization>;
  workScopeCustomizations?: Record<string, WorkScopeUiCustomization>;
}

export interface GroupUiCustomization {
  headerColor?: string;
  headerBackgroundColor?: string;
  icon?: string;
  collapsed?: boolean;
  highlighted?: boolean;
  notes?: string;
}

export interface WorkScopeUiCustomization {
  rowColor?: string;
  textColor?: string;
  highlighted?: boolean;
  tags?: string[];
  notes?: string;
}

/**
 * Zakres pola w szablonie kosztorysu - określa do czego pole należy
 */
export enum FieldScope {
  /** Pole należy do nagłówka grupy */
  Group = 0,
  /** Pole systemowe pozycji (work scope) */
  ItemSystem = 1,
  /** Pole obliczeniowe pozycji (work scope) */
  ItemCalculated = 2,
  /** Pole generyczne pozycji (work scope) */
  ItemGeneric = 3,
}

/**
 * Ujednolicony typ pola w szablonie kosztorysu
 * Łączy wszystkie typy pól z odpowiednimi prefiksami dla czytelności
 */
export enum FieldType {
  // GROUP HEADER FIELDS (0-9)
  GroupName = 0,
  GroupDescription = 1,
  GroupNumber = 2,
  GroupStartDate = 3,
  GroupEndDate = 4,
  GroupStatus = 5,
  GroupNotes = 6,
  GroupResponsible = 7,
  GroupBudget = 8,
  GroupPriority = 9,

  // ITEM SYSTEM FIELDS (100-199)
  ItemSystemName = 100,
  ItemSystemQuantity = 101,
  ItemSystemUnit = 102,
  ItemSystemOptions = 103,
  ItemSystemSelected = 104,

  // ITEM CALCULATED FIELDS (200-299)
  ItemCalculatedUnitPriceNet = 200,
  ItemCalculatedVatRate = 201,
  ItemCalculatedUnitPriceGross = 202,
  ItemCalculatedValueNet = 203,
  ItemCalculatedValueGross = 204,
  ItemCalculatedUnitVat = 205,
  ItemCalculatedTotalVat = 206,

  // ITEM GENERIC FIELDS (300-399)
  ItemGenericInteger = 300,
  ItemGenericDecimal = 301,
  ItemGenericString = 302,
  ItemGenericBoolean = 303,
  ItemGenericDate = 304,
  ItemGenericDateTime = 305,
}

export enum SummaryScope {
  Group = 0,
  Total = 1,
  Both = 2,
}

// ============================================================================
// DEPRECATED ENUMS - For backward compatibility during migration
// Use FieldType instead
// ============================================================================

/**
 * @deprecated Używaj FieldType zamiast GroupHeaderFieldType
 * Mapowanie: GroupHeaderFieldType.X → FieldType.GroupX
 */
export enum GroupHeaderFieldType {
  GroupName = 0,           // → FieldType.GroupName
  GroupDescription = 1,    // → FieldType.GroupDescription
  GroupNumber = 2,         // → FieldType.GroupNumber
  StartDate = 3,           // → FieldType.GroupStartDate
  EndDate = 4,             // → FieldType.GroupEndDate
  Status = 5,              // → FieldType.GroupStatus
  Notes = 6,               // → FieldType.GroupNotes
  Responsible = 7,         // → FieldType.GroupResponsible
  Budget = 8,              // → FieldType.GroupBudget
  Priority = 9,            // → FieldType.GroupPriority
}

/**
 * @deprecated Używaj FieldType zamiast SystemFieldType
 * Mapowanie: SystemFieldType.X → FieldType.ItemSystemX
 */
export enum SystemFieldType {
  Name = 0,                // → FieldType.ItemSystemName (100)
  Quantity = 1,            // → FieldType.ItemSystemQuantity (101)
  Unit = 2,                // → FieldType.ItemSystemUnit (102)
  Options = 3,             // → FieldType.ItemSystemOptions (103)
  Selected = 4,            // → FieldType.ItemSystemSelected (104)
}

/**
 * @deprecated Używaj FieldType zamiast CalculatedFieldType
 * Mapowanie: CalculatedFieldType.X → FieldType.ItemCalculatedX
 */
export enum CalculatedFieldType {
  UnitPriceNet = 0,        // → FieldType.ItemCalculatedUnitPriceNet (200)
  VatRate = 1,             // → FieldType.ItemCalculatedVatRate (201)
  UnitPriceGross = 2,      // → FieldType.ItemCalculatedUnitPriceGross (202)
  ValueNet = 3,            // → FieldType.ItemCalculatedValueNet (203)
  ValueGross = 4,          // → FieldType.ItemCalculatedValueGross (204)
  UnitVat = 5,             // → FieldType.ItemCalculatedUnitVat (205)
  TotalVat = 6,            // → FieldType.ItemCalculatedTotalVat (206)
}

/**
 * @deprecated Używaj FieldType zamiast GenericFieldType
 * Mapowanie: GenericFieldType.X → FieldType.ItemGenericX
 */
export enum GenericFieldType {
  Integer = 0,             // → FieldType.ItemGenericInteger (300)
  Decimal = 1,             // → FieldType.ItemGenericDecimal (301)
  String = 2,              // → FieldType.ItemGenericString (302)
  Boolean = 3,             // → FieldType.ItemGenericBoolean (303)
  Date = 4,                // → FieldType.ItemGenericDate (304)
  DateTime = 5,            // → FieldType.ItemGenericDateTime (305)
  Collection = 10,         // → FieldType.ItemGenericCollection (310) - kolekcja zagnieżdżonych pól
}

export interface BaseFieldDefinition {
  id?: string; // Opcjonalne dla zgodności wstecznej
  name: string;
  label: string;
  description?: string;
  defaultValue?: string;
  order: number;
  required: boolean;
  visible: boolean;
  visibilityCondition?: string;
  requiredCondition?: string;
  helpText?: string;
  helpUrl?: string;
  groupName?: string;
  icon?: string;
  color?: string;
  tags?: string[];
  metadata?: string;
  /** Konfiguracja typu pola z BE - używana do wyświetlania nazwy typu */
  fieldTypeConfig?: {
    fieldType: number;
    fieldScope: number;
    namePl: string;
    valueTypeName: string;
    isNumeric: boolean;
    isText: boolean;
    isDate: boolean;
    isBoolean: boolean;
    isCollection: boolean;
  };
}

export interface SystemFieldDefinition extends BaseFieldDefinition {
  type: SystemFieldType;
  sortable?: boolean;
  filterable?: boolean;
  readOnly?: boolean;
  childFields?: Array<SystemFieldDefinition | CalculatedFieldDefinition | GenericFieldDefinition>;
}

export interface CalculatedFieldDefinition extends BaseFieldDefinition {
  type: CalculatedFieldType;
  unit?: string;
  displayFormat?: string;
  sortable: boolean;
  filterable: boolean;
  summable: boolean;
  summaryScope?: SummaryScope;
  sumInGroup?: boolean;   // Sumowanie w grupie
  sumInTotal?: boolean;   // Sumowanie w podsumowaniu całkowitym
  autoCalculated: boolean;
  calculationFormula?: string;
  readOnly: boolean;
}

export interface GenericFieldDefinition extends BaseFieldDefinition {
  type: GenericFieldType;
  displayFormat?: string;
  sortable: boolean;
  filterable: boolean;
  readOnly?: boolean;
  minValue?: number;
  maxValue?: number;
  minLength?: number;
  maxLength?: number;
  pattern?: string;
  allowedValues?: string[];
  placeholder?: string;
  nestedFields?: GenericFieldCollectionDefinition;
}

export interface GenericFieldCollectionDefinition {
  calculatedFields?: CalculatedFieldDefinition[];
  genericFields?: GenericFieldDefinition[];
  minItems?: number;
  maxItems?: number;
  isSelectableCollection: boolean;
  enableCalculatedFieldsSummation: boolean;
  summableCalculatedFields?: string[];
  uiConfiguration?: CostEstimateUiConfiguration;
}

export interface GroupHeaderFieldDefinition {
  id?: string; // Opcjonalne dla zgodności wstecznej
  name?: string; // GUID jako fieldName
  type: GroupHeaderFieldType;
  customLabel?: string;
  required: boolean;
  visible: boolean;
  order: number;
  defaultValue?: string;
  allowedValues?: string[];
  placeholder?: string;
  readOnly: boolean;
  displayFormat?: string;
  helpText?: string;
  helpUrl?: string;
  icon?: string;
  color?: string;
  sortable?: boolean;
  filterable?: boolean;
  fieldTypeConfig?: {
    fieldType: number;
    fieldScope: number;
    namePl: string;
    valueTypeName: string;
    isNumeric: boolean;
    isText: boolean;
    isDate: boolean;
    isBoolean: boolean;
    isCollection: boolean;
  };
}

export interface CostEstimateGroupDefinition {
  autoNumbered: boolean;
  numberFormat?: string;
  headerFields: GroupHeaderFieldDefinition[];
}

export interface CostEstimateWorkScopeFieldsDefinition {
  calculatedFields: CalculatedFieldDefinition[];
  genericFields: GenericFieldDefinition[];
  crossFieldValidationRules?: CrossFieldValidationRule[];
}

export interface CrossFieldValidationRule {
  ruleName: string;
  expression: string;
  errorMessage: string;
  isActive: boolean;
}

export interface CostEstimateSummaryConfiguration {
  // Nowe API (preferowane)
  groupSummaryFields?: SummaryFieldWeb[] | string[]; // SummaryFieldWeb[] z backendu lub string[] lokalnie
  totalSummaryFields?: SummaryFieldWeb[] | string[]; // SummaryFieldWeb[] z backendu lub string[] lokalnie
  showGroupSummary: boolean;
  showTotalSummary: boolean;
}

export interface CostEstimateUiConfiguration {
  columnLayout?: string[]; // Stare API - zachowane dla kompatybilności
  columnWidths?: Record<string, string>;
  columns?: ColumnConfigurationWeb[]; // Nowe API - preferowane
}

export interface CostEstimateTemplateStructure {
  canAddGroups: boolean;
  canBranchGroups: boolean;
  maxGroupLevel?: number;
  
  // Nowe pola zgodne z dokumentacją API (snapshot z CostEstimateTemplateVersionStructureWeb ale BEZ versionId/versionNumber/versionName)
  currencies?: TemplateCurrencyWeb[];
  units?: TemplateUnitWeb[];
  groupHeaderFields?: GroupHeaderFieldWeb[];
  systemFields?: SystemFieldWeb[];
  calculatedFields?: CalculatedFieldWeb[];
  genericFields?: GenericFieldWeb[];
  summaryConfiguration?: SummaryConfigurationWeb;
  uiConfiguration?: UiConfigurationWeb;
  
  // Stare pola zachowane dla kompatybilności wstecznej (DEPRECATED)
  groupDefinition?: CostEstimateGroupDefinition;
  workScopeFieldsDefinition?: CostEstimateWorkScopeFieldsDefinition;
}
