import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
  UpdateCostEstimateDto,
  CostEstimateGroupDto,
  CostEstimateItemDto,
} from '../types/costEstimate.types.new';
import { getFieldValueAsString } from '../types/costEstimate.types.new';
import type {
  CostEstimateDataModel,
  CostEstimateGroup,
  CostEstimateWorkScope,
  CostEstimateTemplateDto,
  GroupHeaderFieldType,
} from '../types/costEstimate.types';

/**
 * Struktura szablonu z backendu (rzeczywista struktura z API)
 */
export interface CostEstimateTemplateStructureWeb {
  canAddGroups: boolean;
  canBranchGroups: boolean;
  maxGroupLevel: number | null;
  autoNumberGroups: boolean;
  groupNumberFormat: string | null;
  currencies: CurrencyWeb[];
  units: UnitWeb[];
  categories: CategoryWeb[];
  groupHeaderFields: FieldDefinitionWeb[];
  systemFields: FieldDefinitionWeb[];
  calculatedFields: FieldDefinitionWeb[];
  genericFields: FieldDefinitionWeb[];
  summaryConfiguration: SummaryConfigurationWeb | null;
  uiConfiguration: UiConfigurationWeb | null;
}

export interface CurrencyWeb {
  id: string;
  code: string;
  name: string;
  symbol: string | null;
  isDefault: boolean;
  order: number;
}

export interface UnitWeb {
  id: string;
  code: string;
  name: string;
  symbol: string;
  category: string | null;
  isDefault: boolean;
  order: number;
}

export interface CategoryWeb {
  id: string;
  name: string;
  symbol: string | null;
  order: number;
}

export interface FieldDefinitionWeb {
  id: string;
  fieldName: string;
  fieldType: number;
  label: string;
  isSortable: boolean;
  isFilterable: boolean;
}

export interface SummaryConfigurationWeb {
  showGroupSummary: boolean;
  showTotalSummary: boolean;
  groupSummaryFields: SummaryFieldWeb[];
  totalSummaryFields: SummaryFieldWeb[];
}

export interface SummaryFieldWeb {
  fieldId: string;
  fieldName: string;
  fieldLabel: string;
  fieldSource: number; // 0=System, 1=Calculated, 2=Generic
  order: number;
}

export interface UiConfigurationWeb {
  groupColumns: ColumnConfigurationWeb[];
  itemColumns: ColumnConfigurationWeb[];
}

// DTO do command update - tylko kolejność kolumn
export interface UiConfigurationDto {
  /** @deprecated Używaj groupColumnLayout/itemColumnLayout — stare API z pojedynczą listą GUID-ów */
  columnLayout?: string[];
  /** Kolejność kolumn dla pól grupy (etapu) — lista GUID-ów pól */
  groupColumnLayout?: string[];
  /** Kolejność kolumn dla pól pozycji — lista GUID-ów pól */
  itemColumnLayout?: string[];
}

export interface ColumnConfigurationWeb {
  fieldId: string;
  fieldName: string;
  fieldType: number;
  fieldLabel: string;
  fieldScope: number; // 0=GroupHeader, 1=SystemField, 2=CalculatedField, 3=GenericField
  order: number;
}

/**
 * Konwertuje CostEstimateDetailsWeb (nowy format z API) na CostEstimateDataModel (stary format dla CostEstimateExcelView)
 */
export function convertDetailsWebToDataModel(
  details: CostEstimateDetailsWeb,
  templateStructure: CostEstimateTemplateStructureWeb
): CostEstimateDataModel {
  return {
    groups: details.rootGroups.map((group) => convertGroupWebToGroup(group, templateStructure)),
    totals: {
      totalNet: details.totalNet || 0,
      totalGross: details.totalGross || 0,
      totalVat: details.totalVat || 0,
    },
    metadata: {
      lastModified: details.updatedAt || details.createdAt,
      lastModifiedBy: details.ownerName,
      schemaVersion: 1,
    },
  };
}

/**
 * Konwertuje CostEstimateGroupWeb na CostEstimateGroup
 */
function convertGroupWebToGroup(
  groupWeb: CostEstimateGroupWeb,
  templateStructure: CostEstimateTemplateStructureWeb
): CostEstimateGroup {
  // Konwertuj fieldValues na headerValues (klucz-wartość)
  const headerValues: Record<string, any> = {};
  groupWeb.fieldValues.forEach((fv) => {
    // Znajdź definicję pola w template
    const fieldDef = templateStructure.groupHeaderFields.find((f) => f.id === fv.fieldDefinitionId);
    if (fieldDef) {
      // Użyj fieldName jako klucza
      headerValues[fieldDef.fieldName] = getFieldValueAsString(fv);
    }
  });

  return {
    id: groupWeb.id,
    parentId: groupWeb.parentGroupId,
    level: groupWeb.level,
    order: groupWeb.order,
    headerValues,
    workScopes: (groupWeb.items || []).map((item: CostEstimateItemWeb) =>
      convertWorkScopeItemWebToWorkScope(item, templateStructure)
    ),
    subGroups: groupWeb.childGroups.map((child) => convertGroupWebToGroup(child, templateStructure)),
    totalNet: groupWeb.totalNet,
    totalGross: groupWeb.totalGross,
    totalVat: groupWeb.totalVat,
    lastCalculatedAt: groupWeb.lastCalculatedAt,
  };
}

