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
  template: CostEstimateTemplateDto;
  name: string;
  description?: string;
  status: CostEstimateStatus;
  data: CostEstimateDataModel;
  totalNet?: number;
  totalGross?: number;
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
  groupTotals?: Record<string, number>;
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

export enum CalculatedFieldType {
  UnitPriceNet = 0,
  VatRate = 1,
  UnitPriceGross = 2,
  Quantity = 3,
  ValueNet = 4,
  ValueGross = 5,
  UnitVat = 6,
  TotalVat = 7,
}

export enum GenericFieldType {
  Integer = 0,
  Decimal = 1,
  String = 2,
  Boolean = 3,
  Date = 4,
  DateTime = 5,
  Collection = 10,
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

export enum SummaryScope {
  Group = 0,
  Total = 1,
  Both = 2,
}

export interface BaseFieldDefinition {
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
}

export interface CalculatedFieldDefinition extends BaseFieldDefinition {
  type: CalculatedFieldType;
  unit?: string;
  displayFormat?: string;
  sortable: boolean;
  filterable: boolean;
  summable: boolean;
  summaryScope?: SummaryScope;
  autoCalculated: boolean;
  calculationFormula?: string;
  readOnly: boolean;
}

export interface GenericFieldDefinition extends BaseFieldDefinition {
  type: GenericFieldType;
  displayFormat?: string;
  sortable: boolean;
  filterable: boolean;
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
  groupSummaryFields: string[];
  totalSummaryFields: string[];
  showGroupSummary: boolean;
  showTotalSummary: boolean;
}

export interface CostEstimateUiConfiguration {
  columnLayout?: string[];
  columnWidths?: Record<string, string>;
}

export interface CostEstimateTemplateStructure {
  canAddGroups: boolean;
  canBranchGroups: boolean;
  maxGroupLevel?: number;
  groupDefinition: CostEstimateGroupDefinition;
  workScopeFieldsDefinition: CostEstimateWorkScopeFieldsDefinition;
  summaryConfiguration?: CostEstimateSummaryConfiguration;
  uiConfiguration?: CostEstimateUiConfiguration;
}
