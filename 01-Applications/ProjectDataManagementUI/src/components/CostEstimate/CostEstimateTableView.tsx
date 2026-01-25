import React, { useState, useMemo, useCallback } from 'react';
import {
  Box,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Text,
  IconButton,
  Tooltip,
  Badge,
  HStack,
  Input,
  Checkbox,
  VStack,
  Button,
  InputGroup,
  InputLeftElement,
  Select,
} from '@chakra-ui/react';
import {
  Plus,
  Trash2,
  ChevronDown,
  ChevronRight,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  X,
  Search,
} from 'lucide-react';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
} from '../../types/costEstimate.types.new';
import type {
  ColumnConfigurationWeb,
  GroupHeaderFieldWeb,
  SystemFieldWeb,
  CalculatedFieldWeb,
  GenericFieldWeb,
} from '../../types/costEstimate.types';
import { FieldScope } from '../../types/costEstimate.types';

interface CostEstimateTableViewProps {
  details: CostEstimateDetailsWeb;
  editable?: boolean;
  onDataChange?: (updated: CostEstimateDetailsWeb) => void;
  onAddGroup?: () => void;
  onDeleteGroup?: (groupId: string) => void;
  onAddSubGroup?: (parentGroupId: string) => void;
  onAddItem?: (groupId: string) => void;
  onDeleteItem?: (groupId: string, itemId: string) => void;
}

interface FlatRow {
  type: 'group' | 'item';
  level: number;
  groupId?: string;
  group?: CostEstimateGroupWeb;
  item?: CostEstimateItemWeb;
  itemIndex?: number;
}