/**
 * Konwertuje CostEstimateItemWeb na CostEstimateWorkScope
 */
function convertWorkScopeItemWebToWorkScope(
  itemWeb: CostEstimateItemWeb,
  templateStructure: CostEstimateTemplateStructureWeb
): CostEstimateWorkScope {
  const calculatedFieldValues: Record<string, any> = {};
  const genericFieldValues: Record<string, any> = {};

  itemWeb.fieldValues.forEach((fv) => {
    // Use fieldScope to determine which collection to search
    if (fv.fieldScope === 2) { // ItemCalculated
      const fieldDef = templateStructure.calculatedFields.find((f) => f.id === fv.fieldDefinitionId);
      if (fieldDef) {
        calculatedFieldValues[fieldDef.fieldName] = getFieldValueAsString(fv);
      }
    } else if (fv.fieldScope === 3) { // ItemGeneric
      const fieldDef = templateStructure.genericFields.find((f) => f.id === fv.fieldDefinitionId);
      if (fieldDef) {
        genericFieldValues[fieldDef.fieldName] = getFieldValueAsString(fv);
      }
    } else if (fv.fieldScope === 1) { // ItemSystem
      const fieldDef = templateStructure.systemFields.find((f) => f.id === fv.fieldDefinitionId);
      if (fieldDef) {
        // System fields go to calculatedFieldValues for CostEstimateExcelView compatibility
        calculatedFieldValues[fieldDef.fieldName] = getFieldValueAsString(fv);
      }
    }
  });

  return {
    id: itemWeb.id,
    order: itemWeb.order,
    calculatedFieldValues,
    genericFieldValues,
  };
}

/**
 * Konwertuje CostEstimateDataModel (z CostEstimateExcelView) z powrotem na UpdateCostEstimateDto dla API
 */
export function convertDataModelToUpdateDto(
  dataModel: CostEstimateDataModel,
  details: CostEstimateDetailsWeb,
  templateStructure: CostEstimateTemplateStructureWeb
): UpdateCostEstimateDto {
  return {
    name: details.name,
    description: details.description,
    status: details.status,
    rootGroups: dataModel.groups.map((group) => convertGroupToGroupDto(group, templateStructure)),
  };
}

/**
 * Konwertuje CostEstimateGroup na CostEstimateGroupDto
 */
function convertGroupToGroupDto(
  group: CostEstimateGroup,
  templateStructure: CostEstimateTemplateStructureWeb
): CostEstimateGroupDto {
  // Konwertuj headerValues z powrotem na fieldValues
  const fieldValues = templateStructure.groupHeaderFields.map((fieldDef) => {
    const value = group.headerValues[fieldDef.fieldName];

    return {
      fieldDefinitionId: fieldDef.id,
      value: value !== undefined && value !== null ? String(value) : undefined,
    };
  }).filter((fv) => fv.value !== undefined);

  // Dla nowych grup (temp_*) nie wysyłamy id - backend wygeneruje GUID
  const isNewGroup = group.id?.startsWith('temp_');
  // Dla parentId - jeśli jest temp_* lub undefined/null, wysyłamy undefined (backend przyjmie jako null)
  const isNewParent = group.parentId?.startsWith('temp_');
  
  return {
    id: isNewGroup ? undefined : group.id,
    parentGroupId: (!group.parentId || isNewParent) ? undefined : group.parentId,
    level: group.level,
    order: group.order,
    fieldValues,
    items: (group.workScopes || []).map((ws) =>
      convertWorkScopeToItemDto(ws, templateStructure)
    ),
    childGroups: (group.subGroups || []).map((child) => convertGroupToGroupDto(child, templateStructure)),
  };
}

/**
 * Konwertuje CostEstimateWorkScope na CostEstimateItemDto
 */
