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
import { Plus, Trash2 } from "lucide-react";
import type {
  CostEstimateDataModel,
  CostEstimateGroup,
  CostEstimateWorkScope,
  CostEstimateCollectionItem,
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  GroupHeaderFieldDefinition,
} from "../types/costEstimate.types";
import { GroupHeaderFieldType, type CostEstimateTemplateDto } from "../types/costEstimate.types";
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
function sortHeaderFieldsByLayout(
  fields: GroupHeaderFieldDefinition[],
  columnLayout?: string[]
): GroupHeaderFieldDefinition[] {
  if (!columnLayout || columnLayout.length === 0) {
    return [...fields].sort((a, b) => a.order - b.order);
  }

  return [...fields].sort((a, b) => {
    const fieldNameA = GroupHeaderFieldType[a.type];
    const fieldNameB = GroupHeaderFieldType[b.type];
    const indexA = columnLayout.indexOf(fieldNameA);
    const indexB = columnLayout.indexOf(fieldNameB);

    if (indexA === -1 && indexB === -1) return a.order - b.order;
    if (indexA === -1) return 1;
    if (indexB === -1) return -1;

    return indexA - indexB;
  });
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
      "Grupa";

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
        groupNumber
      );
      rows.push(...subRows);
    }
  });

  return rows;
}

