import React, { useMemo, useState, useEffect } from "react";
import {
  Box,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Text,
  VStack,
  HStack,
  Badge,
  IconButton,
  Button,
  Tooltip,
  Input,
  Checkbox,
} from "@chakra-ui/react";
import { Plus, Trash2, Check, ChevronDown, ChevronRight } from "lucide-react";
import type {
  CostEstimateDataModel,
  CostEstimateGroup,
  CostEstimateWorkScope,
  CostEstimateCollectionItem,
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  GroupHeaderFieldDefinition,
} from "../types/costEstimate.types";
import { GroupHeaderFieldType, GenericFieldType, type CostEstimateTemplateDto } from "../types/costEstimate.types";
import { calculateWorkScope, canAutoCalculate } from "../utils/calculationEngine";
import {
  CalculatedFieldRenderer,
  GenericFieldRenderer,
  GroupHeaderFieldRenderer,
  getDefaultGroupHeaderLabel,
} from "./FieldRenderer";

interface CostEstimateViewerProps {
  dataModel: CostEstimateDataModel;
  template: CostEstimateTemplateDto;
  onCollectionSelectionChange?: (
    groupId: string,
    workScopeId: string,
    collectionFieldName: string,
    selectedItemId: string | null
  ) => void;
  readOnly?: boolean;
  editable?: boolean;
  onDataChange?: (dataModel: CostEstimateDataModel) => void;
  onAddGroup?: () => void;
  onDeleteGroup?: (groupId: string) => void;
  onAddSubGroup?: (groupId: string) => void;
  onAddWorkScope?: (groupId: string) => void;
  onDeleteWorkScope?: (groupId: string, workScopeId: string) => void;
  onAddCollectionItem?: (
    groupId: string,
    workScopeId: string,
    collectionFieldName: string
  ) => void;
  onDeleteCollectionItem?: (
    groupId: string,
    workScopeId: string,
    collectionFieldName: string,
    itemId: string
  ) => void;
}

// Typ dla wiersza w płaskiej strukturze
interface FlatRow {
  type: "group" | "subgroup" | "workscope" | "subtotal";
  level: number;
  groupId?: string;
  groupName?: string;
  groupNumber?: string;
  group?: CostEstimateGroup;
  workScope?: CostEstimateWorkScope;
  workScopeIndex?: number;
  totals?: Record<string, number>;
}

// Typ dla rozwiniętych podpól kolekcji
interface ExpandedNestedField {
  collectionField: GenericFieldDefinition;
  nestedField: CalculatedFieldDefinition | GenericFieldDefinition;
  fieldType: 'collection-calculated' | 'collection-generic';
  fullName: string; // np. "field_gen_2.nestedCalc_1"
  label: string;
  order: number;
}

// Helper do sortowania pól według columnLayout
function sortFieldsByLayout<T extends { name: string; order: number }>(
  fields: T[],
  columnLayout?: string[]
): T[] {
  if (!columnLayout || columnLayout.length === 0) {
    return [...fields].sort((a, b) => a.order - b.order);
  }

  return [...fields].sort((a, b) => {
    const indexA = columnLayout.indexOf(a.name);
    const indexB = columnLayout.indexOf(b.name);

    if (indexA === -1 && indexB === -1) return a.order - b.order;
    if (indexA === -1) return 1;
    if (indexB === -1) return -1;

    return indexA - indexB;
  });
}

// Helper do sortowania pól nagłówkowych grup
// Pola nagłówków grup sortujemy zawsze po order, nie używamy columnLayout
function sortHeaderFieldsByLayout(
  fields: GroupHeaderFieldDefinition[],
  columnLayout?: string[]
): GroupHeaderFieldDefinition[] {
  return [...fields].sort((a, b) => a.order - b.order);
}

/**
 * Pobiera sumy grupy z backendu
 * Backend oblicza totalNet, totalGross, totalVat - używamy tych wartości
 */
function getGroupTotals(
  group: CostEstimateGroup,
  summaryConfig?: any,
  calculatedFields?: CalculatedFieldDefinition[]
): Record<string, number> {
  const totals: Record<string, number> = {};
  
  // Jeśli backend zwrócił summaryTotals (nowa struktura), użyj ich bezpośrednio
  if (group.summaryTotals) {
    return group.summaryTotals;
  }

  // Fallback: Mapuj stare nazwy (totalNet/totalGross/totalVat) na GUIDy z groupSummaryFields
  if (summaryConfig?.groupSummaryFields && calculatedFields) {
    // Znajdź fieldName (GUID) dla każdego typu pola
    const fieldMap: Record<number, string> = {}; // fieldType -> fieldName
    
    summaryConfig.groupSummaryFields.forEach((summaryField: any) => {
      if (summaryField.fieldType !== undefined && summaryField.fieldName) {
        fieldMap[summaryField.fieldType] = summaryField.fieldName;
      }
    });

    // Mapuj wartości z group.totalNet/totalGross/totalVat na odpowiednie GUIDy
    // FieldType: 203=ValueNet, 204=ValueGross, 206=TotalVat
    if (group.totalNet !== undefined && fieldMap[203]) {
      totals[fieldMap[203]] = group.totalNet;
    }
    if (group.totalGross !== undefined && fieldMap[204]) {
      totals[fieldMap[204]] = group.totalGross;
    }
    if (group.totalVat !== undefined && fieldMap[206]) {
      totals[fieldMap[206]] = group.totalVat;
    }
  }

  // Dodatkowy fallback: użyj groupTotals (deprecated)
  if (Object.keys(totals).length === 0 && group.groupTotals) {
    return group.groupTotals;
  }

  return totals;
}

// Funkcja do spłaszczenia struktury grup do wierszy tabeli
function flattenGroups(
  groups: CostEstimateGroup[],
  level: number,
  calculatedFields: CalculatedFieldDefinition[],
  genericFields: GenericFieldDefinition[],
  summaryConfig: any,
  collapsedGroups: Set<string>,
  parentNumber: string = ""
): FlatRow[] {
  const rows: FlatRow[] = [];

  groups.forEach((group, groupIndex) => {
    const groupNumber = parentNumber
      ? `${parentNumber}.${groupIndex + 1}`
      : `${groupIndex + 1}`;

    const groupName =
      group.headerValues["GroupName"] ||
      group.headerValues[GroupHeaderFieldType[GroupHeaderFieldType.GroupName]] ||
      "Etap";

    // Pobierz sumy grupy z backendu
    const totals =
      summaryConfig?.showGroupSummary && summaryConfig.groupSummaryFields?.length > 0
        ? getGroupTotals(group, summaryConfig, calculatedFields)
        : {};

    // Wiersz nagłówka grupy z sumami
    rows.push({
      type: level === 0 ? "group" : "subgroup",
      level,
      groupId: group.id,
      groupName,
      groupNumber,
      group,
      totals, // Dodaj sumy do wiersza grupy
    });

    // Jeśli grupa jest zwinięta, nie pokazuj jej zawartości
    const isCollapsed = collapsedGroups.has(group.id);
    if (isCollapsed) {
      return; // Pomiń work scopes i podgrupy
    }

    // Work scopes - używamy bezpośrednio, bo są już przeliczone przez recalculateAll
    group.workScopes.forEach((ws, idx) => {
      rows.push({
        type: "workscope",
        level: level + 1,
        groupId: group.id,
        workScope: ws,
        workScopeIndex: idx + 1,
      });
    });

    // Rekurencyjnie dodaj podgrupy
    if (group.subGroups && group.subGroups.length > 0) {
      const subRows = flattenGroups(
        group.subGroups,
        level + 1,
        calculatedFields,
        genericFields,
        summaryConfig,
        collapsedGroups,
        groupNumber
      );
      rows.push(...subRows);
    }
  });

  return rows;
}

