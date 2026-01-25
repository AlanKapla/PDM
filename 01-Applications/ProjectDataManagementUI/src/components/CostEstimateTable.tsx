import React, { useState } from 'react';
import {
  Box,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  Button,
  HStack,
  VStack,
  Text,
  Badge,
  Collapse,
  useDisclosure,
  Grid,
  GridItem,
  Divider,
  Tooltip,
} from '@chakra-ui/react';
import {
  ChevronDown,
  ChevronRight,
  Plus,
  Trash2,
  GripVertical,
} from 'lucide-react';
import type {
  CostEstimateGroup,
  CostEstimateWorkScope,
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  GroupHeaderFieldDefinition,
} from '../types/costEstimate.types';
import { GroupHeaderFieldType, type CostEstimateTemplateDto } from '../types/costEstimate.types';
import {
  CalculatedFieldRenderer,
  GenericFieldRenderer,
  GroupHeaderFieldRenderer,
} from './FieldRenderer';
import { calculateWorkScope, canAutoCalculate } from '../utils/calculationEngine';

interface CostEstimateTableProps {
  group: CostEstimateGroup;
  level: number;
  calculatedFields: CalculatedFieldDefinition[];
  genericFields: GenericFieldDefinition[];
  groupHeaderFields: GroupHeaderFieldDefinition[];
  template: CostEstimateTemplateDto;
  onGroupChange: (group: CostEstimateGroup) => void;
  onAddWorkScope: () => void;
  onDeleteWorkScope: (workScopeId: string) => void;
  onDeleteGroup: () => void;
  onAddSubGroup?: () => void;
  readOnly?: boolean;
  showGroupActions?: boolean;
}