export const CostEstimateViewer: React.FC<CostEstimateViewerProps> = ({
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
  const calculatedFields =
    template.templateStructure.workScopeFieldsDefinition?.calculatedFields || [];
  const genericFields =
    template.templateStructure.workScopeFieldsDefinition?.genericFields || [];
  const groupHeaderFields =
    template.templateStructure.groupDefinition?.headerFields || [];

  // Ekstraktuj columnLayout z columns jeśli istnieje (nowe API)
  const columnLayout = template.templateStructure.uiConfiguration?.columns?.map(col => col.fieldName);
  const summaryConfig = template.templateStructure.summaryConfiguration;

  // Sortowanie i filtrowanie pól
  const visibleGroupHeaderFields = sortHeaderFieldsByLayout(
    groupHeaderFields.filter((f) => f.visible),
    columnLayout
  );

  const visibleCalculatedFields = sortFieldsByLayout(
    calculatedFields.filter((f) => f.visible),
    columnLayout
  );

  const visibleGenericFields = sortFieldsByLayout(
    genericFields.filter((f) => f.visible),
    columnLayout
  );

  const visibleCollectionFields: GenericFieldDefinition[] = []; // Kolekcje nie są częścią GenericFieldType (0-5)

  // Spłaszcz strukturę do wierszy tabeli
  const flatRows = useMemo(() => {
    return flattenGroups(
      dataModel.groups,
      0,
      calculatedFields,
      genericFields,
      summaryConfig
    );
  }, [dataModel.groups, calculatedFields, genericFields, summaryConfig]);

  // Oblicz całkowite sumy
  const grandTotals = useMemo(() => {
    const totals: Record<string, number> = {};

    if (
      summaryConfig?.showTotalSummary &&
      summaryConfig.totalSummaryFields?.length > 0
    ) {
      dataModel.groups.forEach((group) => {
        const groupTotals = getGroupTotals(group, summaryConfig, calculatedFields);
        Object.entries(groupTotals).forEach(([fieldName, value]) => {
          totals[fieldName] = (totals[fieldName] || 0) + value;
        });
      });
    }

    return totals;
  }, [dataModel.groups, calculatedFields, genericFields, summaryConfig]);

  // Helper do aktualizacji grupy
  const updateGroup = (
    groupId: string,
    updater: (group: CostEstimateGroup) => CostEstimateGroup
  ) => {
    if (!onDataChange) return;

    const updateGroupsRecursive = (
      groups: CostEstimateGroup[]
    ): CostEstimateGroup[] => {
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

    onDataChange({
      ...dataModel,
      groups: updateGroupsRecursive(dataModel.groups),
    });
  };

  // Helper do aktualizacji workScope
  const updateWorkScope = (
    groupId: string,
    workScopeId: string,
    updater: (ws: CostEstimateWorkScope) => CostEstimateWorkScope
  ) => {
    updateGroup(groupId, (group) => ({
      ...group,
      workScopes: group.workScopes.map((ws) =>
        ws.id === workScopeId ? updater(ws) : ws
      ),
    }));
  };

  // Helper do aktualizacji itemu w kolekcji
  const updateCollectionItem = (
    groupId: string,
    workScopeId: string,
    collectionFieldName: string,
    itemId: string,
    updater: (item: CostEstimateCollectionItem) => CostEstimateCollectionItem
  ) => {
    updateWorkScope(groupId, workScopeId, (ws) => ({
      ...ws,
      collectionFieldValues: {
        ...ws.collectionFieldValues,
        [collectionFieldName]: (
          ws.collectionFieldValues?.[collectionFieldName] || []
        ).map((item) => (item.id === itemId ? updater(item) : item)),
      },
    }));
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

      // Jeśli zaznaczamy item, kopiuj wartości z pól kolekcji do głównych pól workScope
      let updatedCalculatedValues = { ...ws.calculatedFieldValues };
      if (isSelected && selectedItem) {
        const nestedCalcFields = collectionField.nestedFields?.calculatedFields || [];
        
        nestedCalcFields.forEach((nestedField) => {
          const nestedValue = selectedItem.calculatedFieldValues?.[nestedField.name];
          
          if (nestedValue !== undefined && nestedValue !== null) {
            // Mapowanie: znajdź pole główne o tym samym typie (UnitPriceNet, VatRate, etc.)
            const mainField = calculatedFields.find(f => f.type === nestedField.type);
            
            if (mainField) {
              updatedCalculatedValues[mainField.name] = nestedValue;
            }
          }
        });
      }

      // Zwróć zaktualizowany workScope BEZ przeliczania
      // Parent (CostEstimateEditor) wywoła recalculateAll i zachowa isSelected
      const updatedWs = {
        ...ws,
        calculatedFieldValues: updatedCalculatedValues,
        collectionFieldValues: {
          ...ws.collectionFieldValues,
          [collectionFieldName]: updatedItems,
        },
      };

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
      <Thead bg="blue.600" position="sticky" top={0} zIndex={10}>
        <Tr>
          {/* Kolumna pozycji */}
          <Th
            w="120px"
            borderRightWidth="2px"
            borderColor="blue.500"
            color="white"
            fontSize="sm"
            py={3}
            whiteSpace="nowrap"
          >
            Pozycja
          </Th>

          {/* Headery grup - najpierw */}
          {visibleGroupHeaderFields.map((field) => (
            <Th
              key={`header-${field.type}`}
              borderRightWidth="1px"
              borderColor="blue.500"
              color="white"
              fontSize="sm"
              py={3}
              minW="150px"
              whiteSpace="nowrap"
            >
              {field.customLabel || getDefaultGroupHeaderLabel(field.type)}
            </Th>
          ))}

          {/* Pola kalkulowane i generyczne */}
          {visibleCalculatedFields.map((field) => (
            <Th
              key={`calc-${field.name}`}
              isNumeric
              borderRightWidth="1px"
              borderColor="blue.500"
              color="white"
              fontSize="sm"
              py={3}
              minW="130px"
              whiteSpace="nowrap"
            >
              {field.label}
              {field.unit && (
                <Text as="span" fontSize="xs" fontWeight="normal" ml={1}>
                  [{field.unit}]
                </Text>
              )}
            </Th>
          ))}

          {visibleGenericFields.map((field) => (
            <Th
              key={`gen-${field.name}`}
              borderRightWidth="1px"
              borderColor="blue.500"
              color="white"
              fontSize="sm"
              py={3}
              minW="150px"
              whiteSpace="nowrap"
            >
              {field.label}
            </Th>
          ))}

          {/* Pola kolekcji - każde nested field jako osobna kolumna */}
          {visibleCollectionFields.flatMap((collectionField) => {
            const nestedCalcFields =
              collectionField.nestedFields?.calculatedFields?.filter((f) => f.visible) || [];
            const nestedGenFields =
              collectionField.nestedFields?.genericFields?.filter((f) => f.visible) || [];
            
            const headers = [];
            
            // Dodaj kolumnę zaznaczenia jeśli tryb edycji
            if (editable) {
              headers.push(
                <Th
                  key={`coll-select-${collectionField.name}`}
                  borderRightWidth="1px"
                  borderColor="blue.500"
                  color="white"
                  fontSize="xs"
                  py={3}
                  w="40px"
                  textAlign="center"
                >
                  ✓
                </Th>
              );
            }
            
            return [
              ...headers,
              ...nestedCalcFields.map((nestedField) => (
                <Th
                  key={`coll-calc-${collectionField.name}-${nestedField.name}`}
                  isNumeric
                  borderRightWidth="1px"
                  borderColor="blue.500"
                  color="white"
                  fontSize="sm"
                  py={3}
                  minW="130px"
                  whiteSpace="nowrap"
                >
                  {nestedField.label}
                  {nestedField.unit && (
                    <Text as="span" fontSize="xs" fontWeight="normal" ml={1}>
                      [{nestedField.unit}]
                    </Text>
                  )}
                </Th>
              )),
              ...nestedGenFields.map((nestedField) => (
                <Th
                  key={`coll-gen-${collectionField.name}-${nestedField.name}`}
                  borderRightWidth="1px"
                  borderColor="blue.500"
                  color="white"
                  fontSize="sm"
                  py={3}
                  minW="150px"
                  whiteSpace="nowrap"
                >
                  {nestedField.label}
                </Th>
              )),
            ];
          })}

          {/* Kolumna akcji */}
          {editable && (
            <Th
              w="120px"
              borderLeftWidth="2px"
              borderColor="blue.500"
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
            <Badge colorScheme="blue" fontSize="md" p={2}>
              {template.name} (v{template.templateVersionNumber})
            </Badge>
            {editable && onAddGroup && (
              <Button
                leftIcon={<Plus size={16} />}
                size="sm"
                colorScheme="green"
                onClick={onAddGroup}
              >
                Dodaj grupę
              </Button>
            )}
          </HStack>
        </HStack>
      </Box>

      {/* Main Table */}
      {dataModel.groups.length === 0 ? (
        <Box
          p={12}
          textAlign="center"
          bg="gray.50"
          borderRadius="md"
          borderWidth="1px"
        >
          <Text color="gray.500" fontSize="lg" mb={4}>
            Brak grup w kosztorysie
          </Text>
          {editable && onAddGroup && (
            <Button
              leftIcon={<Plus size={16} />}
              size="md"
              colorScheme="blue"
              onClick={onAddGroup}
            >
              Dodaj pierwszą grupę
            </Button>
          )}
        </Box>
      ) : (
        <Box
          overflowX="auto"
          bg="white"
          borderRadius="lg"
          shadow="md"
          borderWidth="1px"
        >
          <Table size="sm" variant="simple">
            {renderTableHeader()}
            <Tbody>
              {flatRows.map((row, idx) => {
                const indent = row.level * 24;

                if (row.type === "group" || row.type === "subgroup") {
                  const isGroup = row.type === "group";
                  const bgColor = isGroup ? "blue.50" : "teal.50";
                  const badgeColor = isGroup ? "blue" : "teal";

                  return (
                    <Tr
                      key={`${row.type}-${row.groupId}-${idx}`}
                      bg={bgColor}
                      borderTopWidth={isGroup ? "3px" : "2px"}
                      borderTopColor={isGroup ? "blue.400" : "teal.300"}
                    >
                      {/* Kolumna pozycji - Badge + numer grupy + akcje */}
                      <Td
                        borderRightWidth="1px"
                        borderRightColor="gray.200"
                        p={3}
                        pl={`${indent + 12}px`}
                        borderBottomWidth="2px"
                        borderBottomColor={isGroup ? "blue.200" : "teal.200"}
                      >
                        <Badge
                          colorScheme={badgeColor}
                          fontSize="sm"
                          px={3}
                          py={1}
                          fontWeight="bold"
                        >
                          {row.groupNumber}
                        </Badge>
                      </Td>

                      {/* Pola headerów grup - w tym GroupName jako edytowalne */}
                      {visibleGroupHeaderFields.map((field) => {
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
                            borderBottomColor={isGroup ? "blue.200" : "teal.200"}
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
                      })}

                      {/* Sumy dla pól kalkulowanych */}
                      {visibleCalculatedFields.map((field) => {
                        const totalValue = row.totals?.[field.name];
                        return (
                          <Td
                            key={`group-calc-${field.name}`}
                            isNumeric
                            borderRightWidth="1px"
                            borderRightColor="gray.200"
                            borderBottomWidth="2px"
                            borderBottomColor={isGroup ? "blue.200" : "teal.200"}
                            fontWeight="bold"
                            color={isGroup ? "blue.700" : "teal.700"}
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
                      })}

                      {/* Sumy dla pól generycznych */}
                      {visibleGenericFields.map((field) => {
                        const totalValue = row.totals?.[field.name];
                        return (
                          <Td
                            key={`group-gen-${field.name}`}
                            borderRightWidth="1px"
                            borderRightColor="gray.200"
                            borderBottomWidth="2px"
                            borderBottomColor={isGroup ? "blue.200" : "teal.200"}
                            fontWeight="bold"
                            color={isGroup ? "blue.700" : "teal.700"}
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
                      })}

                      {visibleCollectionFields.flatMap((cf) => [
                        ...(cf.nestedFields?.calculatedFields?.filter((f) => f.visible) ||
                          []).map((field) => (
                          <Td
                            key={`empty-coll-calc-${cf.name}-${field.name}`}
                            borderRightWidth="1px"
                            borderRightColor="gray.200"
                            borderBottomWidth="2px"
                            borderBottomColor={isGroup ? "blue.200" : "teal.200"}
                          />
                        )),
                        ...(cf.nestedFields?.genericFields?.filter((f) => f.visible) || []).map(
                          (field) => (
                            <Td
                              key={`empty-coll-gen-${cf.name}-${field.name}`}
                              borderRightWidth="1px"
                              borderRightColor="gray.200"
                              borderBottomWidth="2px"
                              borderBottomColor={isGroup ? "blue.200" : "teal.200"}
                            />
                          )
                        ),
                      ])}

                      {/* Kolumna akcji dla grupy */}
                      {editable && (
                        <Td
                          borderBottomWidth="2px"
                          borderBottomColor={isGroup ? "blue.200" : "teal.200"}
                        >
                          <HStack spacing={1} justify="flex-end">
                            {onAddWorkScope && row.groupId && (
                              <Tooltip label="Dodaj pozycję">
                                <IconButton
                                  aria-label="Dodaj pozycję"
                                  icon={<Plus size={12} />}
                                  size="xs"
                                  colorScheme="green"
                                  variant="solid"
                                  onClick={() => onAddWorkScope(row.groupId!)}
                                />
                              </Tooltip>
                            )}
                            {onAddSubGroup &&
                              row.groupId &&
                              template.templateStructure.canBranchGroups && (
                                <Tooltip label="Dodaj podgrupę">
                                  <IconButton
                                    aria-label="Dodaj podgrupę"
                                    icon={<Plus size={12} />}
                                    size="xs"
                                    colorScheme={badgeColor}
                                    variant="outline"
                                    onClick={() => onAddSubGroup(row.groupId!)}
                                  />
                                </Tooltip>
                              )}
                            {onDeleteGroup && row.groupId && (
                              <Tooltip
                                label={isGroup ? "Usuń grupę" : "Usuń podgrupę"}
                              >
                                <IconButton
                                  aria-label={
                                    isGroup ? "Usuń grupę" : "Usuń podgrupę"
                                  }
                                  icon={<Trash2 size={12} />}
                                  size="xs"
                                  colorScheme="red"
                                  variant="ghost"
                                  onClick={() => onDeleteGroup(row.groupId!)}
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
                        bg: editable ? "blue.50" : undefined,
                        transform: editable ? "scale(1.001)" : undefined,
                        transition: "all 0.15s",
                      }}
                      borderBottomWidth="1px"
                      borderBottomColor="gray.100"
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

                      {/* Pola headerów grup - edytowalne pod dedykowanymi nagłówkami */}
                      {visibleGroupHeaderFields.map((field) => {
                        const fieldKey = GroupHeaderFieldType[field.type];
                        const value = row.group?.headerValues?.[fieldKey];

                        return (
                          <Td
                            key={`ws-header-${field.type}`}
                            borderRightWidth="1px"
                            borderRightColor="gray.200"
                            py={2}
                            px={2}
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
                              <Text fontSize="sm">
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
                      })}

                      {/* Pola kalkulowane */}
                      {visibleCalculatedFields.map((field) => {
                        const value = row.workScope!.calculatedFieldValues[field.name];
                        
                        // Przygotuj mapę valuesByType dla canAutoCalculate
                        const valuesByType: Record<number, any> = {};
                        calculatedFields.forEach(f => {
                          if (f.name in row.workScope!.calculatedFieldValues) {
                            valuesByType[f.type] = row.workScope!.calculatedFieldValues[f.name];
                          }
                        });
                        const canAutoCalc = canAutoCalculate(field.type, valuesByType);

                        return (
                          <Td
                            key={`ws-calc-${field.name}`}
                            isNumeric
                            borderRightWidth="1px"
                            borderRightColor="gray.200"
                            py={2}
                            px={2}
                            fontSize="sm"
                          >
                            {editable && onDataChange ? (
                              <CalculatedFieldRenderer
                                field={field}
                                value={value}
                                onChange={(newValue) => {
                                  updateWorkScope(
                                    row.groupId!,
                                    row.workScope!.id,
                                    (ws) => {
                                      const updatedWs = {
                                        ...ws,
                                        calculatedFieldValues: {
                                          ...ws.calculatedFieldValues,
                                          [field.name]: newValue,
                                        },
                                      };
                                      return calculateWorkScope(updatedWs, {
                                        calculatedFields,
                                        genericFields,
                                      });
                                    }
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
                              <Text fontSize="sm" fontWeight="medium">
                                {typeof value === "number" ? value.toFixed(2) : "-"}
                              </Text>
                            )}
                          </Td>
                        );
                      })}

                      {/* Pola generyczne */}
                      {visibleGenericFields.map((field) => {
                        const value = row.workScope!.genericFieldValues[field.name];

                        return (
                          <Td
                            key={`ws-gen-${field.name}`}
                            borderRightWidth="1px"
                            borderRightColor="gray.200"
                            py={2}
                            px={2}
                            fontSize="sm"
                          >
                            {editable && onDataChange ? (
                              <GenericFieldRenderer
                                field={field}
                                value={value}
                                onChange={(newValue) => {
                                  updateWorkScope(
                                    row.groupId!,
                                    row.workScope!.id,
                                    (ws) => {
                                      const updatedWs = {
                                        ...ws,
                                        genericFieldValues: {
                                          ...ws.genericFieldValues,
                                          [field.name]: newValue,
                                        },
                                      };
                                      return calculateWorkScope(updatedWs, {
                                        calculatedFields,
                                        genericFields,
                                      });
                                    }
                                  );
                                }}
                                allValues={{
                                  ...row.workScope!.calculatedFieldValues,
                                  ...row.workScope!.genericFieldValues,
                                }}
                                readOnly={false}
                                compact
                              />
                            ) : (
                              <Text fontSize="sm">
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
                      })}

                      {/* Pola kolekcji - wyświetl nested fields jako osobne kolumny */}
                      {visibleCollectionFields.map((collectionField) => {
                        const collectionItems =
                          row.workScope!.collectionFieldValues?.[
                            collectionField.name
                          ] || [];

                        const nestedCalcFields =
                          collectionField.nestedFields?.calculatedFields?.filter(
                            (f) => f.visible
                          ) || [];
                        const nestedGenFields =
                          collectionField.nestedFields?.genericFields?.filter(
                            (f) => f.visible
                          ) || [];

                        // Renderuj kolumny dla każdego nested field
                        return (
                          <React.Fragment key={`ws-coll-${collectionField.name}`}>
                            {/* Kolumna z checkboxami do zaznaczania itemów */}
                            {collectionItems.length > 0 && editable && onDataChange && (
                              <Td
                                key={`coll-select-${collectionField.name}`}
                                borderRightWidth="1px"
                                borderRightColor="gray.200"
                                py={2}
                                px={2}
                                bg="purple.50"
                              >
                                <VStack spacing={1} align="center" key={`vstack-${collectionField.name}-${Date.now()}`}>
                                  {collectionItems.map((item) => (
                                    <Box key={item.id} width="100%" display="flex" justifyContent="center" minHeight="40px" alignItems="center">
                                      <Checkbox
                                        size="sm"
                                        colorScheme="purple"
                                        isChecked={!!item.isSelected}
                                        onChange={(e) => {
                                          handleCollectionItemSelect(
                                            row.groupId!,
                                            row.workScope!.id,
                                            collectionField.name,
                                            item.id,
                                            e.target.checked,
                                            collectionField
                                          );
                                        }}
                                      />
                                    </Box>
                                  ))}
                                </VStack>
                              </Td>
                            )}
                            {nestedCalcFields.map((nestedField) => (
                              <Td
                                key={`nested-calc-${nestedField.name}`}
                                isNumeric
                                borderRightWidth="1px"
                                borderRightColor="gray.200"
                                py={2}
                                px={2}
                                fontSize="sm"
                                bg="purple.50"
                              >
                                {collectionItems.length > 0 ? (
                                  <VStack spacing={1} align="end">
                                    {collectionItems.map((item) => {
                                      const itemValue =
                                        item.calculatedFieldValues?.[
                                          nestedField.name
                                        ];
                                      
                                      // Przygotuj mapę valuesByType dla collection item
                                      const nestedCalcFieldsAll = collectionField.nestedFields?.calculatedFields || [];
                                      const valuesByType: Record<number, any> = {};
                                      nestedCalcFieldsAll.forEach(f => {
                                        if (f.name in (item.calculatedFieldValues || {})) {
                                          valuesByType[f.type] = item.calculatedFieldValues![f.name];
                                        }
                                      });
                                      const canAutoCalc = canAutoCalculate(nestedField.type, valuesByType);
                                      
                                      return (
                                        <Box key={item.id} width="100%">
                                          {editable && onDataChange ? (
                                            <CalculatedFieldRenderer
                                              field={nestedField}
                                              value={itemValue}
                                              onChange={(newValue) => {
                                                updateCollectionItem(
                                                  row.groupId!,
                                                  row.workScope!.id,
                                                  collectionField.name,
                                                  item.id,
                                                  (updatedItem) => ({
                                                    ...updatedItem,
                                                    calculatedFieldValues: {
                                                      ...updatedItem.calculatedFieldValues,
                                                      [nestedField.name]: newValue,
                                                    },
                                                  })
                                                );
                                              }}
                                              readOnly={false}
                                              canAutoCalculate={canAutoCalc}
                                            />
                                          ) : (
                                            <Text fontSize="xs">
                                              {typeof itemValue === "number"
                                                ? itemValue.toFixed(2)
                                                : "-"}
                                            </Text>
                                          )}
                                        </Box>
                                      );
                                    })}
                                  </VStack>
                                ) : (
                                  <Text fontSize="xs" color="gray.400">
                                    -
                                  </Text>
                                )}
                              </Td>
                            ))}

                            {nestedGenFields.map((nestedField) => (
                              <Td
                                key={`nested-gen-${nestedField.name}`}
                                borderRightWidth="1px"
                                borderRightColor="gray.200"
                                py={2}
                                px={2}
                                fontSize="sm"
                                bg="purple.50"
                              >
                                {collectionItems.length > 0 ? (
                                  <VStack spacing={1} align="start">
                                    {collectionItems.map((item) => {
                                      const itemValue =
                                        item.genericFieldValues?.[nestedField.name];
                                      return (
                                        <Box key={item.id} width="100%">
                                          {editable && onDataChange ? (
                                            <GenericFieldRenderer
                                              field={nestedField}
                                              value={itemValue}
                                              onChange={(newValue) => {
                                                updateCollectionItem(
                                                  row.groupId!,
                                                  row.workScope!.id,
                                                  collectionField.name,
                                                  item.id,
                                                  (updatedItem) => ({
                                                    ...updatedItem,
                                                    genericFieldValues: {
                                                      ...updatedItem.genericFieldValues,
                                                      [nestedField.name]: newValue,
                                                    },
                                                  })
                                                );
                                              }}
                                              readOnly={false}
                                            />
                                          ) : (
                                            <Text fontSize="xs">
                                              {itemValue !== undefined &&
                                              itemValue !== null
                                                ? typeof itemValue === "boolean"
                                                  ? itemValue
                                                    ? "Tak"
                                                    : "Nie"
                                                  : String(itemValue)
                                                : "-"}
                                            </Text>
                                          )}
                                        </Box>
                                      );
                                    })}
                                  </VStack>
                                ) : (
                                  <Text fontSize="xs" color="gray.400">
                                    -
                                  </Text>
                                )}
                              </Td>
                            ))}
                          </React.Fragment>
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
                          <HStack spacing={1}>
                            {onDeleteWorkScope && row.groupId && (
                              <Tooltip label="Usuń pozycję">
                                <IconButton
                                  aria-label="Usuń pozycję"
                                  icon={<Trash2 size={12} />}
                                  size="xs"
                                  colorScheme="red"
                                  variant="ghost"
                                  onClick={() =>
                                    onDeleteWorkScope(row.groupId!, row.workScope!.id)
                                  }
                                />
                              </Tooltip>
                            )}
                            {onAddCollectionItem &&
                              visibleCollectionFields.length > 0 &&
                              visibleCollectionFields.map((collectionField) => (
                                <Tooltip
                                  key={collectionField.name}
                                  label={`Dodaj ${collectionField.label}`}
                                >
                                  <IconButton
                                    aria-label={`Dodaj ${collectionField.label}`}
                                    icon={<Plus size={12} />}
                                    size="xs"
                                    colorScheme="purple"
                                    variant="solid"
                                    onClick={() =>
                                      onAddCollectionItem(
                                        row.groupId!,
                                        row.workScope!.id,
                                        collectionField.name
                                      )
                                    }
                                  />
                                </Tooltip>
                              ))}
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
                    <HStack spacing={3}>
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
                      <Text fontSize="md" fontWeight="bold">
                        Całkowite podsumowanie
                      </Text>
                    </HStack>
                  </Td>

                  {/* Puste kolumny dla headerów grup */}
                  {visibleGroupHeaderFields.map((field) => (
                    <Td
                      key={`total-header-${field.type}`}
                      borderRightWidth="1px"
                      borderRightColor="green.600"
                    />
                  ))}

                  {/* Całkowite sumy dla pól kalkulowanych */}
                  {visibleCalculatedFields.map((field) => {
                    const total = grandTotals[field.name];
                    return (
                      <Td
                        key={`total-calc-${field.name}`}
                        isNumeric
                        borderRightWidth="1px"
                        borderRightColor="green.600"
                        py={4}
                        fontSize="md"
                        fontWeight="bold"
                      >
                        {total !== undefined ? `${total.toFixed(2)}${template.currency ? ' ' + template.currency : ''}` : ""}
                      </Td>
                    );
                  })}

                  {/* Puste kolumny dla pól generycznych */}
                  {visibleGenericFields.map((field) => (
                    <Td
                      key={`total-gen-${field.name}`}
                      borderRightWidth="1px"
                      borderRightColor="green.600"
                    />
                  ))}

                  {/* Puste kolumny dla kolekcji */}
                  {visibleCollectionFields.map((collectionField) => {
                    const nestedCount =
                      (collectionField.nestedFields?.calculatedFields?.filter(
                        (f) => f.visible
                      ).length || 0) +
                      (collectionField.nestedFields?.genericFields?.filter((f) => f.visible)
                        .length || 0);

                    return Array.from({ length: nestedCount }).map((_, i) => (
                      <Td
                        key={`total-coll-${collectionField.name}-${i}`}
                        borderRightWidth="1px"
                        borderRightColor="green.600"
                      />
                    ));
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