// Helper do sprawdzania czy można dodać podgrupę na danym poziomie
function canAddSubGroup(
  groupId: string,
  groups: CostEstimateGroup[],
  maxNestingLevel: number | undefined
): boolean {
  
  if (!maxNestingLevel || maxNestingLevel === 0) {
    return true; // Brak limitu
  }

  // Funkcja rekurencyjna do znalezienia poziomu grupy (zaczynamy od 1)
  const findGroupLevel = (
    targetId: string,
    currentGroups: CostEstimateGroup[],
    currentLevel: number
  ): number | null => {
    for (const group of currentGroups) {
      if (group.id === targetId) {
        return currentLevel;
      }
      if (group.subGroups && group.subGroups.length > 0) {
        const foundLevel = findGroupLevel(targetId, group.subGroups, currentLevel + 1);
        if (foundLevel !== null) {
          return foundLevel;
        }
      }
    }
    return null;
  };

  // Top-level grupy są na poziomie 1, więc zaczynamy od 1
  const currentLevel = findGroupLevel(groupId, groups, 1);
  
  if (currentLevel === null) {
    return false;
  }

  // Jeśli maxNestingLevel = 3, to można mieć grupy na poziomach 1, 2, 3
  // Więc podgrupę można dodać tylko jeśli currentLevel < maxNestingLevel
  const canAdd = currentLevel < maxNestingLevel;
  return canAdd;
}