export const CostEstimateTable: React.FC<CostEstimateTableProps> = ({
  group,
  level,
  calculatedFields,
  genericFields,
  groupHeaderFields,
  template,
  onGroupChange,
  onAddWorkScope,
  onDeleteWorkScope,
  onDeleteGroup,
  onAddSubGroup,
  readOnly = false,
  showGroupActions = true,
}) => {
  const { isOpen, onToggle } = useDisclosure({ defaultIsOpen: true });

  // Pobierz columnLayout z template (jeśli istnieje) - zmapuj z columns
  const columnLayout = template.templateStructure.uiConfiguration?.columns?.map(col => col.fieldName);

  // Funkcja sortowania pól według columnLayout lub order (dla pól z 'name')
  const sortFieldsByLayout = <T extends { name: string; order: number }>(fields: T[]): T[] => {
    if (!columnLayout || columnLayout.length === 0) {
      // Sortuj według order jeśli brak columnLayout
      return [...fields].sort((a, b) => a.order - b.order);
    }

    // Sortuj według columnLayout
    return [...fields].sort((a, b) => {
      const indexA = columnLayout.indexOf(a.name);
      const indexB = columnLayout.indexOf(b.name);

      // Jeśli pole nie ma pozycji w columnLayout, umieść na końcu
      if (indexA === -1 && indexB === -1) return a.order - b.order;
      if (indexA === -1) return 1;
      if (indexB === -1) return -1;

      return indexA - indexB;
    });
  };

  // Funkcja sortowania pól nagłówkowych (używają 'type' zamiast 'name')
  const sortHeaderFieldsByLayout = (fields: GroupHeaderFieldDefinition[]): GroupHeaderFieldDefinition[] => {
    if (!columnLayout || columnLayout.length === 0) {
      // Sortuj według order jeśli brak columnLayout
      return [...fields].sort((a, b) => a.order - b.order);
    }

    // Sortuj według columnLayout (używamy GroupHeaderFieldType[type] jako klucza)
    return [...fields].sort((a, b) => {
      const fieldNameA = GroupHeaderFieldType[a.type];
      const fieldNameB = GroupHeaderFieldType[b.type];
      const indexA = columnLayout.indexOf(fieldNameA);
      const indexB = columnLayout.indexOf(fieldNameB);

      // Jeśli pole nie ma pozycji w columnLayout, umieść na końcu
      if (indexA === -1 && indexB === -1) return a.order - b.order;
      if (indexA === -1) return 1;
      if (indexB === -1) return -1;

      return indexA - indexB;
    });
  };

  // Sortowanie pól według columnLayout lub order
  const visibleCalculatedFields = sortFieldsByLayout(
    calculatedFields.filter((f) => f.visible)
  );

  // Podziel generic fields na inline (tabela) i collection (poniżej)
  const visibleGenericFields = sortFieldsByLayout(
    genericFields.filter((f) => f.visible && f.type !== 10) // Wykluczamy Collection z tabeli
  );

  const visibleCollectionFields = sortFieldsByLayout(
    genericFields.filter((f) => f.visible && f.type === 10) // Tylko Collection
  );

  const visibleGroupHeaderFields = sortHeaderFieldsByLayout(
    groupHeaderFields.filter((f) => f.visible || f.type === 0) // GroupName (type 0) zawsze widoczne
  );

  // Handler dla zmian w nagłówku grupy
  const handleHeaderValueChange = (fieldKey: string, value: any) => {
    onGroupChange({
      ...group,
      headerValues: {
        ...group.headerValues,
        [fieldKey]: value,
      },
    });
  };

  // Handler dla zaznaczenia opcji w kolekcji typu selectable
  const handleCollectionSelectionChange = (
    workScopeId: string,
    collectionFieldName: string,
    selectedItem: any | null
  ) => {
    const updatedWorkScopes = group.workScopes.map((ws) => {
      if (ws.id === workScopeId) {
        // Znajdź pola UnitPriceNet (type 0) i VatRate (type 1) w pozycji
        const mainUnitPriceNetField = calculatedFields.find((f) => f.type === 0);
        const mainVatRateField = calculatedFields.find((f) => f.type === 1);

        let newCalculatedValues = { ...ws.calculatedFieldValues };
        let lockedFields = ws.lockedFields ? [...ws.lockedFields] : [];

        if (selectedItem) {
          // Zaznaczono opcję - skopiuj UnitPriceNet i VatRate z kolekcji
          const collectionField = genericFields.find((f) => f.name === collectionFieldName);
          const nestedCalculatedFields = collectionField?.nestedFields?.calculatedFields || [];
          
          // Kopiuj UnitPriceNet (type 0) jeśli istnieje w itemie i w pozycji
          const collectionUnitPriceNetField = nestedCalculatedFields.find((f) => f.type === 0);
          if (collectionUnitPriceNetField && mainUnitPriceNetField) {
            const value = selectedItem.calculatedFieldValues?.[collectionUnitPriceNetField.name];
            if (value !== undefined && value !== null) {
              newCalculatedValues[mainUnitPriceNetField.name] = value;
              if (!lockedFields.includes(mainUnitPriceNetField.name)) {
                lockedFields.push(mainUnitPriceNetField.name);
              }
            }
          }

          // Kopiuj VatRate (type 1) jeśli istnieje w itemie i w pozycji
          const collectionVatRateField = nestedCalculatedFields.find((f) => f.type === 1);
          if (collectionVatRateField && mainVatRateField) {
            const value = selectedItem.calculatedFieldValues?.[collectionVatRateField.name];
            if (value !== undefined && value !== null) {
              newCalculatedValues[mainVatRateField.name] = value;
              if (!lockedFields.includes(mainVatRateField.name)) {
                lockedFields.push(mainVatRateField.name);
              }
            }
          }
        } else {
          // Odznaczono - odblokuj pola i usuń wartości
          if (mainUnitPriceNetField) {
            lockedFields = lockedFields.filter(f => f !== mainUnitPriceNetField.name);
            newCalculatedValues[mainUnitPriceNetField.name] = undefined;
          }
          if (mainVatRateField) {
            lockedFields = lockedFields.filter(f => f !== mainVatRateField.name);
            newCalculatedValues[mainVatRateField.name] = undefined;
          }
        }

        const updatedWs: CostEstimateWorkScope = {
          ...ws,
          calculatedFieldValues: newCalculatedValues,
          lockedFields: lockedFields.length > 0 ? lockedFields : undefined,
        };

        // Przelicz auto-calculated fields (UnitPriceGross, ValueNet, ValueGross itp.)
        return calculateWorkScope(updatedWs, {
          calculatedFields,
          genericFields,
        });
      }
      return ws;
    });

    onGroupChange({
      ...group,
      workScopes: updatedWorkScopes,
    });
  };

  // Handler dla zmian w work scope
  const handleWorkScopeChange = (
    workScopeId: string,
    field: string,
    value: any,
    fieldType: 'calculated' | 'generic' | 'collection'
  ) => {
    const updatedWorkScopes = group.workScopes.map((ws) => {
      if (ws.id === workScopeId) {
        let updatedWs: CostEstimateWorkScope;
        
        if (fieldType === 'calculated') {
          updatedWs = {
            ...ws,
            calculatedFieldValues: {
              ...ws.calculatedFieldValues,
              [field]: value,
            },
          };
        } else if (fieldType === 'collection') {
          // Zmiana w kolekcji - value to cała nowa tablica itemów
          updatedWs = {
            ...ws,
            collectionFieldValues: {
              ...ws.collectionFieldValues,
              [field]: value,
            },
          };
          
          // KLUCZOWE: Jeśli w kolekcji jest zaznaczony item (isSelected=true),
          // skopiuj jego UnitPriceNet i VatRate do pozycji głównej
          const selectedItem = value?.find((item: any) => item.isSelected);
          if (selectedItem) {
            const collectionField = genericFields.find((f) => f.name === field);
            const nestedCalculatedFields = collectionField?.nestedFields?.calculatedFields || [];
            
            const mainUnitPriceNetField = calculatedFields.find((f) => f.type === 0);
            const mainVatRateField = calculatedFields.find((f) => f.type === 1);
            
            let newCalculatedValues = { ...updatedWs.calculatedFieldValues };
            let lockedFields = updatedWs.lockedFields ? [...updatedWs.lockedFields] : [];
            
            // Kopiuj UnitPriceNet (type 0)
            const collectionUnitPriceNetField = nestedCalculatedFields.find((f) => f.type === 0);
            if (collectionUnitPriceNetField && mainUnitPriceNetField) {
              const unitPriceNetValue = selectedItem.calculatedFieldValues?.[collectionUnitPriceNetField.name];
              if (unitPriceNetValue !== undefined && unitPriceNetValue !== null) {
                newCalculatedValues[mainUnitPriceNetField.name] = unitPriceNetValue;
                if (!lockedFields.includes(mainUnitPriceNetField.name)) {
                  lockedFields.push(mainUnitPriceNetField.name);
                }
              }
            }
            
            // Kopiuj VatRate (type 1)
            const collectionVatRateField = nestedCalculatedFields.find((f) => f.type === 1);
            if (collectionVatRateField && mainVatRateField) {
              const vatRateValue = selectedItem.calculatedFieldValues?.[collectionVatRateField.name];
              if (vatRateValue !== undefined && vatRateValue !== null) {
                newCalculatedValues[mainVatRateField.name] = vatRateValue;
                if (!lockedFields.includes(mainVatRateField.name)) {
                  lockedFields.push(mainVatRateField.name);
                }
              }
            }
            
            updatedWs = {
              ...updatedWs,
              calculatedFieldValues: newCalculatedValues,
              lockedFields: lockedFields.length > 0 ? lockedFields : undefined,
            };
          }
        } else {
          updatedWs = {
            ...ws,
            genericFieldValues: {
              ...ws.genericFieldValues,
              [field]: value,
            },
          };
        }
        
        // Przelicz auto-calculated fields po zmianie wartości
        const recalculated = calculateWorkScope(updatedWs, {
          calculatedFields,
          genericFields,
        });
        
        return recalculated;
      }
      return ws;
    });

    // Przelicz groupTotals dla zaktualizowanej grupy - używa konfiguracji z szablonu
    const summaryConfig = template.templateStructure.summaryConfiguration;
    const updatedGroupTotals: Record<string, number> = {};
    
    if (summaryConfig?.showGroupSummary && summaryConfig.groupSummaryFields.length > 0) {
      summaryConfig.groupSummaryFields.forEach((field) => {
        const sum = updatedWorkScopes.reduce((acc, ws) => {
          const val = ws.calculatedFieldValues[field.fieldName];
          return acc + (typeof val === 'number' ? val : 0);
        }, 0);
        updatedGroupTotals[field.fieldName] = sum;
      });
    }

    onGroupChange({
      ...group,
      workScopes: updatedWorkScopes,
      groupTotals: Object.keys(updatedGroupTotals).length > 0 ? updatedGroupTotals : undefined,
    });
  };

  /**
   * Pobiera sumy grupy z backendu (totalNet, totalGross, totalVat)
   * Backend oblicza te wartości - nie obliczamy ich lokalnie
   */
  const getGroupTotals = (): Record<string, number> => {
    const totals: Record<string, number> = {};
    
    // Jeśli backend zwrócił summaryTotals (nowa struktura), użyj ich bezpośrednio
    if (group.summaryTotals) {
      return group.summaryTotals;
    }

    // Fallback: Mapuj stare nazwy (totalNet/totalGross/totalVat) na GUIDy z groupSummaryFields
    if (summaryConfig?.groupSummaryFields) {
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
  };

  const groupTotals = getGroupTotals();

  return (
    <Box mb={6} borderWidth="1px" borderRadius="md" overflow="hidden" bg="white">
      {/* Group Header */}
      <Box
        bg={`gray.${Math.min(100 + level * 50, 200)}`}
        p={4}
        borderBottomWidth={isOpen ? '1px' : '0'}
      >
        <HStack justify="space-between">
          <HStack spacing={3} flex={1}>
            <IconButton
              aria-label="Toggle group"
              icon={isOpen ? <ChevronDown size={18} /> : <ChevronRight size={18} />}
              size="sm"
              variant="ghost"
              onClick={onToggle}
            />
            
            <GripVertical size={16} color="gray" />
            
            <VStack align="start" spacing={1} flex={1}>
              <HStack>
                {group.number && (
                  <Badge colorScheme="blue" fontSize="sm">
                    {group.number}
                  </Badge>
                )}
                <Text fontWeight="bold" fontSize="md">
                  {group.headerValues['GroupName'] || group.headerValues['0'] || 'Grupa bez nazwy'}
                </Text>
                <Badge colorScheme="gray" fontSize="xs">
                  Poziom {level}
                </Badge>
              </HStack>
              
              {(group.headerValues['GroupDescription'] || group.headerValues['1']) && (
                <Text fontSize="sm" color="gray.600">
                  {group.headerValues['GroupDescription'] || group.headerValues['1']}
                </Text>
              )}
            </VStack>
          </HStack>

          {showGroupActions && !readOnly && (
            <HStack spacing={2}>
              <Button
                leftIcon={<Plus size={16} />}
                size="sm"
                colorScheme="blue"
                variant="ghost"
                onClick={onAddWorkScope}
              >
                Dodaj wiersz
              </Button>
              {template.templateStructure.canBranchGroups && 
                (!template.templateStructure.maxGroupLevel || level < template.templateStructure.maxGroupLevel) && 
                onAddSubGroup && (
                <Button
                  leftIcon={<Plus size={16} />}
                  size="sm"
                  colorScheme="teal"
                  variant="ghost"
                  onClick={onAddSubGroup}
                >
                  Dodaj podgrupę
                </Button>
              )}
              <IconButton
                aria-label="Delete group"
                icon={<Trash2 size={16} />}
                size="sm"
                colorScheme="red"
                variant="ghost"
                onClick={onDeleteGroup}
              />
            </HStack>
          )}
        </HStack>

        {/* Group Header Fields */}
        {visibleGroupHeaderFields.length > 0 && isOpen && (
          <Box mt={4} pt={4} borderTopWidth="1px">
            <Grid templateColumns="repeat(auto-fit, minmax(250px, 1fr))" gap={4}>
              {visibleGroupHeaderFields.map((field) => {
                // Klucz to nazwa enuma GroupHeaderFieldType jako string ("GroupName", "GroupDescription"...)
                const fieldKey = GroupHeaderFieldType[field.type];
                return (
                  <GridItem key={fieldKey}>
                    <GroupHeaderFieldRenderer
                      field={field}
                      value={group.headerValues[fieldKey]}
                      onChange={(value) => handleHeaderValueChange(fieldKey, value)}
                      readOnly={readOnly}
                    />
                  </GridItem>
                );
              })}
            </Grid>
          </Box>
        )}
      </Box>

      {/* Work Scopes Table */}
      <Collapse in={isOpen} animateOpacity>
        {group.workScopes.length > 0 ? (
          <Box overflowX="auto">
            <Table size="sm" variant="simple">
              <Thead bg="gray.50">
                <Tr>
                  <Th w="40px">#</Th>
                  {visibleCalculatedFields.map((field) => (
                    <Th key={field.name} minW="150px">
                      <VStack align="start" spacing={0}>
                        <HStack>
                          <Text>{field.label}</Text>
                          {field.required && <Text color="red.500">*</Text>}
                        </HStack>
                        {field.unit && (
                          <Text fontSize="xs" color="gray.500" fontWeight="normal">
                            [{field.unit}]
                          </Text>
                        )}
                      </VStack>
                    </Th>
                  ))}
                  {visibleGenericFields.map((field) => (
                    <Th key={field.name} minW="150px">
                      <HStack>
                        <Text>{field.label}</Text>
                        {field.required && <Text color="red.500">*</Text>}
                      </HStack>
                    </Th>
                  ))}
                  {!readOnly && <Th w="60px">Akcje</Th>}
                </Tr>
              </Thead>
              <Tbody>
                {group.workScopes.map((workScope, index) => (
                  <Tr key={workScope.id} _hover={{ bg: 'gray.50' }}>
                    <Td>
                      <Text fontSize="sm" color="gray.600">
                        {index + 1}
                      </Text>
                    </Td>
                    
                    {/* Calculated Fields */}
                    {visibleCalculatedFields.map((field) => {
                      // Przygotuj mapę valuesByType dla canAutoCalculate
                      const valuesByType: Record<number, any> = {};
                      calculatedFields.forEach(f => {
                        if (f.name in workScope.calculatedFieldValues) {
                          valuesByType[f.type] = workScope.calculatedFieldValues[f.name];
                        }
                      });
                      const canAutoCalc = canAutoCalculate(field.type, valuesByType);
                      
                      return (
                        <Td key={field.name}>
                          <CalculatedFieldRenderer
                            field={field}
                            value={workScope.calculatedFieldValues[field.name]}
                            onChange={(value) =>
                              handleWorkScopeChange(workScope.id, field.name, value, 'calculated')
                            }
                            allValues={{
                              ...workScope.calculatedFieldValues,
                              ...workScope.genericFieldValues,
                            }}
                            readOnly={readOnly}
                            canAutoCalculate={canAutoCalc}
                            compact
                          />
                        </Td>
                      );
                    })}

                    {/* Generic Fields (excluding collections) */}
                    {visibleGenericFields.map((field) => (
                      <Td key={field.name}>
                        <GenericFieldRenderer
                          field={field}
                          value={workScope.genericFieldValues[field.name]}
                          onChange={(value) =>
                            handleWorkScopeChange(workScope.id, field.name, value, 'generic')
                          }
                          allValues={{
                            ...workScope.calculatedFieldValues,
                            ...workScope.genericFieldValues,
                          }}
                          readOnly={readOnly}
                          compact
                        />
                      </Td>
                    ))}

                    {/* Actions */}
                    {!readOnly && (
                      <Td>
                        <Tooltip label="Usuń wiersz">
                          <IconButton
                            aria-label="Delete work scope"
                            icon={<Trash2 size={14} />}
                            size="xs"
                            colorScheme="red"
                            variant="ghost"
                            onClick={() => onDeleteWorkScope(workScope.id)}
                          />
                        </Tooltip>
                      </Td>
                    )}
                  </Tr>
                ))}

                {/* Group Totals Row */}
                {Object.keys(groupTotals).length > 0 && (
                  <Tr bg="blue.50" fontWeight="bold">
                    <Td>
                      <Text fontSize="sm">Suma:</Text>
                    </Td>
                    {visibleCalculatedFields.map((field) => (
                      <Td key={field.name}>
                        {/* Pokaż sumę tylko dla pól z konfiguracji groupSummaryFields */}
                        {groupTotals[field.name] !== undefined ? (
                          <Text fontSize="sm" fontWeight="bold" color="blue.700">
                            {groupTotals[field.name].toFixed(2)}
                            {field.unit && ` ${field.unit}`}
                          </Text>
                        ) : (
                          <Text fontSize="sm" color="gray.400">
                            -
                          </Text>
                        )}
                      </Td>
                    ))}
                    {visibleGenericFields.map(() => (
                      <Td key={Math.random()}>
                        <Text fontSize="sm" color="gray.400">
                          -
                        </Text>
                      </Td>
                    ))}
                    {!readOnly && <Td />}
                  </Tr>
                )}
              </Tbody>
            </Table>

            {/* Collection Fields Section - rendered below table */}
            {visibleCollectionFields.length > 0 && (
              <Box p={4} bg="purple.25" borderTopWidth="1px">
                <Text fontWeight="bold" fontSize="sm" mb={3} color="purple.800">
                  Zagnieżdżone kolekcje:
                </Text>
                <VStack spacing={4} align="stretch">
                  {group.workScopes.map((workScope, index) => (
                    <Box key={workScope.id}>
                      <Text fontSize="sm" fontWeight="medium" color="gray.700" mb={2}>
                        Pozycja {index + 1}
                      </Text>
                      <VStack spacing={3} align="stretch">
                        {visibleCollectionFields.map((field) => (
                          <GenericFieldRenderer
                            key={field.name}
                            field={field}
                            value={workScope.collectionFieldValues?.[field.name] || []}
                            onChange={(value) =>
                              handleWorkScopeChange(workScope.id, field.name, value, 'collection')
                            }
                            onSelectionChange={
                              field.nestedFields?.isSelectableCollection
                                ? (selectedItem) =>
                                    handleCollectionSelectionChange(workScope.id, field.name, selectedItem)
                                : undefined
                            }
                            allValues={{
                              ...workScope.calculatedFieldValues,
                              ...workScope.genericFieldValues,
                            }}
                            readOnly={readOnly}
                            compact={false}
                          />
                        ))}
                      </VStack>
                    </Box>
                  ))}
                </VStack>
              </Box>
            )}
          </Box>
        ) : (
          <Box p={8} textAlign="center">
            <Text color="gray.500" mb={4}>
              Brak wierszy w tej grupie
            </Text>
            {!readOnly && (
              <Button
                leftIcon={<Plus size={16} />}
                size="sm"
                colorScheme="blue"
                variant="outline"
                onClick={onAddWorkScope}
              >
                Dodaj pierwszy wiersz
              </Button>
            )}
          </Box>
        )}
      </Collapse>

      {/* Subgroups */}
      {group.subGroups && group.subGroups.length > 0 && isOpen && (
        <Box pl={8} pt={4} pb={4} bg="gray.25">
          {group.subGroups.map((subGroup) => (
            <CostEstimateTable
              key={subGroup.id}
              group={subGroup}
              level={level + 1}
              calculatedFields={calculatedFields}
              genericFields={genericFields}
              groupHeaderFields={groupHeaderFields}
              template={template}
              onGroupChange={(updatedSubGroup) => {
                const updatedSubGroups = group.subGroups!.map((sg) =>
                  sg.id === updatedSubGroup.id ? updatedSubGroup : sg
                );
                onGroupChange({
                  ...group,
                  subGroups: updatedSubGroups,
                });
              }}
              onAddWorkScope={() => {
                // Dodaj work scope do podgrupy
                const newWorkScope: CostEstimateWorkScope = {
                  id: `ws-${Date.now()}`,
                  order: subGroup.workScopes.length,
                  calculatedFieldValues: {},
                  genericFieldValues: {},
                };

                const updatedSubGroup: CostEstimateGroup = {
                  ...subGroup,
                  workScopes: [...subGroup.workScopes, newWorkScope],
                };

                const updatedSubGroups = group.subGroups!.map((sg) =>
                  sg.id === subGroup.id ? updatedSubGroup : sg
                );
                
                onGroupChange({
                  ...group,
                  subGroups: updatedSubGroups,
                });
              }}
              onDeleteWorkScope={(workScopeId) => {
                // Usuń work scope z podgrupy
                const updatedSubGroup: CostEstimateGroup = {
                  ...subGroup,
                  workScopes: subGroup.workScopes.filter((ws) => ws.id !== workScopeId),
                };

                const updatedSubGroups = group.subGroups!.map((sg) =>
                  sg.id === subGroup.id ? updatedSubGroup : sg
                );
                
                onGroupChange({
                  ...group,
                  subGroups: updatedSubGroups,
                });
              }}
              onDeleteGroup={() => {
                const updatedSubGroups = group.subGroups!.filter((sg) => sg.id !== subGroup.id);
                onGroupChange({
                  ...group,
                  subGroups: updatedSubGroups.length > 0 ? updatedSubGroups : undefined,
                });
              }}
              onAddSubGroup={() => {
                // Dodaj podgrupę w podgrupie
                const newSubGroup: CostEstimateGroup = {
                  id: `group-${Date.now()}`,
                  parentId: subGroup.id,
                  level: level + 2,
                  order: (subGroup.subGroups?.length || 0),
                  headerValues: {},
                  workScopes: [],
                };

                const updatedSubGroup: CostEstimateGroup = {
                  ...subGroup,
                  subGroups: [...(subGroup.subGroups || []), newSubGroup],
                };

                const updatedSubGroups = group.subGroups!.map((sg) =>
                  sg.id === subGroup.id ? updatedSubGroup : sg
                );
                
                onGroupChange({
                  ...group,
                  subGroups: updatedSubGroups,
                });
              }}
              readOnly={readOnly}
            />
          ))}
        </Box>
      )}
    </Box>
  );
};