function convertWorkScopeToItemDto(
  workScope: CostEstimateWorkScope,
  templateStructure: CostEstimateTemplateStructureWeb
): CostEstimateItemDto {
  const fieldValues: { fieldDefinitionId: string; value?: string }[] = [];

  // Konwertuj calculatedFieldValues
  Object.keys(workScope.calculatedFieldValues || {}).forEach((fieldName) => {
    // Sprawdź czy to calculated field
    const calculatedFieldDef = templateStructure.calculatedFields.find((f) => f.fieldName === fieldName);
    if (calculatedFieldDef) {
      fieldValues.push({
        fieldDefinitionId: calculatedFieldDef.id,
        value: workScope.calculatedFieldValues[fieldName],
      });
      return;
    }

    // Sprawdź czy to system field
    const systemFieldDef = templateStructure.systemFields.find((f) => f.fieldName === fieldName);
    if (systemFieldDef) {
      fieldValues.push({
        fieldDefinitionId: systemFieldDef.id,
        value: workScope.calculatedFieldValues[fieldName],
      });
    }
  });

  // Konwertuj genericFieldValues
  Object.keys(workScope.genericFieldValues || {}).forEach((fieldName) => {
    const fieldDef = templateStructure.genericFields.find((f) => f.fieldName === fieldName);
    if (fieldDef) {
      fieldValues.push({
        fieldDefinitionId: fieldDef.id,
        value: workScope.genericFieldValues[fieldName],
      });
    }
  });

  // Dla nowych items (temp_*) nie wysyłamy id - backend wygeneruje GUID
  const isNewItem = workScope.id?.startsWith('temp_');
  
  return {
    id: isNewItem ? undefined : workScope.id,
    order: workScope.order,
    relationType: (workScope as any).relationType ?? 0,
    fieldValues,
  };
}

/**
 * Konwertuje strukturę template z backendu na CostEstimateTemplateDto (dla CostEstimateExcelView)
 */
export function convertTemplateStructureToTemplateDto(
  templateId: string,
  templateName: string,
  templateStructureWeb: CostEstimateTemplateStructureWeb,
  currencyCode?: string,
  ownerId?: string,
  ownerName?: string
): CostEstimateTemplateDto {
  // Konwertuj uproszczone FieldDefinitionWeb na rozbudowane definicje dla CostEstimateExcelView
  const templateStructure = {
    canAddGroups: templateStructureWeb.canAddGroups,
    canBranchGroups: templateStructureWeb.canBranchGroups,
    maxGroupLevel: templateStructureWeb.maxGroupLevel,
    autoNumberGroups: templateStructureWeb.autoNumberGroups,
    groupNumberFormat: templateStructureWeb.groupNumberFormat,
    currencies: templateStructureWeb.currencies.map((c) => ({
      id: c.id,
      code: c.code,
      name: c.name,
      symbol: c.symbol,
      isDefault: c.isDefault,
    })),
    units: templateStructureWeb.units.map((u) => ({
      id: u.id,
      code: u.code,
      name: u.name,
      symbol: u.symbol,
      isDefault: u.isDefault,
    })),
    groupHeaderFields: templateStructureWeb.groupHeaderFields.map((f) => ({
      id: f.id,
      type: f.fieldType,
      customLabel: f.label !== getDefaultGroupHeaderLabel(f.fieldType) ? f.label : undefined,
      label: f.label,
      fieldType: f.fieldType,
      isRequired: false, // API nie zwraca, zakładamy domyślne
      isVisible: true,
      visible: true,
      order: 0, // API nie zwraca order dla pojedynczych pól
      isReadOnly: false,
      helpText: undefined,
    })),
    systemFields: templateStructureWeb.systemFields.map((f) => ({
      id: f.id,
      name: f.fieldName,
      label: f.label,
      type: f.fieldType,
      isRequired: false,
      isVisible: true,
      visible: true,
      order: 0,
      helpText: undefined,
    })),
    calculatedFields: templateStructureWeb.calculatedFields.map((f) => ({
      id: f.id,
      name: f.fieldName,
      label: f.label,
      type: f.fieldType,
      formula: undefined,
      isRequired: false,
      isVisible: true,
      visible: true,
      order: 0,
      helpText: undefined,
      unit: undefined,
    })),
    genericFields: templateStructureWeb.genericFields.map((f) => ({
      id: f.id,
      name: f.fieldName,
      label: f.label,
      type: f.fieldType,
      isRequired: false,
      isVisible: true,
      visible: true,
      order: 0,
      defaultValue: undefined,
      helpText: undefined,
    })),
    summaryConfiguration: templateStructureWeb.summaryConfiguration,
    uiConfiguration: templateStructureWeb.uiConfiguration,
  };

  return {
    id: templateId,
    name: templateName,
    currency: currencyCode,
    templateStructure: templateStructure as any,
    createdAt: new Date().toISOString(),
    ownerId: ownerId || '',
    ownerName: ownerName || '',
  };
}

/**
 * Pomocnicza funkcja do uzyskania domyślnej etykiety dla pola group header
 */
function getDefaultGroupHeaderLabel(fieldType: number): string {
  switch (fieldType) {
    case 0: return 'Nazwa etapu';
    case 1: return 'Opis';
    case 2: return 'Numer';
    case 3: return 'Data rozpoczęcia';
    case 4: return 'Data zakończenia';
    case 5: return 'Status';
    case 6: return 'Notatki';
    case 7: return 'Odpowiedzialny';
    case 8: return 'Budżet';
    case 9: return 'Priorytet';
    default: return 'Pole';
  }
}