export const CostEstimateExcelView: React.FC<CostEstimateViewerProps> = ({
  dataModel,
  template,
  onCollectionSelectionChange,
  readOnly = true,
  editable = false,
  onDataChange,
  onAddGroup,
  onDeleteGroup,
  onAddSubGroup,
  onAddWorkScope,
  onDeleteWorkScope,
  onAddCollectionItem,
  onDeleteCollectionItem,
}) => {
  // Stan dla zwijania/rozwijania grup
  const [collapsedGroups, setCollapsedGroups] = React.useState<Set<string>>(new Set());

  // Funkcja do przełączania stanu zwinięcia grupy
  const toggleGroupCollapse = (groupId: string) => {
    setCollapsedGroups((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(groupId)) {
        newSet.delete(groupId);
      } else {
        newSet.add(groupId);
      }
      return newSet;
    });
  };

  // CRITICAL FIX: Używaj useRef do trzymania AKTUALNEGO stanu dataModel
  // React batching powodował że dataModel z props był stary
  const dataModelRef = React.useRef(dataModel);
  React.useEffect(() => {
    dataModelRef.current = dataModel;
  }, [dataModel]);
  
  const calculatedFields =
    template.templateStructure?.workScopeFieldsDefinition?.calculatedFields || [];
  const genericFields =
    template.templateStructure?.workScopeFieldsDefinition?.genericFields || [];
  const groupHeaderFields =
    template.templateStructure?.groupDefinition?.headerFields || [];

  // Pobierz columnLayout z UiConfigurationWeb
  let columnLayout: string[] | undefined;
  if (template.templateStructure?.uiConfiguration?.columns) {
    columnLayout = template.templateStructure.uiConfiguration.columns
      .filter(col => col.isVisible)
      .sort((a, b) => a.order - b.order)
      .map(col => col.fieldName);
  }
  
  const summaryConfig = template.templateStructure?.summaryConfiguration;

  // Buduj unified list wszystkich kolumn (nagłówki grup + pola zakresów prac)
  // posortowaną według columnLayout - może być wymieszana kolejność!
  type ColumnDef =
    | { fieldType: 'groupHeader'; field: GroupHeaderFieldDefinition }
    | { fieldType: 'calculated'; field: CalculatedFieldDefinition }
    | { fieldType: 'generic'; field: GenericFieldDefinition }
    | { fieldType: 'collection-calculated'; collectionField: GenericFieldDefinition; nestedField: CalculatedFieldDefinition; fullName: string }
    | { fieldType: 'collection-generic'; collectionField: GenericFieldDefinition; nestedField: GenericFieldDefinition; fullName: string };

  const allColumns: ColumnDef[] = [];

  // Pomocnicza funkcja rozwijająca pole kolekcji na kolumny podpól
  const expandCollectionField = (collectionField: GenericFieldDefinition): void => {
    if (!collectionField.nestedFields) return;
    (collectionField.nestedFields.calculatedFields || []).forEach(nestedCalculatedField =>
      allColumns.push({ fieldType: 'collection-calculated', collectionField, nestedField: nestedCalculatedField, fullName: `${collectionField.name}.${nestedCalculatedField.name}` })
    );
    (collectionField.nestedFields.genericFields || []).forEach(nestedGenericField =>
      allColumns.push({ fieldType: 'collection-generic', collectionField, nestedField: nestedGenericField, fullName: `${collectionField.name}.${nestedGenericField.name}` })
    );
  };

  if (!columnLayout || columnLayout.length === 0) {
    // Brak columnLayout - użyj domyślnej kolejności:
    // 1. Nagłówki grup (po order)
    // 2. Pola zakresów prac (po order)
    groupHeaderFields
      .filter((f) => f.visible)
      .sort((a, b) => a.order - b.order)
      .forEach(f => allColumns.push({ fieldType: 'groupHeader', field: f }));

    calculatedFields
      .filter((f) => f.visible)
      .sort((a, b) => a.order - b.order)
      .forEach(f => allColumns.push({ fieldType: 'calculated', field: f }));

    genericFields
      .filter((f) => f.visible)
      .sort((a, b) => a.order - b.order)
      .forEach(f => {
        if (f.type === GenericFieldType.Collection && f.nestedFields) {
          expandCollectionField(f);
        } else {
          allColumns.push({ fieldType: 'generic', field: f });
        }
      });
  } else {
    // Iteruj przez columnLayout i buduj unified list
    columnLayout.forEach(fieldName => {
      // Sprawdź czy to nagłówek grupy
      const groupHeaderField = groupHeaderFields.find(f => {
        const typeName = GroupHeaderFieldType[f.type];
        return typeName === fieldName && f.visible;
      });
      if (groupHeaderField) {
        allColumns.push({ fieldType: 'groupHeader', field: groupHeaderField });
        return;
      }

      // Sprawdź czy to pole kalkulowane
      const calcField = calculatedFields.find(f => f.name === fieldName && f.visible);
      if (calcField) {
        allColumns.push({ fieldType: 'calculated', field: calcField });
        return;
      }
      
      // Sprawdź czy to pole generyczne — kolekcje rozwijamy na podpola
      const genField = genericFields.find(f => f.name === fieldName && f.visible);
      if (genField) {
        if (genField.type === GenericFieldType.Collection && genField.nestedFields) {
          expandCollectionField(genField);
        } else {
          allColumns.push({ fieldType: 'generic', field: genField });
        }
        return;
      }
    });
  }

  // Spłaszcz strukturę do wierszy tabeli
  const flatRows = useMemo(() => {
    return flattenGroups(
      dataModel.groups,
      0,
      calculatedFields,
      genericFields,
      summaryConfig,
      collapsedGroups
    );
  }, [dataModel.groups, calculatedFields, genericFields, summaryConfig, collapsedGroups]);

  // Oblicz całkowite sumy
  const grandTotals = useMemo(() => {
    const totals: Record<string, number> = {};

    if (
      summaryConfig?.showTotalSummary &&
      summaryConfig.totalSummaryFields?.length > 0
    ) {
      // Inicjalizuj wszystkie pola z totalSummaryFields
      summaryConfig.totalSummaryFields.forEach((field) => {
        totals[field.fieldName] = 0;
      });

      // Funkcja rekurencyjna do sumowania grup i podgrup
      const sumGroupRecursively = (group: CostEstimateGroup) => {
        // Sumuj work scopes
        group.workScopes.forEach((ws) => {
          summaryConfig.totalSummaryFields.forEach((field) => {
            const value = ws.calculatedFieldValues[field.fieldName];
            if (typeof value === 'number') {
              totals[field.fieldName] += value;
            }
          });
        });

        // Sumuj podgrupy rekurencyjnie
        if (group.subGroups && group.subGroups.length > 0) {
          group.subGroups.forEach((subGroup) => {
            sumGroupRecursively(subGroup);
          });
        }
      };

      // Sumuj wszystkie grupy top-level
      dataModel.groups.forEach((group) => {
        sumGroupRecursively(group);
      });
    }

    return totals;
  }, [dataModel.groups, calculatedFields, genericFields, summaryConfig]);

  // Helper do aktualizacji grupy - zwraca nowy dataModel zamiast wywoływać onDataChange
  const updateGroupInModel = (
    model: CostEstimateDataModel,
    groupId: string,
    updater: (group: CostEstimateGroup) => CostEstimateGroup
  ): CostEstimateDataModel => {
    const updateGroupsRecursive = (groups: CostEstimateGroup[]): CostEstimateGroup[] => {
      return groups.map((group) => {
        if (group.id === groupId) {
          return updater(group);
        }
        if (group.subGroups) {
          return { ...group, subGroups: updateGroupsRecursive(group.subGroups) };
        }
        return group;
      });
    };

    return {
      ...model,
      groups: updateGroupsRecursive(model.groups),
    };
  };

  // Helper do aktualizacji grupy
  const updateGroup = (
    groupId: string,
    updater: (group: CostEstimateGroup) => CostEstimateGroup
  ) => {
    if (!onDataChange) return;
    
    // Użyj AKTUALNEGO stanu z ref, nie z props
    const updated = updateGroupInModel(dataModelRef.current, groupId, updater);
    onDataChange(updated);
  };

  // Helper do aktualizacji workScope
  const updateWorkScope = (
    groupId: string,
    workScopeId: string,
    updater: (ws: CostEstimateWorkScope) => CostEstimateWorkScope
  ) => {
    if (!onDataChange) return;
    
    // CRITICAL FIX: Użyj AKTUALNEGO stanu z ref zamiast starego z props
    const updated = updateGroupInModel(dataModelRef.current, groupId, (group) => ({
      ...group,
      workScopes: group.workScopes.map((ws) =>
        ws.id === workScopeId ? updater(ws) : ws
      ),
    }));
    
    onDataChange(updated);
  };

  // Helper do aktualizacji itemu w kolekcji
  const updateCollectionItem = (
    groupId: string,
    workScopeId: string,
    collectionFieldName: string,
    itemId: string,
    updater: (item: CostEstimateCollectionItem) => CostEstimateCollectionItem
  ) => {
    updateWorkScope(groupId, workScopeId, (ws) => {
      const updatedItems = (ws.collectionFieldValues?.[collectionFieldName] || []).map(
        (item) => (item.id === itemId ? updater(item) : item)
      );
      
      // Sprawdź czy edytowany item jest zaznaczony (isSelected)
      const selectedItem = updatedItems.find((item) => item.id === itemId && item.isSelected);
      
      let updatedCalculatedValues = { ...ws.calculatedFieldValues };
      let lockedFields = ws.lockedFields ? [...ws.lockedFields] : [];
      
      // Jeśli edytowany item jest zaznaczony, skopiuj zaktualizowane wartości
      if (selectedItem) {
        const collectionField = genericFields.find((f) => f.name === collectionFieldName);
        const nestedCalcFields = collectionField?.nestedFields?.calculatedFields || [];
        
        nestedCalcFields.forEach((nestedField) => {
          const nestedValue = selectedItem.calculatedFieldValues?.[nestedField.name];
          const mainField = calculatedFields.find((f) => f.type === nestedField.type);
          
          if (mainField && nestedValue !== undefined && nestedValue !== null) {
            updatedCalculatedValues[mainField.name] = nestedValue;
            if (!lockedFields.includes(mainField.name)) {
              lockedFields.push(mainField.name);
            }
          }
        });
      }
      
      return {
        ...ws,
        calculatedFieldValues: updatedCalculatedValues,
        lockedFields: lockedFields.length > 0 ? lockedFields : undefined,
        collectionFieldValues: {
          ...ws.collectionFieldValues,
          [collectionFieldName]: updatedItems,
        },
      };
    });
  };

  // Helper do zaznaczania itemu w kolekcji i kopiowania wartości
  const handleCollectionItemSelect = (
    groupId: string,
    workScopeId: string,
    collectionFieldName: string,
    itemId: string,
    isSelected: boolean,
    collectionField: GenericFieldDefinition
  ) => {
    if (!onDataChange) {
      return;
    }

    updateWorkScope(groupId, workScopeId, (ws) => {
      const items = ws.collectionFieldValues?.[collectionFieldName] || [];
      const selectedItem = items.find((i) => i.id === itemId);

      // Odznacz wszystkie inne itemy w tej kolekcji
      const updatedItems = items.map((item) => ({
        ...item,
        isSelected: item.id === itemId ? isSelected : false,
      }));

      const nestedCalcFields = collectionField.nestedFields?.calculatedFields || [];
      let updatedCalculatedValues = { ...ws.calculatedFieldValues };
      const lockedFields = ws.lockedFields ? [...ws.lockedFields] : [];
      
      // Przygotuj klucz do przechowywania oryginalnych wartości
      const backupKey = `_originalValues_${collectionFieldName}`;
      
      if (isSelected && selectedItem) {
        // ZAZNACZONO: zapisz oryginalne wartości i skopiuj z collection item
        const originalValues: Record<string, any> = {};
        
        nestedCalcFields.forEach((nestedField) => {
          const mainField = calculatedFields.find(f => f.type === nestedField.type);
          
          if (mainField) {
            // Zapisz oryginalną wartość
            originalValues[mainField.name] = updatedCalculatedValues[mainField.name];
            
            // Kopiuj wartość z collection item (nawet jeśli null lub undefined)
            const nestedValue = selectedItem.calculatedFieldValues?.[nestedField.name];
            updatedCalculatedValues[mainField.name] = nestedValue;
            
            // Blokuj pole przed auto-kalkulacją
            if (!lockedFields.includes(mainField.name)) {
              lockedFields.push(mainField.name);
            }
          }
        });
        
        // Zapisz oryginalne wartości w workScope
        (ws as any)[backupKey] = originalValues;
      } else {
        // ODZNACZONO: przywróć oryginalne wartości
        const originalValues = (ws as any)[backupKey];
        
        if (originalValues) {
          nestedCalcFields.forEach((nestedField) => {
            const mainField = calculatedFields.find(f => f.type === nestedField.type);
            
            if (mainField && mainField.name in originalValues) {
              // Przywróć oryginalną wartość
              updatedCalculatedValues[mainField.name] = originalValues[mainField.name];
            }
          });
        }
        
        // Usuń backup
        delete (ws as any)[backupKey];
      }

      // Zwróć zaktualizowany workScope BEZ przeliczania
      // Parent (CostEstimateEditor) wywoła recalculateAll i zachowa isSelected
      const updatedWs = {
        ...ws,
        calculatedFieldValues: updatedCalculatedValues,
        lockedFields: isSelected ? lockedFields : undefined,
        collectionFieldValues: {
          ...ws.collectionFieldValues,
          [collectionFieldName]: updatedItems,
        },
      };
      
      // Zachowaj lub usuń backup w zależności od stanu
      if (isSelected) {
        (updatedWs as any)[backupKey] = (ws as any)[backupKey];
      }

      return updatedWs;
    });

    // Wywołaj callback selekcji
    if (onCollectionSelectionChange) {
      onCollectionSelectionChange(
        groupId,
        workScopeId,
        collectionFieldName,
        isSelected ? itemId : null
      );
    }
  };

  // Budowanie nagłówka tabeli
  const renderTableHeader = () => {
    return (
      <Thead bgGradient="linear(to-r, primary.600, primary.700)" position="sticky" top={0} zIndex={10} shadow="md">
        <Tr>
          {/* Kolumna pozycji */}
          <Th
            borderRightWidth="2px"
            borderColor="primary.500"
            color="white"
            fontSize="xs"
            py={4}
            whiteSpace="nowrap"
            textTransform="uppercase"
            letterSpacing="wider"
            fontWeight="bold"
          >
            Pozycja
          </Th>

          {/* Wszystkie kolumny - nagłówki grup i pola work scope - posortowane według columnLayout */}
          {allColumns.map((item, idx) => {
            if (item.fieldType === 'groupHeader') {
              const field = item.field;
              return (
                <Th
                  key={`header-${field.type}`}
                  borderRightWidth="1px"
                  borderColor="primary.500"
                  color="white"
                  fontSize="sm"
                  py={3}
                  minW="150px"
                  whiteSpace="nowrap"
                >
                  {field.customLabel || getDefaultGroupHeaderLabel(field.type)}
                </Th>
              );
            } else if (item.fieldType === 'calculated') {
              const field = item.field;
              return (
                <Th
                  key={`calc-${field.name}`}
                  isNumeric
                  borderRightWidth="1px"
                  borderColor="primary.500"
                  color="white"
                  fontSize="sm"
                  py={3}
                  minW="120px"
                  whiteSpace="nowrap"
                >
                  {field.label}
                  {field.unit && (
                    <Text as="span" fontSize="xs" fontWeight="normal" ml={1}>
                      [{field.unit}]
                    </Text>
                  )}
                </Th>
              );
            } else if (item.fieldType === 'generic') {
              const field = item.field;
              return (
                <Th
                  key={`gen-${field.name}`}
                  borderRightWidth="1px"
                  borderColor="primary.500"
                  color="white"
                  fontSize="sm"
                  py={3}
                  minW="150px"
                  whiteSpace="nowrap"
                >
                  {field.label}
                </Th>
              );
            } else if (item.fieldType === 'collection-calculated') {
              const { nestedField, fullName } = item;
              
              // Sprawdź czy to pierwsze podpole tej kolekcji - jeśli tak, dodaj kolumnę akcji
              const isFirstFieldOfCollection = idx === 0 || 
                allColumns[idx - 1].fieldType === 'groupHeader' ||
                allColumns[idx - 1].fieldType === 'calculated' ||
                allColumns[idx - 1].fieldType === 'generic' ||
                (allColumns[idx - 1].fieldType.startsWith('collection-') && 
                 (allColumns[idx - 1] as any).collectionField.name !== item.collectionField.name);
              
              const headers = [];
              
              if (isFirstFieldOfCollection && editable) {
                headers.push(
                  <Th
                    key={`coll-action-${item.collectionField.name}`}
                    borderRightWidth="1px"
                    borderColor="primary.500"
                    color="white"
                    fontSize="xs"
                    py={3}
                    textAlign="center"
                    whiteSpace="nowrap"
                  >
                    Akcje
                  </Th>
                );
              }
              
              headers.push(
                <Th
                  key={fullName}
                  isNumeric
                  borderRightWidth="1px"
                  borderColor="primary.500"
                  color="white"
                  fontSize="sm"
                  py={3}
                  minW="120px"
                  whiteSpace="nowrap"
                >
                  {nestedField.label}
                  {nestedField.unit && (
                    <Text as="span" fontSize="xs" fontWeight="normal" ml={1}>
                      [{nestedField.unit}]
                    </Text>
                  )}
                </Th>
              );
              
              return headers;
            } else if (item.fieldType === 'collection-generic') {
              const { nestedField, fullName } = item;
              
              // Sprawdź czy to pierwsze podpole tej kolekcji
              const isFirstFieldOfCollection = idx === 0 || 
                allColumns[idx - 1].fieldType === 'groupHeader' ||
                allColumns[idx - 1].fieldType === 'calculated' ||
                allColumns[idx - 1].fieldType === 'generic' ||
                (allColumns[idx - 1].fieldType.startsWith('collection-') && 
                 (allColumns[idx - 1] as any).collectionField.name !== item.collectionField.name);
              
              const headers = [];
              
              if (isFirstFieldOfCollection && editable) {
                headers.push(
                  <Th
                    key={`coll-action-${item.collectionField.name}`}
                    borderRightWidth="1px"
                    borderColor="primary.500"
                    color="white"
                    fontSize="xs"
                    py={3}
                    textAlign="center"
                    whiteSpace="nowrap"
                  >
                    Akcje
                  </Th>
                );
              }
              
              headers.push(
                <Th
                  key={fullName}
                  borderRightWidth="1px"
                  borderColor="primary.500"
                  color="white"
                  fontSize="sm"
                  py={3}
                  minW="150px"
                  whiteSpace="nowrap"
                >
                  {nestedField.label}
                </Th>
              );
              
              return headers;
            }
            
            return null;
          })}

          {/* Kolumna akcji */}
          {editable && (
            <Th
              borderLeftWidth="2px"
              borderColor="primary.500"
              color="white"
              fontSize="sm"
              py={3}
              whiteSpace="nowrap"
            >
              Akcje
            </Th>
          )}
        </Tr>
      </Thead>
    );
  };

  return (
    <VStack spacing={6} align="stretch">
      {/* Header Info */}
      <Box bg="white" p={4} borderRadius="lg" shadow="sm" borderWidth="1px">
        <HStack justify="space-between">
          <Text fontSize="2xl" fontWeight="bold">
            Kosztorys
          </Text>
          <HStack spacing={3}>
            {editable && (
              <Tooltip 
                label={!onAddGroup ? "Szablon nie pozwala na dodawanie etapów" : ""}
                hasArrow
              >
                <Button
                  leftIcon={<Plus size={16} />}
                  size="sm"
                  colorScheme="green"
                  onClick={onAddGroup}
                  isDisabled={!onAddGroup}
                >
                  Dodaj etap
                </Button>
              </Tooltip>
            )}
          </HStack>
        </HStack>
      </Box>

      {/* Totals Summary */}
      {Object.keys(grandTotals).length > 0 && 
       summaryConfig?.showTotalSummary && 
       summaryConfig.totalSummaryFields?.length > 0 && (
        <Box bg="green.50" p={4} borderRadius="md" borderWidth="1px" borderColor="green.200">
          <HStack spacing={2} mb={3}>
            <Text fontWeight="bold" fontSize="lg">
              Podsumowanie całkowite
            </Text>
          </HStack>
          <VStack spacing={2} align="stretch">
            {summaryConfig.totalSummaryFields
              .filter((field) => field.fieldName in grandTotals)
              .map((field) => {
                const fieldName = field.fieldName;
                const value = grandTotals[fieldName];
                const fieldDef = calculatedFields.find((f: any) => f.name === fieldName);
                return (
                  <HStack key={fieldName} justify="space-between">
                    <Text fontWeight="bold" fontSize="md">
                      {fieldDef?.label || field.fieldLabel || fieldName}:
                    </Text>
                    <Text fontSize="lg" fontWeight="bold">
                      {value !== undefined && value !== null ? value.toFixed(2) : '-'}
                      {fieldDef?.unit && ` ${fieldDef.unit}`}
                      {template.currency && ` ${template.currency}`}
                    </Text>
                  </HStack>
                );
              })}
          </VStack>
        </Box>
      )}

      {/* Main Table */}
      {dataModel.groups.length === 0 ? (
        <Box
          p={12}
          textAlign="center"
          bg="gray.50"
          borderRadius="md"
          borderWidth="1px"
        >
          <VStack spacing={4}>
            <Text color="gray.500" fontSize="lg">
              Brak etapów w kosztorysie.
            </Text>
            {(() => {
              return editable;
            })() && (
              <Tooltip
                label={!onAddGroup ? "Szablon nie pozwala na dodawanie etapów" : ""}
                hasArrow
              >
                <Button
                  leftIcon={<Plus size={16} />}
                  size="md"
                  colorScheme="green"
                  onClick={onAddGroup}
                  isDisabled={!onAddGroup}
                >
                  Dodaj pierwszy etap
                </Button>
              </Tooltip>
            )}
          </VStack>
        </Box>
      ) : (
        <Box
          overflowX="auto"
          bg="white"
          borderRadius="xl"
          shadow="lg"
          borderWidth="1px"
          borderColor="gray.200"
        >
          <Table size="sm" variant="simple">
            {renderTableHeader()}
            <Tbody>
              {flatRows.map((row, idx) => {
                const indent = row.level * 24;

                if (row.type === "group" || row.type === "subgroup") {
                  const isGroup = row.type === "group";
                  const bgGradient = isGroup 
                    ? "linear(to-r, primary.50, primary.100)"
                    : "linear(to-r, action.50, action.100)";
                  const badgeColor = isGroup ? "blue" : "teal";

                  return (
                    <Tr
                      key={`${row.type}-${row.groupId}-${idx}`}
                      bgGradient={bgGradient}
                      borderTopWidth={isGroup ? "3px" : "2px"}
                      borderTopColor={isGroup ? "primary.400" : "action.300"}
                      _hover={{
                        transform: "scale(1.001)",
                        shadow: "md",
                        transition: "all 0.2s"
                      }}
                    >
                      {/* Kolumna pozycji - Ikona zwijania + Badge + numer grupy + akcje */}
                      <Td
                        borderRightWidth="1px"
                        borderRightColor="gray.200"
                        p={3}
                        pl={`${indent + 12}px`}
                        borderBottomWidth="2px"
                        borderBottomColor={isGroup ? "primary.200" : "action.200"}
                      >
                        <HStack spacing={2}>
                          <IconButton
                            aria-label={collapsedGroups.has(row.groupId!) ? "Rozwiń" : "Zwiń"}
                            icon={collapsedGroups.has(row.groupId!) ? <ChevronRight size={16} /> : <ChevronDown size={16} />}
                            size="xs"
                            variant="ghost"
                            onClick={() => toggleGroupCollapse(row.groupId!)}
                            _hover={{ bg: isGroup ? "primary.200" : "action.200" }}
                          />
                          <Badge
                            colorScheme={badgeColor}
                            fontSize="sm"
                            px={3}
                            py={1}
                            fontWeight="bold"
                            borderRadius="full"
                            shadow="sm"
                          >
                            {row.groupNumber}
                          </Badge>
                        </HStack>
                      </Td>

                      {/* Pola headerów grup i pól work scope - posortowane według columnLayout */}
                      {allColumns.map((item, colIdx) => {
                        if (item.fieldType === 'groupHeader') {
                          // Renderuj pole headera grupy
                          const field = item.field;
                          const fieldKey = GroupHeaderFieldType[field.type];
                          const value = row.group?.headerValues?.[fieldKey];

                          return (
                            <Td
                              key={`group-header-${field.type}`}
                              borderRightWidth="1px"
                              borderRightColor="gray.200"
                              py={2}
                              px={2}
                              borderBottomWidth="2px"
                              borderBottomColor={isGroup ? "primary.200" : "action.200"}
                            >
                              {editable && onDataChange ? (
                                <GroupHeaderFieldRenderer
                                  field={field}
                                  value={value}
                                  onChange={(newValue) => {
                                    updateGroup(row.groupId!, (group) => ({
                                      ...group,
                                      headerValues: {
                                        ...group.headerValues,
                                        [fieldKey]: newValue,
                                      },
                                    }));
                                  }}
                                  readOnly={false}
                                  compact={true}
                                />
                              ) : (
                                <Text fontSize="sm" fontWeight={field.type === GroupHeaderFieldType.GroupName ? "bold" : "normal"}>
                                  {value !== undefined && value !== null
                                    ? typeof value === "boolean"
                                      ? value
                                        ? "Tak"
                                        : "Nie"
                                      : String(value)
                                    : "-"}
                                </Text>
                              )}
                            </Td>
                          );
                        } else if (item.fieldType === 'collection-calculated' || item.fieldType === 'collection-generic') {
                          // Dla podpól kolekcji nie wyświetlamy sum w wierszach grup
                          const collectionField = item.collectionField;
                          const isFirstFieldOfCollection = colIdx === 0 ||
                            allColumns[colIdx - 1].fieldType === 'groupHeader' ||
                            allColumns[colIdx - 1].fieldType === 'calculated' ||
                            allColumns[colIdx - 1].fieldType === 'generic' ||
                            (allColumns[colIdx - 1].fieldType.startsWith('collection-') && 
                             (allColumns[colIdx - 1] as any).collectionField.name !== collectionField.name);
                          
                          const cells = [];
                          if (isFirstFieldOfCollection && editable) {
                            cells.push(
                              <Td
                                key={`empty-coll-action-${collectionField.name}`}
                                borderRightWidth="1px"
                                borderRightColor="gray.200"
                                borderBottomWidth="2px"
                                borderBottomColor={isGroup ? "primary.200" : "action.200"}
                              />
                            );
                          }
                          cells.push(
                            <Td
                              key={`empty-coll-${item.fullName}`}
                              borderRightWidth="1px"
                              borderRightColor="gray.200"
                              borderBottomWidth="2px"
                              borderBottomColor={isGroup ? "primary.200" : "action.200"}
                            />
                          );
                          return cells;
                        } else {
                          // Dla regularnych pól (calculated/generic) wyświetl sumy
                          const field = item.field;
                          const totalValue = row.totals?.[field.name];
                          return (
                            <Td
                              key={`group-${item.fieldType}-${field.name}`}
                              isNumeric={item.fieldType === 'calculated'}
                              borderRightWidth="1px"
                              borderRightColor="gray.200"
                              borderBottomWidth="2px"
                              borderBottomColor={isGroup ? "primary.200" : "action.200"}
                              fontWeight="bold"
                              color={isGroup ? "primary.700" : "action.700"}
                            >
                              {totalValue !== undefined && totalValue !== null ? (
                                <Text fontSize="sm">
                                  {typeof totalValue === "number"
                                    ? totalValue.toFixed(2)
                                    : String(totalValue)}
                                </Text>
                              ) : null}
                            </Td>
                          );
                        }
                      })}

                      {/* Kolumna akcji dla grupy */}
                      {editable && (
                        <Td
                          borderBottomWidth="2px"
                          borderBottomColor={isGroup ? "primary.200" : "action.200"}
                        >
                          <HStack spacing={1} justify="flex-start">
                            {onAddWorkScope && row.groupId && (
                              <Tooltip label="Dodaj pozycję" hasArrow>
                                <IconButton
                                  aria-label="Dodaj pozycję"
                                  icon={<Plus size={12} />}
                                  size="xs"
                                  colorScheme="primary"
                                  variant="solid"
                                  borderRadius="md"
                                  onClick={() => onAddWorkScope(row.groupId!)}
                                  _hover={{ transform: "scale(1.1)", shadow: "md" }}
                                  transition="all 0.2s"
                                />
                              </Tooltip>
                            )}
                            {onAddSubGroup &&
                              row.groupId &&
                              template.templateStructure?.canBranchGroups &&
                              canAddSubGroup(
                                row.groupId,
                                dataModel.groups,
                                template.templateStructure?.maxGroupLevel
                              ) && (
                                <Tooltip label="Dodaj podetap" hasArrow>
                                  <IconButton
                                    aria-label="Dodaj podetap"
                                    icon={<Plus size={12} />}
                                    size="xs"
                                    colorScheme="action"
                                    variant="solid"
                                    borderRadius="md"
                                    onClick={() => onAddSubGroup(row.groupId!)}
                                    _hover={{ transform: "scale(1.1)", shadow: "md" }}
                                    transition="all 0.2s"
                                  />
                                </Tooltip>
                              )}
                            {onDeleteGroup && row.groupId && (
                              <Tooltip
                                label={isGroup ? "Usuń etap" : "Usuń podetap"}
                                hasArrow
                              >
                                <IconButton
                                  aria-label={
                                    isGroup ? "Usuń etap" : "Usuń podetap"
                                  }
                                  icon={<Trash2 size={12} />}
                                  size="xs"
                                  colorScheme="red"
                                  variant="ghost"
                                  borderRadius="md"
                                  onClick={() => onDeleteGroup(row.groupId!)}
                                  _hover={{ transform: "scale(1.1)", bg: "red.50" }}
                                  transition="all 0.2s"
                                />
                              </Tooltip>
                            )}
                          </HStack>
                        </Td>
                      )}
                    </Tr>
                  );
                }

                if (row.type === "workscope" && row.workScope && row.groupId) {
                  return (
                    <Tr
                      key={`ws-${row.workScope.id}-${idx}`}
                      _hover={{
                        bg: editable ? "primary.50" : "gray.50",
                        transform: "translateX(2px)",
                        shadow: "sm",
                        transition: "all 0.2s ease",
                      }}
                      borderBottomWidth="1px"
                      borderBottomColor="gray.100"
                      transition="all 0.2s"
                    >
                      {/* Kolumna pozycji z numerem */}
                      <Td
                        borderRightWidth="2px"
                        borderRightColor="gray.300"
                        pl={`${indent + 8}px`}
                        py={3}
                        color="gray.700"
                        fontWeight="medium"
                        fontSize="sm"
                      >
                        {row.workScopeIndex}
                      </Td>

                      {/* Wszystkie kolumny - nagłówki grup i pola work scope - posortowane według columnLayout */}
                      {allColumns.map((item, colIdx) => {
                        if (item.fieldType === 'groupHeader') {
                          // Pola headerów grup - tylko widok (read-only) dla wierszy pozycji
                          const field = item.field;
                          const fieldKey = GroupHeaderFieldType[field.type];
                          const value = row.group?.headerValues?.[fieldKey];

                          return (
                            <Td
                              key={`ws-header-${field.type}`}
                              borderRightWidth="1px"
                              borderRightColor="gray.200"
                              py={2}
                              px={2}
                              bgGradient="linear(to-b, gray.50, gray.100)"
                            >
                              <Text fontSize="sm" color="gray.600">
                                {value !== undefined && value !== null
                                  ? typeof value === "boolean"
                                    ? value
                                      ? "Tak"
                                      : "Nie"
                                    : String(value)
                                  : "-"}
                              </Text>
                            </Td>
                          );
                        } else if (item.fieldType === 'collection-calculated' || item.fieldType === 'collection-generic') {
                          const collectionField = item.collectionField;
                          const nestedField = item.nestedField;
                          const collectionItems = row.workScope!.collectionFieldValues?.[collectionField.name] || [];
                          
                          // Sprawdź czy to pierwsze podpole - wtedy dodaj kolumnę akcji
                          const isFirstFieldOfCollection = colIdx === 0 ||
                            allColumns[colIdx - 1].fieldType === 'groupHeader' ||
                            allColumns[colIdx - 1].fieldType === 'calculated' ||
                            allColumns[colIdx - 1].fieldType === 'generic' ||
                            (allColumns[colIdx - 1].fieldType.startsWith('collection-') && 
                             (allColumns[colIdx - 1] as any).collectionField.name !== collectionField.name);
                          
                          const cells = [];
                          
                          // Kolumna akcji dla kolekcji
                          if (isFirstFieldOfCollection && editable && onDataChange) {
                            cells.push(
                              <Td
                                key={`coll-action-${collectionField.name}`}
                                borderRightWidth="1px"
                                borderRightColor="gray.200"
                                py={2}
                                px={2}
                                bg="level2.50"
                              >
                                <VStack spacing={0} align="center" width="100%">
                                  {collectionItems.map((cItem) => (
                                    <HStack key={cItem.id} width="100%" justifyContent="center" height="40px" spacing={1} alignItems="center" py={0.5}>
                                      {cItem.isSelected ? (
                                        <Box
                                          as="button"
                                          onClick={() => {
                                            handleCollectionItemSelect(
                                              row.groupId!,
                                              row.workScope!.id,
                                              collectionField.name,
                                              cItem.id,
                                              false,
                                              collectionField
                                            );
                                          }}
                                          p={1}
                                          borderRadius="md"
                                          bg="level2.500"
                                          color="white"
                                          cursor="pointer"
                                          _hover={{ bg: "level2.600" }}
                                          display="flex"
                                          alignItems="center"
                                          justifyContent="center"
                                        >
                                          <Check size={16} />
                                        </Box>
                                      ) : (
                                        <Checkbox
                                          size="sm"
                                          colorScheme="level2"
                                          isChecked={false}
                                          onChange={(e) => {
                                            handleCollectionItemSelect(
                                              row.groupId!,
                                              row.workScope!.id,
                                              collectionField.name,
                                              cItem.id,
                                              e.target.checked,
                                              collectionField
                                            );
                                          }}
                                          sx={{ "span": { bg: "white", borderColor: "gray.300" } }}
                                        />
                                      )}
                                      {onDeleteCollectionItem && (
                                        <IconButton
                                          aria-label="Usuń item"
                                          icon={<Trash2 size={12} />}
                                          size="xs"
                                          colorScheme="red"
                                          variant="ghost"
                                          borderRadius="md"
                                          _hover={{ transform: "scale(1.1)", bg: "red.50" }}
                                          transition="all 0.2s"
                                          onClick={() => onDeleteCollectionItem(row.groupId!, row.workScope!.id, collectionField.name, cItem.id)}
                                        />
                                      )}
                                    </HStack>
                                  ))}
                                  {/* Quasi-wiersz z przyciskiem dodawania */}
                                  {onAddCollectionItem && (
                                    <HStack width="100%" justifyContent="center" height="40px" spacing={1} alignItems="center" py={0.5}>
                                      <Tooltip label={`Dodaj ${collectionField.label}`} hasArrow>
                                        <IconButton
                                          aria-label={`Dodaj ${collectionField.label}`}
                                          icon={<Plus size={12} />}
                                          size="xs"
                                          colorScheme="level2"
                                          variant="solid"
                                          borderRadius="md"
                                          _hover={{ transform: "scale(1.1)", shadow: "md" }}
                                          transition="all 0.2s"
                                          onClick={() =>
                                            onAddCollectionItem(
                                              row.groupId!,
                                              row.workScope!.id,
                                              collectionField.name
                                            )
                                          }
                                        />
                                      </Tooltip>
                                    </HStack>
                                  )}
                                </VStack>
                              </Td>
                            );
                          }
                          
                          // Kolumna z wartościami podpola
                          const isCalc = item.fieldType === 'collection-calculated';
                          cells.push(
                            <Td
                              key={`ws-coll-${item.fullName}`}
                              isNumeric={isCalc}
                              borderRightWidth="1px"
                              borderRightColor="gray.200"
                              py={2}
                              px={2}
                              fontSize="sm"
                              bg="level2.50"
                            >
                              {collectionItems.length > 0 ? (
                                <VStack spacing={0} align={isCalc ? "end" : "start"} width="100%">
                                  {collectionItems.map((cItem) => {
                                    const itemValue = isCalc
                                      ? cItem.calculatedFieldValues?.[nestedField.name]
                                      : cItem.genericFieldValues?.[nestedField.name];
                                    
                                    let canAutoCalc = false;
                                    if (isCalc) {
                                      const nestedCalcFieldsAll = collectionField.nestedFields?.calculatedFields || [];
                                      const valuesByType: Record<number, any> = {};
                                      nestedCalcFieldsAll.forEach(f => {
                                        if (f.name in (cItem.calculatedFieldValues || {})) {
                                          valuesByType[f.type] = cItem.calculatedFieldValues![f.name];
                                        }
                                      });
                                      canAutoCalc = canAutoCalculate((nestedField as any).type, valuesByType);
                                    }
                                    
                                    return (
                                      <Box key={cItem.id} height="40px" display="flex" alignItems="center" width="100%" py={0.5}>
                                        {editable && onDataChange ? (
                                          isCalc ? (
                                            <CalculatedFieldRenderer
                                              field={nestedField as any}
                                              value={itemValue}
                                              onChange={(newValue) => {
                                                updateCollectionItem(row.groupId!, row.workScope!.id, collectionField.name, cItem.id, (item) => ({
                                                  ...item,
                                                  calculatedFieldValues: { ...item.calculatedFieldValues, [nestedField.name]: newValue },
                                                }));
                                              }}
                                              allValues={{ ...cItem.calculatedFieldValues, ...cItem.genericFieldValues }}
                                              readOnly={false}
                                              canAutoCalculate={canAutoCalc}
                                              compact
                                            />
                                          ) : (
                                            <GenericFieldRenderer
                                              field={nestedField as any}
                                              value={itemValue}
                                              onChange={(newValue) => {
                                                updateCollectionItem(row.groupId!, row.workScope!.id, collectionField.name, cItem.id, (item) => ({
                                                  ...item,
                                                  genericFieldValues: { ...item.genericFieldValues, [nestedField.name]: newValue },
                                                }));
                                              }}
                                              allValues={{ ...cItem.calculatedFieldValues, ...cItem.genericFieldValues }}
                                              readOnly={false}
                                              compact
                                            />
                                          )
                                        ) : (
                                          <Text fontSize="sm">
                                            {itemValue !== undefined && itemValue !== null
                                              ? typeof itemValue === "number"
                                                ? itemValue.toFixed(2)
                                                : typeof itemValue === "boolean"
                                                ? itemValue ? "Tak" : "Nie"
                                                : String(itemValue)
                                              : "-"}
                                          </Text>
                                        )}
                                      </Box>
                                    );
                                  })}
                                  {/* Quasi-wiersz pusty - aby wyrównać z przyciskiem Dodaj */}
                                  {onAddCollectionItem && (
                                    <Box height="40px" display="flex" alignItems="center" width="100%" py={0.5}>
                                      <Text fontSize="xs" color="gray.400">-</Text>
                                    </Box>
                                  )}
                                </VStack>
                              ) : (
                                <VStack spacing={0} align={isCalc ? "end" : "start"} width="100%">
                                  {/* Pusty wiersz z przyciskiem dodaj */}
                                  {onAddCollectionItem && (
                                    <Box height="40px" display="flex" alignItems="center" width="100%" py={0.5}>
                                      <Text fontSize="xs" color="gray.400">-</Text>
                                    </Box>
                                  )}
                                </VStack>
                              )}
                            </Td>
                          );
                          
                          return cells;
                        }
                        
                        // Dla regularnych pól (calculated, generic)
                        const field = item.field;
                        const isCalculated = item.fieldType === 'calculated';
                        const value = isCalculated
                          ? row.workScope!.calculatedFieldValues[field.name]
                          : row.workScope!.genericFieldValues[field.name];
                        
                        // Dla pól kalkulowanych - sprawdź czy można auto-kalkulować
                        let canAutoCalc = false;
                        if (isCalculated) {
                          const valuesByType: Record<number, any> = {};
                          calculatedFields.forEach(f => {
                            if (f.name in row.workScope!.calculatedFieldValues) {
                              valuesByType[f.type] = row.workScope!.calculatedFieldValues[f.name];
                            }
                          });
                          canAutoCalc = canAutoCalculate((field as any).type, valuesByType);
                        }

                        return (
                          <Td
                            key={`ws-${item.fieldType}-${field.name}`}
                            isNumeric={isCalculated}
                            borderRightWidth="1px"
                            borderRightColor="gray.200"
                            py={2}
                            px={2}
                            fontSize="sm"
                          >
                            {editable && onDataChange ? (
                              isCalculated ? (
                                <CalculatedFieldRenderer
                                  field={field as any}
                                  value={value}
                                  onChange={(newValue) => {
                                    updateWorkScope(
                                      row.groupId!,
                                      row.workScope!.id,
                                      (ws) => ({
                                        ...ws,
                                        calculatedFieldValues: {
                                          ...ws.calculatedFieldValues,
                                          [field.name]: newValue,
                                        },
                                      })
                                    );
                                  }}
                                  allValues={{
                                    ...row.workScope!.calculatedFieldValues,
                                    ...row.workScope!.genericFieldValues,
                                  }}
                                  readOnly={false}
                                  canAutoCalculate={canAutoCalc}
                                  compact
                                />
                              ) : (
                                <GenericFieldRenderer
                                  field={field as any}
                                  value={value}
                                  onChange={(newValue) => {
                                    updateWorkScope(
                                      row.groupId!,
                                      row.workScope!.id,
                                      (ws) => ({
                                        ...ws,
                                        genericFieldValues: {
                                          ...ws.genericFieldValues,
                                          [field.name]: newValue,
                                        },
                                      })
                                    );
                                  }}
                                  allValues={{
                                    ...row.workScope!.calculatedFieldValues,
                                    ...row.workScope!.genericFieldValues,
                                  }}
                                  readOnly={false}
                                  compact
                                />
                              )
                            ) : (
                              <Text fontSize="sm" fontWeight={isCalculated ? "medium" : "normal"}>
                                {value !== undefined && value !== null
                                  ? typeof value === "number"
                                    ? value.toFixed(2)
                                    : typeof value === "boolean"
                                    ? value ? "Tak" : "Nie"
                                    : String(value)
                                  : "-"}
                              </Text>
                            )}
                          </Td>
                        );
                      })}

                      {/* Kolumna akcji */}
                      {editable && (
                        <Td
                          borderLeftWidth="2px"
                          borderLeftColor="gray.300"
                          py={2}
                          px={2}
                        >
                          <HStack spacing={1} justify="flex-start">
                            {onDeleteWorkScope && row.groupId && (
                              <Tooltip label="Usuń pozycję" hasArrow>
                                <IconButton
                                  aria-label="Usuń pozycję"
                                  icon={<Trash2 size={12} />}
                                  size="xs"
                                  colorScheme="red"
                                  variant="ghost"
                                  borderRadius="md"
                                  _hover={{ transform: "scale(1.1)", bg: "red.50" }}
                                  transition="all 0.2s"
                                  onClick={() =>
                                    onDeleteWorkScope(row.groupId!, row.workScope!.id)
                                  }
                                />
                              </Tooltip>
                            )}
                          </HStack>
                        </Td>
                      )}
                    </Tr>
                  );
                }

                return null;
              })}

              {/* Grand Total Row */}
              {Object.keys(grandTotals).length > 0 && (
                <Tr
                  bg="green.500"
                  fontWeight="bold"
                  fontSize="md"
                  color="white"
                  borderTopWidth="4px"
                  borderTopColor="green.700"
                >
                  <Td borderRightWidth="2px" borderRightColor="green.600" py={4}>
                    <Badge
                      colorScheme="green"
                      bg="white"
                      color="green.700"
                      fontSize="sm"
                      px={3}
                      py={1}
                    >
                      RAZEM
                    </Badge>
                  </Td>

                  {/* Wszystkie kolumny - nagłówki grup i pola work scope - posortowane według columnLayout */}
                  {allColumns.map((item, colIdx) => {
                    if (item.fieldType === 'groupHeader') {
                      // Puste kolumny dla headerów grup
                      return (
                        <Td
                          key={`total-header-${item.field.type}`}
                          borderRightWidth="1px"
                          borderRightColor="green.600"
                        />
                      );
                    } else if (item.fieldType === 'collection-calculated' || item.fieldType === 'collection-generic') {
                      // Dla podpól kolekcji renderuj puste komórki
                      const collectionField = item.collectionField;
                      const isFirstFieldOfCollection = colIdx === 0 ||
                        allColumns[colIdx - 1].fieldType === 'groupHeader' ||
                        allColumns[colIdx - 1].fieldType === 'calculated' ||
                        allColumns[colIdx - 1].fieldType === 'generic' ||
                        (allColumns[colIdx - 1].fieldType.startsWith('collection-') && 
                         (allColumns[colIdx - 1] as any).collectionField.name !== collectionField.name);
                      
                      const cells = [];
                      if (isFirstFieldOfCollection && editable) {
                        cells.push(
                          <Td key={`total-coll-action-${collectionField.name}`} borderRightWidth="1px" borderRightColor="green.600" />
                        );
                      }
                      cells.push(
                        <Td key={`total-coll-${item.fullName}`} borderRightWidth="1px" borderRightColor="green.600" />
                      );
                      return cells;
                    } else {
                      // Dla regularnych pól (calculated/generic) wyświetl sumy
                      const field = item.field;
                      const isCalculated = item.fieldType === 'calculated';
                      const total = grandTotals[field.name];
                      return (
                        <Td
                          key={`total-${item.fieldType}-${field.name}`}
                          isNumeric={isCalculated}
                          borderRightWidth="1px"
                          borderRightColor="green.600"
                          py={4}
                          fontSize="md"
                          fontWeight="bold"
                        >
                          {isCalculated && total !== undefined ? total.toFixed(2) : ""}
                        </Td>
                      );
                    }
                  })}

                  {/* Pusta kolumna akcji */}
                  {editable && (
                    <Td borderLeftWidth="2px" borderLeftColor="green.600" />
                  )}
                </Tr>
              )}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
};