export const CostEstimateTableView: React.FC<CostEstimateTableViewProps> = ({
  details,
  editable = false,
  onDataChange,
  onAddGroup,
  onDeleteGroup,
  onAddSubGroup,
  onAddItem,
  onDeleteItem,
}) => {
  const [collapsedGroups, setCollapsedGroups] = useState<Set<string>>(new Set());
  
  // Stan sortowania: { fieldId, direction: 'asc' | 'desc' }
  const [sortConfig, setSortConfig] = useState<{ fieldId: string; direction: 'asc' | 'desc' } | null>(null);
  
  // Stan filtrów: { fieldId: filterValue }
  const [filters, setFilters] = useState<Record<string, string>>({});

  const templateStructure = details.templateStructure;
  
  // Konfiguracja podsumowań z szablonu
  const summaryConfig = templateStructure.summaryConfiguration;
  const showGroupSummary = summaryConfig?.showGroupSummary ?? true;
  const groupSummaryFields = summaryConfig?.groupSummaryFields || [];

  // Funkcje obsługi sortowania
  const handleSort = useCallback((fieldId: string) => {
    setSortConfig((prev) => {
      if (prev?.fieldId === fieldId) {
        if (prev.direction === 'asc') {
          return { fieldId, direction: 'desc' };
        } else {
          return null; // Trzecie kliknięcie usuwa sortowanie
        }
      }
      return { fieldId, direction: 'asc' };
    });
  }, []);

  // Funkcje obsługi filtrowania
  const handleFilterChange = useCallback((fieldId: string, value: string) => {
    setFilters((prev) => {
      if (value === '') {
        const { [fieldId]: _, ...rest } = prev;
        return rest;
      }
      return { ...prev, [fieldId]: value };
    });
  }, []);

  const clearFilter = useCallback((fieldId: string) => {
    setFilters((prev) => {
      const { [fieldId]: _, ...rest } = prev;
      return rest;
    });
  }, []);

  const clearAllFilters = useCallback(() => {
    setFilters({});
  }, []);

  // Typ rozszerzonej kolumny - może być zwykła kolumna lub childField z pola kolekcji
  interface ExpandedColumn {
    type: 'regular' | 'childField';
    originalColumn: ColumnConfigurationWeb;
    fieldDef?: any;
    childField?: any;
    parentFieldDef?: any;
    label: string;
    fieldId: string;
    width?: string;
    isSortable?: boolean;
    isFilterable?: boolean;
    isBoolean?: boolean;
    isNumeric?: boolean;
  }

  // Rozszerz kolumny - dla pól z isCollection dodaj kolumny dla childFields
  const expandedColumns = useMemo((): ExpandedColumn[] => {
    const columns = templateStructure.uiConfiguration?.columns || [];
    const visibleColumns = columns
      .filter((col: ColumnConfigurationWeb) => col.isVisible)
      .sort((a: ColumnConfigurationWeb, b: ColumnConfigurationWeb) => a.order - b.order);

    const result: ExpandedColumn[] = [];

    for (const col of visibleColumns) {
      // Znajdź definicję pola
      let fieldDef: any = templateStructure.groupHeaderFields?.find((f: GroupHeaderFieldWeb) => f.fieldName === col.fieldName);
      if (!fieldDef) {
        fieldDef = templateStructure.systemFields?.find((f: SystemFieldWeb) => f.fieldName === col.fieldName);
      }
      if (!fieldDef) {
        fieldDef = templateStructure.calculatedFields?.find((f: CalculatedFieldWeb) => f.fieldName === col.fieldName);
      }
      if (!fieldDef) {
        fieldDef = templateStructure.genericFields?.find((f: GenericFieldWeb) => f.fieldName === col.fieldName);
      }

      const cfg = fieldDef?.fieldTypeConfig;
      const childFields = fieldDef?.childFields || [];

      // Jeśli pole ma isCollection i childFields, rozwiń na osobne kolumny
      if (cfg?.isCollection && childFields.length > 0) {
        for (const childField of childFields) {
          const childCfg = childField.fieldTypeConfig;
          result.push({
            type: 'childField',
            originalColumn: col,
            childField,
            parentFieldDef: fieldDef,
            label: childCfg?.namePl || childField.label || childField.fieldName,
            fieldId: `${col.fieldId}_${childField.fieldName}`,
            width: '120px',
            isSortable: childField.isSortable ?? false,
            isFilterable: childField.isFilterable ?? false,
            isBoolean: childCfg?.isBoolean ?? false,
            isNumeric: childCfg?.isNumeric ?? false,
          });
        }
      } else {
        // Zwykła kolumna
        const fieldCfg = fieldDef?.fieldTypeConfig;
        result.push({
          type: 'regular',
          originalColumn: col,
          fieldDef,
          label: col.fieldLabel,
          fieldId: col.fieldId,
          width: col.width,
          isSortable: fieldDef?.isSortable ?? false,
          isFilterable: fieldDef?.isFilterable ?? false,
          isBoolean: fieldCfg?.isBoolean ?? false,
          isNumeric: fieldCfg?.isNumeric ?? false,
        });
      }
    }

    return result;
  }, [templateStructure]);

  // Pomocnicza funkcja do pobierania wartości pola pozycji do filtrowania/sortowania
  const getItemFieldValueForColumn = (item: CostEstimateItemWeb, col: { fieldId: string; fieldDef?: any; childField?: any; type: string }): string | number | boolean | undefined => {
    if (col.type === 'childField' && col.childField) {
      // Dla childField szukaj w opcjach
      const optionValue = item.options?.find(opt => 
        opt.fieldValues.some(fv => fv.fieldDefinitionId === col.childField.id)
      )?.fieldValues.find(fv => fv.fieldDefinitionId === col.childField.id)?.value;
      return optionValue;
    }
    
    // Dla zwykłej kolumny
    const fieldDef = col.fieldDef;
    if (!fieldDef) return undefined;
    
    const fieldValue = item.fieldValues.find(fv => fv.fieldDefinitionId === fieldDef.id);
    return fieldValue?.value;
  };

  // Filtruj i sortuj pozycje w grupie
  const filterAndSortItems = useCallback((items: CostEstimateItemWeb[]): CostEstimateItemWeb[] => {
    let result = [...items];
    
    // Filtrowanie
    const activeFilters = Object.entries(filters);
    if (activeFilters.length > 0) {
      result = result.filter(item => {
        return activeFilters.every(([fieldId, filterValue]) => {
          const col = expandedColumns.find(c => c.fieldId === fieldId);
          if (!col) return true;
          
          const itemValue = getItemFieldValueForColumn(item, col);
          
          // Specjalna obsługa dla pól boolean
          if (col.isBoolean) {
            if (filterValue === 'true') return itemValue === true || itemValue === 'true';
            if (filterValue === 'false') return itemValue === false || itemValue === 'false' || itemValue === undefined || itemValue === null;
            return true; // puste = wszystkie
          }
          
          if (itemValue === undefined || itemValue === null) return false;
          
          const strValue = String(itemValue).toLowerCase();
          return strValue.includes(filterValue.toLowerCase());
        });
      });
    }
    
    // Sortowanie
    if (sortConfig) {
      const col = expandedColumns.find(c => c.fieldId === sortConfig.fieldId);
      if (col) {
        result.sort((a, b) => {
          const valueA = getItemFieldValueForColumn(a, col);
          const valueB = getItemFieldValueForColumn(b, col);
          
          // Obsługa undefined/null
          if (valueA === undefined && valueB === undefined) return 0;
          if (valueA === undefined) return sortConfig.direction === 'asc' ? 1 : -1;
          if (valueB === undefined) return sortConfig.direction === 'asc' ? -1 : 1;
          
          // Próba porównania numerycznego
          const numA = parseFloat(String(valueA));
          const numB = parseFloat(String(valueB));
          
          if (!isNaN(numA) && !isNaN(numB)) {
            return sortConfig.direction === 'asc' ? numA - numB : numB - numA;
          }
          
          // Porównanie tekstowe
          const strA = String(valueA).toLowerCase();
          const strB = String(valueB).toLowerCase();
          const comparison = strA.localeCompare(strB, 'pl');
          return sortConfig.direction === 'asc' ? comparison : -comparison;
        });
      }
    }
    
    return result;
  }, [filters, sortConfig, expandedColumns]);

  // Spłaszcz hierarchię grup do wierszy tabeli
  const flatRows = useMemo(() => {
    const rows: FlatRow[] = [];

    const processGroup = (group: CostEstimateGroupWeb, level: number) => {
      // Filtruj i sortuj pozycje grupy
      const filteredItems = filterAndSortItems(group.items || []);
      
      // Jeśli są aktywne filtry i grupa nie ma pasujących pozycji, pomiń ją
      const groupHasActiveFilters = Object.keys(filters).length > 0;
      if (groupHasActiveFilters && filteredItems.length === 0 && (group.childGroups || []).length === 0) {
        return;
      }
      
      // Dodaj wiersz grupy
      rows.push({
        type: 'group',
        level,
        groupId: group.id,
        group,
      });

      // Jeśli grupa nie jest zwinięta, dodaj pozycje
      if (!collapsedGroups.has(group.id)) {
        filteredItems.forEach((item, index) => {
          rows.push({
            type: 'item',
            level: level + 1,
            groupId: group.id,
            item: item,
            itemIndex: index,
          });
        });

        // Rekurencyjnie przetwórz podgrupy
        (group.childGroups || []).forEach((child) => {
          processGroup(child, level + 1);
        });
      }
    };

    (details.rootGroups || []).forEach((group) => processGroup(group, 0));
    return rows;
  }, [details.rootGroups, collapsedGroups, showGroupSummary, filterAndSortItems, filters]);

  const toggleGroupCollapse = (groupId: string) => {
    setCollapsedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(groupId)) {
        next.delete(groupId);
      } else {
        next.add(groupId);
      }
      return next;
    });
  };

  // Pobierz wartość pola grupy
  const getGroupFieldValue = (group: CostEstimateGroupWeb, fieldId: string): string | undefined => {
    const fieldValue = group.fieldValues.find((fv) => fv.fieldDefinitionId === fieldId);
    return fieldValue?.value;
  };

  // Pobierz wartość pola pozycji
  const getItemFieldValue = (
    item: CostEstimateItemWeb,
    fieldId: string
  ): string | undefined => {
    const fieldValue = item.fieldValues.find(
      (fv) => fv.fieldDefinitionId === fieldId
    );
    return fieldValue?.value;
  };

  // Aktualizuj wartość pola grupy
  const updateGroupFieldValue = (groupId: string, fieldId: string, value: string | undefined) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const existingIndex = group.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
        const newFieldValues = [...group.fieldValues];

        if (existingIndex >= 0) {
          if (value === undefined || value === '') {
            newFieldValues.splice(existingIndex, 1);
          } else {
            newFieldValues[existingIndex] = {
              ...newFieldValues[existingIndex],
              value,
            };
          }
        } else if (value !== undefined && value !== '') {
          const ghDef = templateStructure.groupHeaderFields.find((f: GroupHeaderFieldWeb) => f.id === fieldId);
          newFieldValues.push({
            id: `temp_${Date.now()}`,
            fieldDefinitionId: fieldId,
            fieldType: ghDef ? ghDef.fieldType : 0,
            fieldScope: FieldScope.Group,
            fieldLabel: ghDef?.customLabel || '',
            value,
          });
        }

        return {
          ...group,
          fieldValues: newFieldValues,
        };
      }

      return {
        ...group,
        childGroups: group.childGroups.map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };

    onDataChange(updatedDetails);
  };

  // Aktualizuj wartość pola pozycji
  const updateItemFieldValue = (
    groupId: string,
    itemId: string,
    fieldId: string,
    fieldSource: 'system' | 'calculated' | 'generic',
    value: string | undefined
  ) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            const existingIndex = item.fieldValues.findIndex((fv) => {
              return fv.fieldDefinitionId === fieldId;
            });

            const newFieldValues = [...item.fieldValues];

            if (existingIndex >= 0) {
              if (value === undefined || value === '') {
                newFieldValues.splice(existingIndex, 1);
              } else {
                newFieldValues[existingIndex] = {
                  ...newFieldValues[existingIndex],
                  value,
                };
              }
            } else if (value !== undefined && value !== '') {
              // Znajdź definicję pola po id, aby uzupełnić typ i etykietę
              const sysDef = templateStructure.systemFields.find((f: SystemFieldWeb) => f.id === fieldId);
              const calcDef = templateStructure.calculatedFields.find((f: CalculatedFieldWeb) => f.id === fieldId);
              const genDef = templateStructure.genericFields.find((f: GenericFieldWeb) => f.id === fieldId);

              const def: any = sysDef || calcDef || genDef;

              const scopeMap: Record<typeof fieldSource, FieldScope> = {
                system: FieldScope.ItemSystem,
                calculated: FieldScope.ItemCalculated,
                generic: FieldScope.ItemGeneric,
              };

              const newFieldValue = {
                id: `temp_${Date.now()}`,
                fieldDefinitionId: fieldId,
                fieldType: def?.fieldType ?? 0,
                fieldScope: scopeMap[fieldSource],
                fieldName: def?.fieldName ?? '',
                fieldLabel: def?.label ?? '',
                value,
              };

              newFieldValues.push(newFieldValue as any);
            }

            return {
              ...item,
              fieldValues: newFieldValues,
            };
          }
          return item;
        });

        return {
          ...group,
          items,
        };
      }

      return {
        ...group,
        childGroups: group.childGroups.map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };

    onDataChange(updatedDetails);
  };

  // Renderuj input dla pola
  const renderFieldInput = (
    fieldDef: any,
    value: string | undefined,
    onChange: (value: string | undefined) => void,
    disabled: boolean = false
  ) => {
     // Prefer new FieldTypeConfig flags when available
     const cfg = fieldDef.fieldTypeConfig as
       | { isNumeric: boolean; isText: boolean; isDate: boolean; isBoolean: boolean; isCollection: boolean; valueTypeName?: string }
       | undefined;

     // Pola z isCollection są obsługiwane przez expandedColumns jako osobne kolumny childFields
     // więc tutaj pomijamy (nie powinno się zdarzyć, ale dla pewności)
     if (cfg?.isCollection) {
       return <Text fontSize="xs" color="gray.400">—</Text>;
     }

     if (cfg?.isBoolean) {
       return (
         <Checkbox
           isChecked={value === 'true' || value === '1'}
           onChange={(e) => onChange(e.target.checked ? 'true' : 'false')}
           isDisabled={disabled}
           size="sm"
         />
       );
     }

     if (cfg?.isNumeric) {
       return (
         <Input
           type="number"
           value={value || ''}
           onChange={(e) => onChange(e.target.value || undefined)}
           isDisabled={disabled}
           size="sm"
           variant="filled"
         />
       );
     }

     if (cfg?.isDate) {
       return (
         <Input
           type="date"
           value={value || ''}
           onChange={(e) => onChange(e.target.value || undefined)}
           isDisabled={disabled}
           size="sm"
           variant="filled"
         />
       );
     }

     // Legacy fallback based on numeric fieldType
     const fieldType = fieldDef.fieldType;
     if (fieldType === 3) {
       return (
         <Checkbox
           isChecked={value === 'true' || value === '1'}
           onChange={(e) => onChange(e.target.checked ? 'true' : 'false')}
           isDisabled={disabled}
           size="sm"
         />
       );
     }
     if (fieldType === 0 || fieldType === 1) {
       return (
         <Input
           type="number"
           value={value || ''}
           onChange={(e) => onChange(e.target.value || undefined)}
           isDisabled={disabled}
           size="sm"
           variant="filled"
         />
       );
     }
     if (fieldType === 4) {
       return (
         <Input
           type="date"
           value={value || ''}
           onChange={(e) => onChange(e.target.value || undefined)}
           isDisabled={disabled}
           size="sm"
           variant="filled"
         />
       );
     }
     if (fieldType === 5) {
       return (
         <Input
           type="datetime-local"
           value={value || ''}
           onChange={(e) => onChange(e.target.value || undefined)}
           isDisabled={disabled}
           size="sm"
           variant="filled"
         />
       );
     }

     // String (default)
     return (
       <Input
         type="text"
         value={value || ''}
         onChange={(e) => onChange(e.target.value || undefined)}
         isDisabled={disabled}
         size="sm"
         variant="filled"
       />
     );
   };

  // Znajdź pola z isCollection (Opcje/Warianty) dla pozycji
  const collectionFields = useMemo(() => {
    const fields: any[] = [];
    
    // Szukaj w systemFields
    templateStructure.systemFields?.forEach((f: SystemFieldWeb) => {
      if (f.fieldTypeConfig?.isCollection && f.childFields?.length) {
        fields.push({ fieldDef: f, source: 'system' as const });
      }
    });
    
    // Szukaj w calculatedFields
    templateStructure.calculatedFields?.forEach((f: CalculatedFieldWeb) => {
      if ((f as any).fieldTypeConfig?.isCollection && (f as any).childFields?.length) {
        fields.push({ fieldDef: f, source: 'calculated' as const });
      }
    });
    
    // Szukaj w genericFields
    templateStructure.genericFields?.forEach((f: GenericFieldWeb) => {
      if ((f as any).fieldTypeConfig?.isCollection && (f as any).childFields?.length) {
        fields.push({ fieldDef: f, source: 'generic' as const });
      }
    });
    
    return fields;
  }, [templateStructure]);

  // Funkcja do dodawania opcji dla pozycji (opcje są teraz zagnieżdżonymi pozycjami)
  const addOptionToItem = (groupId: string, itemId: string) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            const newOption: CostEstimateItemWeb = {
              id: `temp_opt_${Date.now()}`,
              groupId: groupId,
              parentItemId: itemId,
              order: (item.options || []).length,
              fieldValues: [],
              options: undefined,
              createdAt: new Date().toISOString(),
              updatedAt: undefined,
            };
            return {
              ...item,
              options: [...(item.options || []), newOption],
            };
          }
          return item;
        });
        return { ...group, items };
      }
      return {
        ...group,
        childGroups: group.childGroups.map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };
    onDataChange(updatedDetails);
  };

  // Funkcja do usuwania opcji
  const removeOptionFromItem = (groupId: string, itemId: string, optionId: string) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            return {
              ...item,
              options: (item.options || []).filter(opt => opt.id !== optionId),
            };
          }
          return item;
        });
        return { ...group, items };
      }
      return {
        ...group,
        childGroups: group.childGroups.map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };
    onDataChange(updatedDetails);
  };

  // Funkcja do aktualizacji pola opcji
  const updateOptionFieldValue = (
    groupId: string,
    itemId: string,
    optionId: string,
    fieldId: string,
    fieldSource: 'system' | 'calculated' | 'generic',
    value: string | undefined
  ) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            const options = (item.options || []).map((opt) => {
              if (opt.id === optionId) {
                const existingIndex = opt.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
                const newFieldValues = [...opt.fieldValues];

                if (existingIndex >= 0) {
                  if (value === undefined || value === '') {
                    newFieldValues.splice(existingIndex, 1);
                  } else {
                    newFieldValues[existingIndex] = {
                      ...newFieldValues[existingIndex],
                      value,
                    };
                  }
                } else if (value !== undefined && value !== '') {
                  const sysDef = templateStructure.systemFields?.find((f: SystemFieldWeb) => f.id === fieldId);
                  const calcDef = templateStructure.calculatedFields?.find((f: CalculatedFieldWeb) => f.id === fieldId);
                  const genDef = templateStructure.genericFields?.find((f: GenericFieldWeb) => f.id === fieldId);
                  const def: any = sysDef || calcDef || genDef;

                  const scopeMap: Record<typeof fieldSource, FieldScope> = {
                    system: FieldScope.ItemSystem,
                    calculated: FieldScope.ItemCalculated,
                    generic: FieldScope.ItemGeneric,
                  };

                  newFieldValues.push({
                    id: `temp_${Date.now()}`,
                    fieldDefinitionId: fieldId,
                    fieldType: def?.fieldType ?? 0,
                    fieldScope: scopeMap[fieldSource],
                    fieldName: def?.fieldName ?? '',
                    fieldLabel: def?.label ?? '',
                    value,
                  } as any);
                }

                return { ...opt, fieldValues: newFieldValues };
              }
              return opt;
            });
            return { ...item, options };
          }
          return item;
        });
        return { ...group, items };
      }
      return {
        ...group,
        childGroups: group.childGroups.map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };
    onDataChange(updatedDetails);
  };

  // Pomocnicza funkcja do znalezienia pozycji
  const findItem = (groupId: string, itemId: string): CostEstimateItemWeb | undefined => {
    const findInGroups = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb | undefined => {
      for (const group of groups) {
        if (group.id === groupId) {
          return (group.items || []).find(item => item.id === itemId);
        }
        const found = findInGroups(group.childGroups || []);
        if (found) return found;
      }
      return undefined;
    };
    return findInGroups(details.rootGroups || []);
  };

  // Sprawdzenie czy są aktywne filtry
  const hasActiveFilters = Object.keys(filters).length > 0;

  // Renderuj nagłówki tabeli
  const renderTableHeader = () => {
    return (
      <Thead bgGradient="linear(to-r, blue.600, blue.700)" position="sticky" top={0} zIndex={10}>
        <Tr>
          {/* Kolumna akcji - zamrożona */}
          {editable && (
            <Th
              color="white"
              fontSize="xs"
              py={4}
              minW="80px"
              maxW="80px"
              textAlign="center"
              position="sticky"
              left={0}
              zIndex={11}
              bg="blue.600"
            >
              Akcje
            </Th>
          )}

          {/* Kolumna pozycji - zamrożona */}
          <Th
            color="white"
            fontSize="xs"
            py={4}
            minW="180px"
            position="sticky"
            left={editable ? '80px' : 0}
            zIndex={11}
            bg="blue.600"
          >
            Pozycja
          </Th>

          {/* Kolumny według rozszerzonej konfiguracji (childFields jako osobne kolumny) */}
          {expandedColumns.map((col) => {
            const isSorted = sortConfig?.fieldId === col.fieldId;
            const sortDirection = isSorted ? sortConfig?.direction : null;
            const filterValue = filters[col.fieldId] || '';
            
            return (
              <Th
                key={col.fieldId}
                color="white"
                fontSize="sm"
                py={2}
                minW={col.width || '150px'}
                verticalAlign="top"
              >
                <VStack spacing={1} align="stretch">
                  {/* Nagłówek z nazwą i sortowaniem */}
                  <HStack spacing={1} justify="space-between">
                    <Text whiteSpace="nowrap">{col.label}</Text>
                    {col.isSortable && (
                      <Tooltip label={
                        sortDirection === 'asc' 
                          ? 'Sortuj malejąco' 
                          : sortDirection === 'desc' 
                            ? 'Usuń sortowanie' 
                            : 'Sortuj rosnąco'
                      }>
                        <IconButton
                          aria-label="Sortuj"
                          icon={
                            sortDirection === 'asc' ? (
                              <ArrowUp size={14} />
                            ) : sortDirection === 'desc' ? (
                              <ArrowDown size={14} />
                            ) : (
                              <ArrowUpDown size={14} />
                            )
                          }
                          size="xs"
                          variant="ghost"
                          color={sortDirection ? 'yellow.300' : 'whiteAlpha.700'}
                          _hover={{ color: 'white', bg: 'whiteAlpha.200' }}
                          onClick={() => handleSort(col.fieldId)}
                        />
                      </Tooltip>
                    )}
                  </HStack>
                  
                  {/* Input filtra bezpośrednio pod nazwą */}
                  {col.isFilterable && (
                    col.isBoolean ? (
                      // Select dla pól boolean
                      <Select
                        size="xs"
                        value={filterValue}
                        onChange={(e) => handleFilterChange(col.fieldId, e.target.value)}
                        bg="whiteAlpha.200"
                        border="none"
                        color="white"
                        _hover={{ bg: 'whiteAlpha.300' }}
                        _focus={{ bg: 'whiteAlpha.300', boxShadow: 'none' }}
                        h="24px"
                        fontSize="xs"
                        sx={{
                          '> option': {
                            bg: 'gray.700',
                            color: 'white',
                          },
                        }}
                      >
                        <option value="">Wszystkie</option>
                        <option value="true">Tak</option>
                        <option value="false">Nie</option>
                      </Select>
                    ) : col.isNumeric ? (
                      // Input numeryczny dla pól liczbowych
                      <InputGroup size="xs">
                        <InputLeftElement h="24px" w="24px">
                          <Search size={12} color="white" style={{ opacity: 0.7 }} />
                        </InputLeftElement>
                        <Input
                          type="number"
                          placeholder="Filtruj..."
                          value={filterValue}
                          onChange={(e) => handleFilterChange(col.fieldId, e.target.value)}
                          bg="whiteAlpha.200"
                          border="none"
                          color="white"
                          _placeholder={{ color: 'whiteAlpha.600' }}
                          _hover={{ bg: 'whiteAlpha.300' }}
                          _focus={{ bg: 'whiteAlpha.300', boxShadow: 'none' }}
                          h="24px"
                          pl="24px"
                          fontSize="xs"
                          sx={{
                            '&::-webkit-inner-spin-button, &::-webkit-outer-spin-button': {
                              WebkitAppearance: 'none',
                              margin: 0,
                            },
                            MozAppearance: 'textfield',
                          }}
                        />
                        {filterValue && (
                          <IconButton
                            aria-label="Wyczyść filtr"
                            icon={<X size={10} />}
                            size="xs"
                            variant="ghost"
                            color="whiteAlpha.700"
                            _hover={{ color: 'white' }}
                            position="absolute"
                            right={0}
                            top={0}
                            h="24px"
                            minW="24px"
                            onClick={() => clearFilter(col.fieldId)}
                          />
                        )}
                      </InputGroup>
                    ) : (
                      // Input tekstowy dla pozostałych pól
                      <InputGroup size="xs">
                        <InputLeftElement h="24px" w="24px">
                          <Search size={12} color="white" style={{ opacity: 0.7 }} />
                        </InputLeftElement>
                        <Input
                          placeholder="Filtruj..."
                          value={filterValue}
                          onChange={(e) => handleFilterChange(col.fieldId, e.target.value)}
                          bg="whiteAlpha.200"
                          border="none"
                          color="white"
                          _placeholder={{ color: 'whiteAlpha.600' }}
                          _hover={{ bg: 'whiteAlpha.300' }}
                          _focus={{ bg: 'whiteAlpha.300', boxShadow: 'none' }}
                          h="24px"
                          pl="24px"
                          fontSize="xs"
                        />
                        {filterValue && (
                          <IconButton
                            aria-label="Wyczyść filtr"
                            icon={<X size={10} />}
                            size="xs"
                            variant="ghost"
                            color="whiteAlpha.700"
                            _hover={{ color: 'white' }}
                            position="absolute"
                            right={0}
                            top={0}
                            h="24px"
                            minW="24px"
                            onClick={() => clearFilter(col.fieldId)}
                          />
                        )}
                      </InputGroup>
                    )
                  )}
                </VStack>
              </Th>
            );
          })}
        </Tr>
      </Thead>
    );
  };

  return (
    <Box bg="white" borderRadius="xl" shadow="lg" borderWidth="1px">
      {/* Pasek z przyciskiem czyszczenia filtrów */}
      {hasActiveFilters && (
        <Box px={4} py={2} bg="orange.50" borderBottomWidth="1px" borderBottomColor="orange.200">
          <HStack justify="space-between" align="center">
            <Text fontSize="sm" color="orange.700">
              Aktywne filtry: {Object.keys(filters).length}
            </Text>
            <Button
              size="sm"
              colorScheme="orange"
              variant="ghost"
              leftIcon={<X size={14} />}
              onClick={clearAllFilters}
            >
              Wyczyść wszystkie filtry
            </Button>
          </HStack>
        </Box>
      )}
      
      <Box overflowX="auto">
        {flatRows.length === 0 ? (
          // Brak grup - wyświetl komunikat i przycisk dodawania
          <Box p={8} textAlign="center">
            <Text fontSize="lg" fontWeight="medium" color="gray.600" mb={4}>
              {hasActiveFilters ? 'Brak wyników dla aktywnych filtrów' : 'Brak grup w kosztorysie'}
            </Text>
            <Text fontSize="sm" color="gray.500" mb={6}>
              {hasActiveFilters 
                ? 'Zmień kryteria filtrowania lub wyczyść filtry'
                : 'Rozpocznij tworzenie kosztorysu dodając pierwszą grupę'
              }
            </Text>
            {hasActiveFilters ? (
              <Button
                colorScheme="orange"
                leftIcon={<X size={16} />}
                onClick={clearAllFilters}
              >
                Wyczyść filtry
              </Button>
            ) : (
              editable && onAddGroup && (
                <IconButton
                  aria-label="Dodaj grupę"
                  icon={<Plus size={20} />}
                  colorScheme="green"
                  size="lg"
                  onClick={onAddGroup}
                />
              )
            )}
          </Box>
        ) : (
          <Table size="sm" variant="simple">
          {renderTableHeader()}
          <Tbody>
          {flatRows.map((row, idx) => {
            const indent = row.level * 24;

            if (row.type === 'group' && row.group) {
              const group = row.group;
              const isCollapsed = collapsedGroups.has(group.id);

              return (
                <Tr
                  key={`group-${group.id}-${idx}`}
                  bgGradient={row.level === 0 ? 'linear(to-r, blue.50, blue.100)' : 'linear(to-r, teal.50, teal.100)'}
                  borderTopWidth={row.level === 0 ? '3px' : '2px'}
                  borderTopColor={row.level === 0 ? 'blue.400' : 'teal.300'}
                >
                  {/* Akcje grupy - zamrożona kolumna */}
                  {editable && (
                    <Td
                      p={2}
                      textAlign="center"
                      position="sticky"
                      left={0}
                      zIndex={5}
                      bg={row.level === 0 ? 'blue.50' : 'teal.50'}
                      minW="80px"
                      maxW="80px"
                    >
                      <HStack spacing={1} justify="center">
                        {onAddItem && (
                          <Tooltip label="Dodaj pozycję">
                            <IconButton
                              aria-label="Dodaj pozycję"
                              icon={<Plus size={14} />}
                              size="xs"
                              colorScheme="green"
                              onClick={() => onAddItem(group.id)}
                            />
                          </Tooltip>
                        )}
                        {onAddSubGroup && (templateStructure?.canBranchGroups !== false) && (
                          <Tooltip label="Dodaj podgrupę">
                            <IconButton
                              aria-label="Dodaj podgrupę"
                              icon={<Plus size={14} />}
                              size="xs"
                              colorScheme="blue"
                              onClick={() => onAddSubGroup(group.id)}
                            />
                          </Tooltip>
                        )}
                        {onDeleteGroup && (
                          <Tooltip label="Usuń grupę">
                            <IconButton
                              aria-label="Usuń grupę"
                              icon={<Trash2 size={14} />}
                              size="xs"
                              colorScheme="red"
                              onClick={() => onDeleteGroup(group.id)}
                            />
                          </Tooltip>
                        )}
                      </HStack>
                    </Td>
                  )}

                  {/* Pozycja + expand/collapse - zamrożona kolumna */}
                  <Td
                    p={3}
                    pl={`${indent + 12}px`}
                    position="sticky"
                    left={editable ? '80px' : 0}
                    zIndex={5}
                    bg={row.level === 0 ? 'blue.50' : 'teal.50'}
                    minW="180px"
                  >
                    <HStack spacing={2}>
                      <IconButton
                        aria-label={isCollapsed ? 'Rozwiń' : 'Zwiń'}
                        icon={isCollapsed ? <ChevronRight size={16} /> : <ChevronDown size={16} />}
                        size="xs"
                        variant="ghost"
                        onClick={() => toggleGroupCollapse(group.id)}
                      />
                      <Badge colorScheme={row.level === 0 ? 'blue' : 'teal'} px={3} py={1}>
                        Grupa {row.level + 1}
                      </Badge>
                    </HStack>
                  </Td>

                  {/* Kolumny pól grup - używaj expandedColumns */}
                  {expandedColumns.map((col) => {
                    // Dla childFields (z pola Opcje) - puste dla wiersza grupy
                    if (col.type === 'childField') {
                      return (
                        <Td key={col.fieldId} p={2} bg={row.level === 0 ? 'blue.50' : 'teal.50'}>
                          <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">
                            —
                          </Text>
                        </Td>
                      );
                    }

                    // Sprawdź czy to pole grupy - szukaj po fieldName
                    const groupHeaderField = templateStructure.groupHeaderFields.find((f: GroupHeaderFieldWeb) => f.fieldName === col.originalColumn.fieldName);
                    
                    if (groupHeaderField) {
                      const value = getGroupFieldValue(group, groupHeaderField.id);
                      return (
                        <Td key={col.fieldId} p={2}>
                          {editable ? (
                            renderFieldInput(groupHeaderField, value, (newValue) =>
                              updateGroupFieldValue(group.id, groupHeaderField.id, newValue)
                            )
                          ) : (
                            <Text fontSize="sm">{value || '-'}</Text>
                          )}
                        </Td>
                      );
                    }
                    
                    // Pola pozycji - wyświetl sumy grupy jeśli showGroupSummary
                    if (showGroupSummary) {
                      // Znajdź definicję pola pozycji
                      const systemField = templateStructure.systemFields.find((f: SystemFieldWeb) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName);
                      const calcField = templateStructure.calculatedFields.find((f: CalculatedFieldWeb) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName);
                      const genericField = templateStructure.genericFields.find((f: GenericFieldWeb) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName);
                      const fieldDef = systemField || calcField || genericField;
                      
                      if (fieldDef) {
                        // Sprawdź czy to pole powinno być sumowane
                        const shouldSum = groupSummaryFields.length > 0
                          ? groupSummaryFields.some((sf) => sf.fieldId === col.fieldId || sf.fieldId === fieldDef.id)
                          : (fieldDef.fieldName === 'valueNet' || fieldDef.fieldName === 'valueGross' || fieldDef.fieldName === 'totalVat');

                        if (shouldSum) {
                          const summaryValues = (group as any).summaryValues || {};
                          let sumValue: number | undefined;
                          
                          if (summaryValues[fieldDef.id] !== undefined) {
                            sumValue = summaryValues[fieldDef.id];
                          } else if (fieldDef.fieldName === 'valueNet' && group.totalNet !== undefined) {
                            sumValue = group.totalNet;
                          } else if (fieldDef.fieldName === 'valueGross' && group.totalGross !== undefined) {
                            sumValue = group.totalGross;
                          } else if (fieldDef.fieldName === 'totalVat' && group.totalVat !== undefined) {
                            sumValue = group.totalVat;
                          }

                          return (
                            <Td key={col.fieldId} p={2} textAlign="right" bg={row.level === 0 ? 'blue.50' : 'teal.50'}>
                              <Text fontSize="sm" fontWeight="bold" color={row.level === 0 ? 'blue.700' : 'teal.700'}>
                                {sumValue !== undefined ? `Σ ${sumValue.toFixed(2)}` : '—'}
                              </Text>
                            </Td>
                          );
                        }
                      }
                    }
                    
                    // Pola pozycji bez sumowania - puste dla wiersza grupy
                    return (
                      <Td key={col.fieldId} p={2} bg={row.level === 0 ? 'blue.50' : 'teal.50'}>
                        <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">
                          —
                        </Text>
                      </Td>
                    );
                  })}

                </Tr>
              );
            }

            if (row.type === 'item' && row.item && row.groupId) {
              const item = row.item;

              // Opcje są teraz zagnieżdżonymi pozycjami w item.options
              const itemOptions = item.options || [];

              return (
                <React.Fragment key={`item-${item.id}-${idx}`}>
                  {/* Główny wiersz pozycji */}
                  <Tr _hover={{ bg: 'gray.50' }}>
                    {/* Akcje pozycji - zamrożona kolumna */}
                    {editable && (
                      <Td
                        p={2}
                        textAlign="center"
                        position="sticky"
                        left={0}
                        zIndex={5}
                        bg="white"
                        minW="80px"
                        maxW="80px"
                        _groupHover={{ bg: 'gray.50' }}
                      >
                        <HStack spacing={1} justify="center">
                          {/* Przycisk dodawania opcji - tylko gdy są pola z isCollection */}
                          {collectionFields.length > 0 && (
                            <Tooltip label="Dodaj opcję">
                              <IconButton
                                aria-label="Dodaj opcję"
                                icon={<Plus size={14} />}
                                size="xs"
                                colorScheme="purple"
                                onClick={() => addOptionToItem(row.groupId!, item.id)}
                              />
                            </Tooltip>
                          )}
                          {onDeleteItem && (
                            <Tooltip label="Usuń pozycję">
                              <IconButton
                                aria-label="Usuń pozycję"
                                icon={<Trash2 size={14} />}
                                size="xs"
                                colorScheme="red"
                                onClick={() => onDeleteItem(row.groupId!, item.id)}
                              />
                            </Tooltip>
                          )}
                        </HStack>
                      </Td>
                    )}

                    {/* Pozycja - zamrożona kolumna */}
                    <Td
                      p={3}
                      pl={`${indent + 32}px`}
                      position="sticky"
                      left={editable ? '80px' : 0}
                      zIndex={5}
                      bg="white"
                      minW="180px"
                      _groupHover={{ bg: 'gray.50' }}
                    >
                      <Text fontSize="xs" color="gray.500">
                        {row.itemIndex! + 1}
                      </Text>
                    </Td>

                    {/* Kolumny pól pozycji - używaj expandedColumns */}
                    {expandedColumns.map((col) => {
                      // Sprawdź czy to pole grupy - jeśli tak, zostaw puste dla wiersza pozycji
                      const groupHeaderField = templateStructure.groupHeaderFields.find((f: GroupHeaderFieldWeb) => f.fieldName === col.originalColumn.fieldName);
                      if (groupHeaderField) {
                        return (
                          <Td key={col.fieldId} p={2} bg="gray.50">
                            <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">
                              —
                            </Text>
                          </Td>
                        );
                      }

                      // Dla childField - puste w głównym wierszu, wartości są w wierszach opcji
                      if (col.type === 'childField') {
                        return (
                          <Td key={col.fieldId} p={2} bg="purple.50">
                            <Text fontSize="xs" color="purple.400" fontStyle="italic" textAlign="center">
                              {itemOptions.length > 0 ? `${itemOptions.length} opcji` : '—'}
                            </Text>
                          </Td>
                        );
                      }

                      // Szukaj pola pozycji - sprawdzaj po fieldName
                      let fieldDef: any = col.fieldDef;
                      let fieldSource: 'system' | 'calculated' | 'generic' = 'generic';

                      if (!fieldDef) {
                        // Sprawdź w systemFields
                        fieldDef = templateStructure.systemFields.find((f: SystemFieldWeb) => f.fieldName === col.originalColumn.fieldName);
                        if (fieldDef) {
                          fieldSource = 'system';
                        } else {
                          // Sprawdź w calculatedFields
                          fieldDef = templateStructure.calculatedFields.find((f: CalculatedFieldWeb) => f.fieldName === col.originalColumn.fieldName);
                          if (fieldDef) {
                            fieldSource = 'calculated';
                          } else {
                            // Sprawdź w genericFields
                            fieldDef = templateStructure.genericFields.find((f: GenericFieldWeb) => f.fieldName === col.originalColumn.fieldName);
                            if (fieldDef) {
                              fieldSource = 'generic';
                            }
                          }
                        }
                      } else {
                        // Określ źródło z wcześniej znalezionego fieldDef
                        if (templateStructure.systemFields?.find((f: SystemFieldWeb) => f.id === fieldDef.id)) {
                          fieldSource = 'system';
                        } else if (templateStructure.calculatedFields?.find((f: CalculatedFieldWeb) => f.id === fieldDef.id)) {
                          fieldSource = 'calculated';
                        }
                      }

                      if (fieldDef) {
                        const value = getItemFieldValue(item, fieldDef.id);
                        return (
                          <Td key={col.fieldId} p={2}>
                            {editable ? (
                              renderFieldInput(fieldDef, value, (newValue) =>
                                updateItemFieldValue(
                                  row.groupId!,
                                  item.id,
                                  fieldDef.id,
                                  fieldSource,
                                  newValue
                                )
                              )
                            ) : (
                              <Text fontSize="sm">{value || '-'}</Text>
                            )}
                          </Td>
                        );
                      }

                      return <Td key={col.fieldId} p={2}>-</Td>;
                    })}

                  </Tr>

                  {/* Wiersze opcji - opcje są teraz zagnieżdżonymi pozycjami */}
                  {itemOptions.map((option, optIndex) => (
                    <Tr key={`option-${item.id}-${option.id}`} bg="purple.50" _hover={{ bg: 'purple.100' }}>
                      {/* Akcje opcji - zamrożona kolumna */}
                      {editable && (
                        <Td
                          p={2}
                          textAlign="center"
                          position="sticky"
                          left={0}
                          zIndex={5}
                          bg="purple.50"
                          minW="80px"
                          maxW="80px"
                          _groupHover={{ bg: 'purple.100' }}
                        >
                          <Tooltip label="Usuń opcję">
                            <IconButton
                              aria-label="Usuń opcję"
                              icon={<Trash2 size={12} />}
                              size="xs"
                              colorScheme="red"
                              variant="ghost"
                              onClick={() => removeOptionFromItem(row.groupId!, item.id, option.id)}
                            />
                          </Tooltip>
                        </Td>
                      )}

                      {/* Kolumna pozycji - numer opcji - zamrożona */}
                      <Td
                        p={2}
                        pl={`${indent + 48}px`}
                        position="sticky"
                        left={editable ? '80px' : 0}
                        zIndex={5}
                        bg="purple.50"
                        minW="180px"
                        _groupHover={{ bg: 'purple.100' }}
                      >
                        <Badge colorScheme="purple" size="sm">
                          Opcja {optIndex + 1}
                        </Badge>
                      </Td>

                      {/* Kolumny - childFields są teraz fieldValues opcji */}
                      {expandedColumns.map((col) => {
                        // Dla childField - pokaż input z wartościami opcji
                        if (col.type === 'childField' && col.childField) {
                          // Znajdź wartość w fieldValues opcji po childField.id
                          const optionFieldValue = option.fieldValues.find(
                            fv => fv.fieldDefinitionId === col.childField.id
                          );
                          const childValue = optionFieldValue?.value ?? '';
                          const childCfg = col.childField.fieldTypeConfig;

                          // Znajdź źródło pola
                          let fieldSource: 'system' | 'calculated' | 'generic' = 'system';
                          if (templateStructure.calculatedFields?.find((f: CalculatedFieldWeb) => f.id === col.childField.id)) {
                            fieldSource = 'calculated';
                          } else if (templateStructure.genericFields?.find((f: GenericFieldWeb) => f.id === col.childField.id)) {
                            fieldSource = 'generic';
                          }

                          return (
                            <Td key={col.fieldId} p={2}>
                              {editable ? (
                                childCfg?.isNumeric ? (
                                  <Input
                                    size="sm"
                                    type="number"
                                    value={childValue}
                                    onChange={(e) => updateOptionFieldValue(
                                      row.groupId!,
                                      item.id,
                                      option.id,
                                      col.childField.id,
                                      fieldSource,
                                      e.target.value
                                    )}
                                    variant="filled"
                                    bg="white"
                                  />
                                ) : childCfg?.isBoolean ? (
                                  <Checkbox
                                    size="sm"
                                    isChecked={childValue === 'true'}
                                    onChange={(e) => updateOptionFieldValue(
                                      row.groupId!,
                                      item.id,
                                      option.id,
                                      col.childField.id,
                                      fieldSource,
                                      e.target.checked ? 'true' : 'false'
                                    )}
                                  />
                                ) : (
                                  <Input
                                    size="sm"
                                    type="text"
                                    value={childValue}
                                    onChange={(e) => updateOptionFieldValue(
                                      row.groupId!,
                                      item.id,
                                      option.id,
                                      col.childField.id,
                                      fieldSource,
                                      e.target.value
                                    )}
                                    variant="filled"
                                    bg="white"
                                  />
                                )
                              ) : (
                                <Text fontSize="sm">{childValue || '-'}</Text>
                              )}
                            </Td>
                          );
                        }

                        // Dla innych kolumn - puste
                        return (
                          <Td key={col.fieldId} p={2}>
                            <Text fontSize="xs" color="gray.300" textAlign="center">—</Text>
                          </Td>
                        );
                      })}

                    </Tr>
                  ))}
                </React.Fragment>
              );
            }

            return null;
          })}
        </Tbody>
      </Table>
    )}
      </Box>
    </Box>
  );
};
