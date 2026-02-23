import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
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
  Flex,
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
  GripVertical,
  Layers,
  FolderPlus,
  ListPlus,
  GitBranch,
} from 'lucide-react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragOverlay,
} from '@dnd-kit/core';
import type { DragEndEvent, DragStartEvent } from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import {
  getFieldValueAsString,
  type CostEstimateDetailsWeb,
  type CostEstimateGroupWeb,
  type CostEstimateItemWeb,
  type CostEstimateFieldValueWeb,
} from '../../types/costEstimate.types.new';
import type {
  ColumnConfigurationWeb,
  GroupHeaderFieldWeb,
  SystemFieldWeb,
  CalculatedFieldWeb,
  GenericFieldWeb,
  FieldType,
} from '../../types/costEstimate.types';
import { FieldScope } from '../../types/costEstimate.types';

// ---------------------------------------------------------------------------
// Wydzielone moduły
// ---------------------------------------------------------------------------
import {
  SOURCE_FIELD_TYPES,
  CALCULATED_FIELD_TYPES,
  round2,
  readFieldValue,
  getSourceValues,
  getAllValues,
  getAllOptionValues,
  canComputeFromAvailable,
  computeFieldFromAvailable,
  recalculateItem,
  recalculateOption,
  type AllItemValues,
  type ItemCalcValues,
} from '../../utils/costEstimateCalculations';
import { FormattedNumericInput } from '../common/FormattedNumericInput';
import { UnitComboBox } from '../common/UnitComboBox';
import { SortableGroupRow } from './rows/SortableGroupRow';
import { SortableItemRow } from './rows/SortableItemRow';
import {
  POSITION_COL_MIN_WIDTH,
  type FlatRow,
  type ExpandedColumn,
} from './costEstimateTableTypes';

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

interface CostEstimateTableViewProps {
  details: CostEstimateDetailsWeb;
  editable?: boolean;
  onDataChange?: (updated: CostEstimateDetailsWeb) => void;
  onAddGroup?: () => string | undefined;
  onDeleteGroup?: (groupId: string) => void;
  onAddSubGroup?: (parentGroupId: string) => string | undefined;
  onAddItem?: (groupId: string) => void;
  onDeleteItem?: (groupId: string, itemId: string) => void;
  /** Maksymalna wysokość tabeli — domyślnie 'calc(100vh - 220px)' */
  maxTableHeight?: string;
}

// ---------------------------------------------------------------------------
// Komponent główny
// ---------------------------------------------------------------------------

export const CostEstimateTableView: React.FC<CostEstimateTableViewProps> = ({
  details,
  editable = false,
  onDataChange,
  onAddGroup,
  onDeleteGroup,
  onAddSubGroup,
  onAddItem,
  onDeleteItem,
  maxTableHeight = 'calc(100vh - 220px)',
}) => {
  const [collapsedGroups, setCollapsedGroups] = useState<Set<string>>(new Set());
  
  // Stan sortowania: { fieldId, direction: 'asc' | 'desc' }
  const [sortConfig, setSortConfig] = useState<{ fieldId: string; direction: 'asc' | 'desc' } | null>(null);
  
  // Stan filtrów: { fieldId: filterValue }
  const [filters, setFilters] = useState<Record<string, string>>({});

  // Stan szerokości kolumn: { fieldId: width w px }
  const [columnWidths, setColumnWidths] = useState<Record<string, number>>({});
  
  // Stan drag and drop
  const [activeId, setActiveId] = useState<string | null>(null);
  
  // Sensory dla drag and drop
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8, // Wymagane przesunięcie o 8px przed rozpoczęciem drag
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );
  
  // Ref do przechowywania stanu resizing
  const resizeRef = useRef<{
    isResizing: boolean;
    columnId: string | null;
    startX: number;
    startWidth: number;
  }>({ isResizing: false, columnId: null, startX: 0, startWidth: 0 });

  const templateStructure = details.templateStructure;
  
  // Konfiguracja podsumowań z szablonu
  const summaryConfig = templateStructure.summaryConfiguration;
  const showGroupSummary = summaryConfig?.showGroupSummary ?? true;
  const groupSummaryFields = summaryConfig?.groupSummaryFields || [];
  const showTotalSummary = summaryConfig?.showTotalSummary ?? true;
  const totalSummaryFields = summaryConfig?.totalSummaryFields || [];

  // Funkcja formatująca wartość do wyświetlania w trybie podglądu
  const formatDisplayValue = useCallback((value: string | undefined, fieldDef?: any): string => {
    if (value === undefined || value === null || value === '') {
      return '—';
    }
    
    const cfg = fieldDef?.fieldTypeConfig as {
      isNumeric?: boolean;
      isBoolean?: boolean;
      isDate?: boolean;
    } | undefined;
    
    // Boolean - wyświetl jako Tak/Nie
    if (cfg?.isBoolean || fieldDef?.fieldType === 3) {
      return value === 'true' || value === '1' ? 'Tak' : 'Nie';
    }
    
    // Liczby - formatuj z separatorem tysięcy i 2 miejscami po przecinku
    if (cfg?.isNumeric || fieldDef?.fieldType === 0 || fieldDef?.fieldType === 1) {
      const num = parseFloat(value);
      if (!isNaN(num)) {
        return num.toLocaleString('pl-PL', {
          minimumFractionDigits: 2,
          maximumFractionDigits: 2,
        });
      }
    }
    
    // Data - formatuj jako DD.MM.YYYY
    if (cfg?.isDate || fieldDef?.fieldType === 4 || fieldDef?.fieldType === 5) {
      const date = new Date(value);
      if (!isNaN(date.getTime())) {
        return date.toLocaleDateString('pl-PL');
      }
    }
    
    return value;
  }, []);

  // ========== SORTOWANIE I FILTROWANIE ==========

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

  // ========== RESIZE KOLUMN ==========

  const handleResizeStart = useCallback((e: React.MouseEvent, columnId: string, currentWidth: number, minWidth: number = 80) => {
    e.preventDefault();
    e.stopPropagation();
    
    resizeRef.current = {
      isResizing: true,
      columnId,
      startX: e.clientX,
      startWidth: currentWidth,
    };

    const handleMouseMove = (moveEvent: MouseEvent) => {
      if (!resizeRef.current.isResizing) return;
      
      const diff = moveEvent.clientX - resizeRef.current.startX;
      const newWidth = Math.max(minWidth, resizeRef.current.startWidth + diff);
      
      setColumnWidths((prev) => ({
        ...prev,
        [resizeRef.current.columnId!]: newWidth,
      }));
    };

    const handleMouseUp = () => {
      resizeRef.current.isResizing = false;
      resizeRef.current.columnId = null;
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const calculateWidthFromLabel = useCallback((label: string): number => {
    const charWidth = 9;
    const basePadding = 50;
    const minWidth = 80;
    const maxWidth = 300;
    const calculatedWidth = label.length * charWidth + basePadding;
    return Math.min(Math.max(calculatedWidth, minWidth), maxWidth);
  }, []);

  const getColumnWidth = useCallback((fieldId: string, defaultWidth?: string, label?: string): number => {
    if (columnWidths[fieldId]) {
      return columnWidths[fieldId];
    }
    if (label) {
      return calculateWidthFromLabel(label);
    }
    if (defaultWidth) {
      const parsed = parseInt(defaultWidth.replace('px', ''), 10);
      if (!isNaN(parsed)) return parsed;
    }
    return 150;
  }, [columnWidths, calculateWidthFromLabel]);

  // ========== EXPANDED COLUMNS ==========

  const expandedColumns = useMemo((): ExpandedColumn[] => {
    const columns = templateStructure.uiConfiguration?.columns || [];
    const sortedColumns = [...columns]
      .sort((a: ColumnConfigurationWeb, b: ColumnConfigurationWeb) => a.order - b.order);

    const result: ExpandedColumn[] = [];

    for (const col of sortedColumns) {
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
          // Etykieta użytkownika (label) ma priorytet nad generyczną nazwą typu (namePl)
          const childLabel = childField.label || childCfg?.namePl || childField.fieldName || 'Pole';
          result.push({
            type: 'childField',
            originalColumn: col,
            childField,
            parentFieldDef: fieldDef,
            label: childLabel,
            fieldId: `${col.fieldId}_${childField.fieldName}`,
            width: undefined,
            isSortable: childField.isSortable ?? false,
            isFilterable: childField.isFilterable ?? false,
            isBoolean: childCfg?.isBoolean ?? false,
            isNumeric: childCfg?.isNumeric ?? false,
          });
        }
      } else {
        const fieldCfg = fieldDef?.fieldTypeConfig;
        const label = col.fieldLabel || fieldDef?.label || fieldDef?.fieldTypeConfig?.namePl || col.fieldName || 'Kolumna';
        result.push({
          type: 'regular',
          originalColumn: col,
          fieldDef,
          label,
          fieldId: col.fieldId,
          width: undefined,
          isSortable: fieldDef?.isSortable ?? false,
          isFilterable: fieldDef?.isFilterable ?? false,
          isBoolean: fieldCfg?.isBoolean ?? false,
          isNumeric: fieldCfg?.isNumeric ?? false,
        });
      }
    }

    return result;
  }, [templateStructure]);

  // ========== FILTROWANIE I SORTOWANIE POZYCJI ==========

  const getItemFieldValueForColumn = (item: CostEstimateItemWeb, col: { fieldId: string; fieldDef?: any; childField?: any; type: string }): string | number | boolean | undefined => {
    if (col.type === 'childField' && col.childField) {
      const optionFieldValue = item.options?.find(opt => 
        opt.fieldValues.some(fv => fv.fieldDefinitionId === col.childField.id)
      )?.fieldValues.find(fv => fv.fieldDefinitionId === col.childField.id);
      return getFieldValueAsString(optionFieldValue);
    }
    const fieldDef = col.fieldDef;
    if (!fieldDef) return undefined;
    const fieldValue = item.fieldValues.find(fv => fv.fieldDefinitionId === fieldDef.id);
    return getFieldValueAsString(fieldValue);
  };

  const filterOptions = useCallback((options: any[], optionFilters: [string, string][]): any[] => {
    if (optionFilters.length === 0) return options;
    
    return options.filter(option => {
      return optionFilters.every(([fieldId, filterValue]) => {
        const col = expandedColumns.find(c => c.fieldId === fieldId);
        if (!col || col.type !== 'childField' || !col.childField) return true;
        
        const optionFieldValue = option.fieldValues?.find(
          (fv: any) => fv.fieldDefinitionId === col.childField.id
        );
        const value = getFieldValueAsString(optionFieldValue);
        
        if (col.isBoolean) {
          if (filterValue === 'true') return value === 'true';
          if (filterValue === 'false') return value === 'false' || value === undefined || value === null;
          return true;
        }
        
        if (value === undefined || value === null) return false;
        return String(value).toLowerCase().includes(filterValue.toLowerCase());
      });
    });
  }, [expandedColumns]);

  const filterAndSortItems = useCallback((items: CostEstimateItemWeb[]): CostEstimateItemWeb[] => {
    let result = [...items];
    
    const activeFilters = Object.entries(filters);
    const itemFilters = activeFilters.filter(([fieldId]) => {
      const col = expandedColumns.find(c => c.fieldId === fieldId);
      return col && col.type !== 'childField';
    });
    const optionFilters = activeFilters.filter(([fieldId]) => {
      const col = expandedColumns.find(c => c.fieldId === fieldId);
      return col && col.type === 'childField';
    });
    
    if (itemFilters.length > 0) {
      result = result.filter(item => {
        return itemFilters.every(([fieldId, filterValue]) => {
          const col = expandedColumns.find(c => c.fieldId === fieldId);
          if (!col) return true;
          const itemValue = getItemFieldValueForColumn(item, col);
          
          if (col.isBoolean) {
            if (filterValue === 'true') return itemValue === true || itemValue === 'true';
            if (filterValue === 'false') return itemValue === false || itemValue === 'false' || itemValue === undefined || itemValue === null;
            return true;
          }
          
          if (itemValue === undefined || itemValue === null) return false;
          return String(itemValue).toLowerCase().includes(filterValue.toLowerCase());
        });
      });
    }
    
    if (optionFilters.length > 0) {
      result = result.map(item => {
        if (!item.options || item.options.length === 0) return item;
        const filteredOpts = filterOptions(item.options, optionFilters);
        return { ...item, options: filteredOpts };
      });
    }
    
    if (sortConfig) {
      const col = expandedColumns.find(c => c.fieldId === sortConfig.fieldId);
      if (col) {
        result.sort((a, b) => {
          const valueA = getItemFieldValueForColumn(a, col);
          const valueB = getItemFieldValueForColumn(b, col);
          
          if (valueA === undefined && valueB === undefined) return 0;
          if (valueA === undefined) return sortConfig.direction === 'asc' ? 1 : -1;
          if (valueB === undefined) return sortConfig.direction === 'asc' ? -1 : 1;
          
          const numA = parseFloat(String(valueA));
          const numB = parseFloat(String(valueB));
          
          if (!isNaN(numA) && !isNaN(numB)) {
            return sortConfig.direction === 'asc' ? numA - numB : numB - numA;
          }
          
          const strA = String(valueA).toLowerCase();
          const strB = String(valueB).toLowerCase();
          const comparison = strA.localeCompare(strB, 'pl');
          return sortConfig.direction === 'asc' ? comparison : -comparison;
        });
      }
    }
    
    return result;
  }, [filters, sortConfig, expandedColumns]);

  // ========== FLAT ROWS ==========

  const flatRows = useMemo(() => {
    const rows: FlatRow[] = [];

    const processGroup = (group: CostEstimateGroupWeb, level: number, parentNumber: string, indexInParent: number) => {
      const groupNumber = parentNumber ? `${parentNumber}.${indexInParent + 1}` : `${indexInParent + 1}`;
      const filteredItems = filterAndSortItems(group.items || []);
      
      const groupHasActiveFilters = Object.keys(filters).length > 0;
      if (groupHasActiveFilters && filteredItems.length === 0 && (group.childGroups || []).length === 0) {
        return;
      }
      
      rows.push({
        type: 'group',
        level,
        groupId: group.id,
        group,
        groupNumber,
      });

      if (!collapsedGroups.has(group.id)) {
        filteredItems.forEach((item, index) => {
          rows.push({
            type: 'item',
            level: level + 1,
            groupId: group.id,
            item,
            itemIndex: index,
          });
        });

        (group.childGroups || []).forEach((child, childIndex) => {
          processGroup(child, level + 1, groupNumber, childIndex);
        });
      }
    };

    (details.rootGroups || []).forEach((group, index) => processGroup(group, 0, '', index));
    return rows;
  }, [details.rootGroups, collapsedGroups, showGroupSummary, filterAndSortItems, filters]);

  // ========== COLLAPSE / EXPAND ==========

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

  const expandGroup = (groupId: string) => {
    setCollapsedGroups((prev) => {
      const next = new Set(prev);
      next.delete(groupId);
      return next;
    });
  };

  const handleAddGroupWithExpand = () => {
    if (onAddGroup) {
      const newGroupId = onAddGroup();
      if (newGroupId) {
        expandGroup(newGroupId);
      }
    }
  };

  const handleAddSubGroupWithExpand = (parentGroupId: string) => {
    if (onAddSubGroup) {
      expandGroup(parentGroupId);
      const newSubGroupId = onAddSubGroup(parentGroupId);
      if (newSubGroupId) {
        expandGroup(newSubGroupId);
      }
    }
  };

  /** Dodaj pozycję i automatycznie rozwiń grupę nadrzędną */
  const handleAddItemWithExpand = (groupId: string) => {
    if (onAddItem) {
      expandGroup(groupId);
      onAddItem(groupId);
    }
  };

  // ========== DRAG AND DROP ==========
  
  const handleDragStart = (event: DragStartEvent) => {
    setActiveId(event.active.id as string);
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveId(null);

    if (!over || active.id === over.id || !onDataChange) {
      return;
    }

    const activeIdStr = active.id as string;
    const overIdStr = over.id as string;

    const isGroupDrag = activeIdStr.startsWith('group-');
    const isItemDrag = activeIdStr.startsWith('item-');
    const isOptionDrag = activeIdStr.startsWith('option-');

    if (isGroupDrag && overIdStr.startsWith('group-')) {
      const activeGroupId = activeIdStr.replace('group-', '');
      const overGroupId = overIdStr.replace('group-', '');
      handleReorderGroups(activeGroupId, overGroupId);
    } else if (isOptionDrag && overIdStr.startsWith('option-')) {
      const activeParts = activeIdStr.replace('option-', '').split('-');
      const overParts = overIdStr.replace('option-', '').split('-');
      
      if (activeParts.length >= 3 && overParts.length >= 3) {
        const activeGroupId = activeParts[0];
        const activeItemId = activeParts[1];
        const activeOptionId = activeParts.slice(2).join('-');
        const overGroupId = overParts[0];
        const overItemId = overParts[1];
        const overOptionId = overParts.slice(2).join('-');
        
        if (activeGroupId === overGroupId && activeItemId === overItemId) {
          handleReorderOptions(activeGroupId, activeItemId, activeOptionId, overOptionId);
        }
      }
    } else if (isItemDrag) {
      const activeParts = activeIdStr.replace('item-', '').split('-');
      
      if (activeParts.length >= 2) {
        const activeGroupId = activeParts[0];
        const activeItemId = activeParts.slice(1).join('-');
        
        if (overIdStr.startsWith('item-')) {
          const overParts = overIdStr.replace('item-', '').split('-');
          if (overParts.length >= 2) {
            const overGroupId = overParts[0];
            const overItemId = overParts.slice(1).join('-');
            
            if (activeGroupId === overGroupId) {
              handleReorderItems(activeGroupId, activeItemId, overItemId);
            } else {
              handleMoveItemToGroup(activeGroupId, activeItemId, overGroupId, overItemId);
            }
          }
        } else if (overIdStr.startsWith('group-')) {
          const overGroupId = overIdStr.replace('group-', '');
          if (activeGroupId !== overGroupId) {
            handleMoveItemToGroup(activeGroupId, activeItemId, overGroupId, null);
          }
        }
      }
    }
  };

  const handleReorderOptions = (groupId: string, itemId: string, activeOptionId: string, overOptionId: string) => {
    if (!onDataChange) return;

    const reorderOptionsInGroups = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map(group => {
        if (group.id === groupId) {
          const items = group.items || [];
          const itemIndex = items.findIndex(item => item.id === itemId);
          
          if (itemIndex !== -1) {
            const item = items[itemIndex];
            const options = item.options || [];
            const activeIndex = options.findIndex((o: any) => o.id === activeOptionId);
            const overIndex = options.findIndex((o: any) => o.id === overOptionId);
            
            if (activeIndex !== -1 && overIndex !== -1) {
              const newOptions = arrayMove(options, activeIndex, overIndex);
              const newItems = [...items];
              newItems[itemIndex] = {
                ...item,
                options: newOptions.map((opt: any, idx: number) => ({ ...opt, order: idx })),
              };
              return { ...group, items: newItems };
            }
          }
        }
        return {
          ...group,
          childGroups: reorderOptionsInGroups(group.childGroups || []),
        };
      });
    };

    onDataChange({
      ...details,
      rootGroups: reorderOptionsInGroups(details.rootGroups),
    });
  };

  const handleMoveItemToGroup = (
    sourceGroupId: string, 
    itemId: string, 
    targetGroupId: string, 
    targetItemId: string | null
  ) => {
    if (!onDataChange) return;

    let movedItem: CostEstimateItemWeb | null = null;

    const removeItemFromSource = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map(group => {
        if (group.id === sourceGroupId) {
          const items = group.items || [];
          const itemIndex = items.findIndex(item => item.id === itemId);
          if (itemIndex !== -1) {
            movedItem = { ...items[itemIndex] };
            const newItems = [...items];
            newItems.splice(itemIndex, 1);
            return {
              ...group,
              items: newItems.map((item, idx) => ({ ...item, order: idx })),
            };
          }
        }
        return {
          ...group,
          childGroups: removeItemFromSource(group.childGroups || []),
        };
      });
    };

    const addItemToTarget = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map(group => {
        if (group.id === targetGroupId && movedItem) {
          const items = group.items || [];
          const updatedItem = { ...movedItem, groupId: targetGroupId };
          
          if (targetItemId) {
            const targetIndex = items.findIndex(item => item.id === targetItemId);
            if (targetIndex !== -1) {
              const newItems = [...items];
              newItems.splice(targetIndex, 0, updatedItem);
              return {
                ...group,
                items: newItems.map((item, idx) => ({ ...item, order: idx })),
              };
            }
          }
          
          return {
            ...group,
            items: [...items, { ...updatedItem, order: items.length }],
          };
        }
        return {
          ...group,
          childGroups: addItemToTarget(group.childGroups || []),
        };
      });
    };

    const afterRemove = removeItemFromSource(details.rootGroups);
    if (!movedItem) return;
    const afterAdd = addItemToTarget(afterRemove);

    onDataChange({ ...details, rootGroups: afterAdd });
  };

  const handleReorderGroups = (activeGroupId: string, overGroupId: string) => {
    if (!onDataChange) return;

    const findGroupAndParent = (
      groups: CostEstimateGroupWeb[],
      targetId: string,
      parent: CostEstimateGroupWeb | null = null
    ): { group: CostEstimateGroupWeb; parent: CostEstimateGroupWeb | null; siblings: CostEstimateGroupWeb[] } | null => {
      for (const group of groups) {
        if (group.id === targetId) {
          return { group, parent, siblings: groups };
        }
        const found = findGroupAndParent(group.childGroups || [], targetId, group);
        if (found) return found;
      }
      return null;
    };

    const activeInfo = findGroupAndParent(details.rootGroups, activeGroupId);
    const overInfo = findGroupAndParent(details.rootGroups, overGroupId);

    if (!activeInfo || !overInfo) return;

    // Zapobiegaj cyklom — przenoszona grupa nie może być rodzicem docelowej
    const isDescendant = (parentGroup: CostEstimateGroupWeb, childId: string): boolean => {
      if (parentGroup.id === childId) return true;
      return (parentGroup.childGroups || []).some(child => isDescendant(child, childId));
    };
    if (isDescendant(activeInfo.group, overGroupId)) return;

    const sameParent = activeInfo.parent?.id === overInfo.parent?.id;
    
    if (sameParent) {
      const siblings = activeInfo.siblings;
      const activeIndex = siblings.findIndex(g => g.id === activeGroupId);
      const overIndex = siblings.findIndex(g => g.id === overGroupId);

      if (activeIndex === -1 || overIndex === -1) return;

      const reorderedSiblings = arrayMove(siblings, activeIndex, overIndex);
      const updatedSiblings = reorderedSiblings.map((g, idx) => ({ ...g, order: idx }));

      const updateGroupsInTree = (groups: CostEstimateGroupWeb[], parentId: string | undefined): CostEstimateGroupWeb[] => {
        if (parentId === activeInfo.parent?.id || (parentId === undefined && activeInfo.parent === null)) {
          return updatedSiblings.map(g => ({
            ...g,
            childGroups: updateGroupsInTree(g.childGroups || [], g.id),
          }));
        }
        return groups.map(g => ({
          ...g,
          childGroups: updateGroupsInTree(g.childGroups || [], g.id),
        }));
      };

      onDataChange({
        ...details,
        rootGroups: activeInfo.parent === null 
          ? updatedSiblings.map(g => ({ ...g, childGroups: g.childGroups || [] }))
          : updateGroupsInTree(details.rootGroups, undefined),
      });
    } else {
      handleMoveGroupToNewParent(activeGroupId, overGroupId, activeInfo, overInfo);
    }
  };

  const handleMoveGroupToNewParent = (
    activeGroupId: string,
    overGroupId: string,
    activeInfo: { group: CostEstimateGroupWeb; parent: CostEstimateGroupWeb | null; siblings: CostEstimateGroupWeb[] },
    overInfo: { group: CostEstimateGroupWeb; parent: CostEstimateGroupWeb | null; siblings: CostEstimateGroupWeb[] }
  ) => {
    if (!onDataChange) return;

    const movedGroup = { ...activeInfo.group };
    const newParentId = overInfo.parent?.id || null;

    const removeGroupFromSource = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups
        .filter(g => g.id !== activeGroupId)
        .map((g, idx) => ({
          ...g,
          order: idx,
          childGroups: removeGroupFromSource(g.childGroups || []),
        }));
    };

    const addGroupToTarget = (groups: CostEstimateGroupWeb[], parentId: string | null): CostEstimateGroupWeb[] => {
      if (parentId === newParentId) {
        const targetIndex = groups.findIndex(g => g.id === overGroupId);
        if (targetIndex !== -1) {
          const newGroups = [...groups];
          const updatedMovedGroup = { ...movedGroup, parentGroupId: newParentId || undefined };
          newGroups.splice(targetIndex, 0, updatedMovedGroup);
          return newGroups.map((g, idx) => ({ ...g, order: idx }));
        }
        return [...groups, { ...movedGroup, parentGroupId: newParentId || undefined, order: groups.length }];
      }
      return groups.map(g => ({
        ...g,
        childGroups: addGroupToTarget(g.childGroups || [], g.id),
      }));
    };

    const afterRemove = removeGroupFromSource(details.rootGroups);
    const afterAdd = addGroupToTarget(afterRemove, null);

    onDataChange({ ...details, rootGroups: afterAdd });
  };

  const handleReorderItems = (groupId: string, activeItemId: string, overItemId: string) => {
    if (!onDataChange) return;

    const updateGroupItems = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map(group => {
        if (group.id === groupId) {
          const items = group.items || [];
          const activeIndex = items.findIndex(item => item.id === activeItemId);
          const overIndex = items.findIndex(item => item.id === overItemId);

          if (activeIndex === -1 || overIndex === -1) return group;

          const reorderedItems = arrayMove(items, activeIndex, overIndex);
          return {
            ...group,
            items: reorderedItems.map((item, idx) => ({ ...item, order: idx })),
          };
        }
        return {
          ...group,
          childGroups: updateGroupItems(group.childGroups || []),
        };
      });
    };

    onDataChange({
      ...details,
      rootGroups: updateGroupItems(details.rootGroups),
    });
  };

  // ========== SORTABLE IDS ==========

  const getSortableIds = useMemo(() => {
    const ids: string[] = [];
    
    flatRows.forEach(row => {
      if (row.type === 'group' && row.group) {
        ids.push(`group-${row.group.id}`);
      } else if (row.type === 'item' && row.item && row.groupId) {
        ids.push(`item-${row.groupId}-${row.item.id}`);
        const itemComponents = row.item.components || [];
        itemComponents.forEach((comp: CostEstimateItemWeb) => {
          ids.push(`comp-${row.groupId}-${row.item!.id}-${comp.id}`);
          const compOptions = comp.options || [];
          compOptions.forEach((option: any) => {
            ids.push(`comp-option-${row.groupId}-${comp.id}-${option.id}`);
          });
        });
        const itemOptions = row.item.options || [];
        itemOptions.forEach((option: any) => {
          ids.push(`option-${row.groupId}-${row.item!.id}-${option.id}`);
        });
      }
    });
    
    return ids;
  }, [flatRows]);

  // ========== FIELD VALUE GETTERS / SETTERS ==========

  const getGroupFieldValue = (group: CostEstimateGroupWeb, fieldId: string): string | undefined => {
    const fieldValue = group.fieldValues.find((fv) => fv.fieldDefinitionId === fieldId);
    return getFieldValueAsString(fieldValue);
  };

  const getItemFieldValue = (
    item: CostEstimateItemWeb,
    fieldId: string
  ): string | undefined => {
    const fieldValue = item.fieldValues.find(
      (fv) => fv.fieldDefinitionId === fieldId
    );
    return getFieldValueAsString(fieldValue);
  };

  // Helper: tworzy obiekt wartości pola z odpowiednimi typowanymi polami
  const createFieldValueWithTypedValue = (
    existingFieldValue: CostEstimateFieldValueWeb | undefined,
    fieldDef: { id: string; fieldType?: number; fieldTypeConfig?: { isNumeric?: boolean; isBoolean?: boolean; isDate?: boolean }; customLabel?: string; label?: string; fieldName?: string },
    fieldScope: number,
    value: string | undefined
  ): CostEstimateFieldValueWeb => {
    const cfg = fieldDef.fieldTypeConfig;
    
    let stringValue: string | undefined;
    let decimalValue: number | undefined;
    let boolValue: boolean | undefined;
    let dateTimeValue: string | undefined;

    if (value !== undefined && value !== '') {
      if (cfg?.isBoolean) {
        boolValue = value === 'true' || value === '1';
      } else if (cfg?.isNumeric) {
        decimalValue = parseFloat(value) || 0;
      } else if (cfg?.isDate) {
        dateTimeValue = value;
      } else {
        stringValue = value;
      }
    }

    return {
      id: existingFieldValue?.id || `temp_${Date.now()}`,
      fieldDefinitionId: fieldDef.id,
      fieldType: fieldDef.fieldType ?? existingFieldValue?.fieldType ?? 0,
      fieldScope: fieldScope,
      fieldName: fieldDef.fieldName ?? existingFieldValue?.fieldName,
      fieldLabel: fieldDef.customLabel || fieldDef.label || existingFieldValue?.fieldLabel || '',
      stringValue,
      decimalValue,
      boolValue,
      dateTimeValue,
    };
  };

  // ========== MUTACJE DANYCH — GRUPY ==========

  const updateGroupFieldValue = (groupId: string, fieldId: string, value: string | undefined) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const existingIndex = group.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
        const newFieldValues = [...group.fieldValues];
        const ghDef = templateStructure.groupHeaderFields.find((f: GroupHeaderFieldWeb) => f.id === fieldId);

        if (existingIndex >= 0) {
          if (value === undefined || value === '') {
            newFieldValues.splice(existingIndex, 1);
          } else {
            newFieldValues[existingIndex] = createFieldValueWithTypedValue(
              newFieldValues[existingIndex],
              ghDef || { id: fieldId },
              FieldScope.Group,
              value
            );
          }
        } else if (value !== undefined && value !== '') {
          newFieldValues.push(createFieldValueWithTypedValue(
            undefined,
            ghDef || { id: fieldId },
            FieldScope.Group,
            value
          ));
        }

        return { ...group, fieldValues: newFieldValues };
      }
      return {
        ...group,
        childGroups: (group.childGroups || []).map(updateGroup),
      };
    };

    onDataChange({
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    });
  };

  // ========== MUTACJE DANYCH — POZYCJE ==========

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
            const existingIndex = item.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
            let newFieldValues = [...item.fieldValues];

            const sysDef = templateStructure.systemFields.find((f: SystemFieldWeb) => f.id === fieldId);
            const calcDef = templateStructure.calculatedFields.find((f: CalculatedFieldWeb) => f.id === fieldId);
            const genDef = templateStructure.genericFields.find((f: GenericFieldWeb) => f.id === fieldId);
            const def: any = sysDef || calcDef || genDef;

            const scopeMap: Record<typeof fieldSource, FieldScope> = {
              system: FieldScope.ItemSystem,
              calculated: FieldScope.ItemCalculated,
              generic: FieldScope.ItemGeneric,
            };

            if (existingIndex >= 0) {
              if (value === undefined || value === '') {
                newFieldValues.splice(existingIndex, 1);
              } else {
                newFieldValues[existingIndex] = createFieldValueWithTypedValue(
                  newFieldValues[existingIndex],
                  def || { id: fieldId },
                  scopeMap[fieldSource],
                  value
                );
              }
            } else if (value !== undefined && value !== '') {
              newFieldValues.push(createFieldValueWithTypedValue(
                undefined,
                def || { id: fieldId },
                scopeMap[fieldSource],
                value
              ));
            }

            let updatedItem: CostEstimateItemWeb = { ...item, fieldValues: newFieldValues };

            const changedFieldType = def?.fieldType ?? def?.fieldTypeConfig?.fieldType;
            if (SOURCE_FIELD_TYPES.has(changedFieldType)) {
              updatedItem = recalculateItem(updatedItem, templateStructure);
            } else if (CALCULATED_FIELD_TYPES.has(changedFieldType)) {
              updatedItem = recalculateItem(updatedItem, templateStructure, changedFieldType);
            }

            // Gdy zmieniono ilość (101) → przelicz opcje/warianty
            if (changedFieldType === 101 && updatedItem.options && updatedItem.options.length > 0) {
              const recalculatedOptions = updatedItem.options.map((opt) => ({
                ...opt,
                fieldValues: recalculateOption(opt.fieldValues || [], templateStructure, updatedItem),
              }));
              updatedItem = { ...updatedItem, options: recalculatedOptions };
            }

            return updatedItem;
          }
          return item;
        });
        return { ...group, items };
      }
      return {
        ...group,
        childGroups: (group.childGroups || []).map(updateGroup),
      };
    };

    onDataChange({
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    });
  };

  // ========== RENDEROWANIE INPUTÓW ==========

  const renderFieldInput = (
    fieldDef: any,
    value: string | undefined,
    onChange: (value: string | undefined) => void,
    disabled: boolean = false,
    itemAllValues?: AllItemValues
  ) => {
     const cfg = fieldDef.fieldTypeConfig as
       | { isNumeric: boolean; isText: boolean; isDate: boolean; isBoolean: boolean; isCollection: boolean; valueTypeName?: string }
       | undefined;

     const calcFieldType = fieldDef?.fieldType ?? fieldDef?.fieldTypeConfig?.fieldType;
     const isCalcField = CALCULATED_FIELD_TYPES.has(calcFieldType);
     const shouldBeReadonly = isCalcField && itemAllValues != null && canComputeFromAvailable(calcFieldType, itemAllValues);

     // Pola z isCollection są obsługiwane przez expandedColumns jako osobne kolumny childFields
     if (cfg?.isCollection) {
       return <Text fontSize="xs" color="gray.400">—</Text>;
     }

     // Readonly — pole obliczane lub zablokowane przez komponenty
     if (shouldBeReadonly || disabled) {
       const isNumForDisplay = cfg?.isNumeric || [0, 1].includes(fieldDef.fieldType);
       const displayValue = value !== undefined && value !== ''
         ? (isNumForDisplay
             ? parseFloat(value).toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
             : value)
         : '—';
       return (
         <Text 
           fontSize="sm" 
           textAlign={isNumForDisplay ? 'right' : 'left'}
           fontWeight="medium"
           bg="gray.100" 
           px={2} 
           py={1} 
           borderRadius="md"
           color="gray.700"
           title={disabled ? 'Wartość obliczana z komponentów' : 'Wartość obliczana automatycznie'}
         >
           {displayValue}
         </Text>
       );
     }

     // Pole jednostki — combobox z jednostkami z szablonu
     const isUnitField = fieldDef.fieldType === 102 || fieldDef.fieldTypeConfig?.fieldType === 102;
     if (isUnitField && templateStructure.units && templateStructure.units.length > 0) {
       return (
         <UnitComboBox
           units={templateStructure.units}
           value={value}
           onChange={onChange}
           disabled={disabled}
         />
       );
     }

     if (cfg?.isBoolean) {
       return (
         <Flex justify="center" align="center" w="100%" h="100%">
           <Checkbox
             isChecked={value === 'true' || value === '1'}
             onChange={(e) => onChange(e.target.checked ? 'true' : 'false')}
             isDisabled={disabled}
             size="md"
             colorScheme="blue"
             borderColor="gray.400"
             sx={{
               '.chakra-checkbox__control': {
                 borderWidth: '2px',
                 borderColor: 'gray.400',
                 bg: 'white',
                 _checked: { bg: 'blue.500', borderColor: 'blue.500' },
                 _hover: { borderColor: 'blue.400' },
               },
             }}
           />
         </Flex>
       );
     }

     if (cfg?.isNumeric) {
       return (
         <FormattedNumericInput
           value={value}
           onChange={onChange}
           disabled={disabled}
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
           variant="outline"
           bg="white"
           borderColor="gray.300"
           _hover={{ borderColor: 'blue.400' }}
           _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px var(--chakra-colors-blue-500)' }}
         />
       );
     }

     // Legacy fallback based on numeric fieldType
     const fieldType = fieldDef.fieldType;
     if (fieldType === 3) {
       return (
         <Flex justify="center" align="center" w="100%" h="100%">
           <Checkbox
             isChecked={value === 'true' || value === '1'}
             onChange={(e) => onChange(e.target.checked ? 'true' : 'false')}
             isDisabled={disabled}
             size="md"
             colorScheme="blue"
             borderColor="gray.400"
             sx={{
               '.chakra-checkbox__control': {
                 borderWidth: '2px',
                 borderColor: 'gray.400',
                 bg: 'white',
                 _checked: { bg: 'blue.500', borderColor: 'blue.500' },
                 _hover: { borderColor: 'blue.400' },
               },
             }}
           />
         </Flex>
       );
     }
     if (fieldType === 0 || fieldType === 1) {
       return (
         <FormattedNumericInput
           value={value}
           onChange={onChange}
           disabled={disabled}
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
           variant="outline"
           bg="white"
           borderColor="gray.300"
           _hover={{ borderColor: 'blue.400' }}
           _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px var(--chakra-colors-blue-500)' }}
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
           variant="outline"
           bg="white"
           borderColor="gray.300"
           _hover={{ borderColor: 'blue.400' }}
           _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px var(--chakra-colors-blue-500)' }}
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
         variant="outline"
         bg="white"
         borderColor="gray.300"
         _hover={{ borderColor: 'blue.400' }}
         _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px var(--chakra-colors-blue-500)' }}
       />
     );
   };

  // ========== OPCJE / WARIANTY ==========

  const collectionFields = useMemo(() => {
    const fields: any[] = [];
    templateStructure.systemFields?.forEach((f: SystemFieldWeb) => {
      if (f.fieldTypeConfig?.isCollection && f.childFields?.length) {
        fields.push({ fieldDef: f, source: 'system' as const });
      }
    });
    templateStructure.calculatedFields?.forEach((f: CalculatedFieldWeb) => {
      if ((f as any).fieldTypeConfig?.isCollection && (f as any).childFields?.length) {
        fields.push({ fieldDef: f, source: 'calculated' as const });
      }
    });
    templateStructure.genericFields?.forEach((f: GenericFieldWeb) => {
      if ((f as any).fieldTypeConfig?.isCollection && (f as any).childFields?.length) {
        fields.push({ fieldDef: f, source: 'generic' as const });
      }
    });
    return fields;
  }, [templateStructure]);

  const addOptionToItem = (groupId: string, itemId: string) => {
    if (!onDataChange) return;

    const addOptionTo = (target: CostEstimateItemWeb): CostEstimateItemWeb => {
      const newOption: CostEstimateItemWeb = {
        id: `temp_opt_${Date.now()}`,
        groupId: groupId,
        parentItemId: target.id,
        relationType: 1,
        order: (target.options || []).length,
        fieldValues: [],
        options: undefined,
        createdAt: new Date().toISOString(),
        updatedAt: undefined,
      };
      return { ...target, options: [...(target.options || []), newOption] };
    };

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) return addOptionTo(item);
          const comp = (item.components || []).find(c => c.id === itemId);
          if (comp) {
            return {
              ...item,
              components: (item.components || []).map(c =>
                c.id === itemId ? addOptionTo(c) : c
              ),
            };
          }
          return item;
        });
        return { ...group, items };
      }
      return { ...group, childGroups: (group.childGroups || []).map(updateGroup) };
    };

    onDataChange({ ...details, rootGroups: details.rootGroups.map(updateGroup) });
  };

  const removeOptionFromItem = (groupId: string, itemId: string, optionId: string) => {
    if (!onDataChange) return;

    const removeOption = (target: CostEstimateItemWeb): CostEstimateItemWeb => ({
      ...target,
      options: (target.options || []).filter(opt => opt.id !== optionId),
    });

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) return removeOption(item);
          const comp = (item.components || []).find(c => c.id === itemId);
          if (comp) {
            return {
              ...item,
              components: (item.components || []).map(c =>
                c.id === itemId ? removeOption(c) : c
              ),
            };
          }
          return item;
        });
        return { ...group, items };
      }
      return { ...group, childGroups: (group.childGroups || []).map(updateGroup) };
    };

    onDataChange({ ...details, rootGroups: details.rootGroups.map(updateGroup) });
  };

  // ========== ZARZĄDZANIE KOMPONENTAMI ==========

  /**
   * Sumuje wartości pól kalkulowanych z komponentów i wstawia do pozycji nadrzędnej.
   * Pola o fieldType 203, 204, 206 są sumowane. Pola źródłowe (200, 201) i jednostkowe (202, 205) NIE.
   */
  const sumComponentValuesToItem = (item: CostEstimateItemWeb): CostEstimateItemWeb => {
    const components = item.components || [];
    if (components.length === 0) return item;

    const calcFields = templateStructure.calculatedFields || [];
    const summableFieldTypes = new Set([203, 204, 206]);
    let updatedFieldValues = [...item.fieldValues];

    for (const calcField of calcFields) {
      const ft = calcField.fieldType ?? calcField.fieldTypeConfig?.fieldType;
      if (!summableFieldTypes.has(ft)) continue;

      let sum = 0;
      for (const comp of components) {
        const fv = comp.fieldValues?.find((v: CostEstimateFieldValueWeb) => v.fieldDefinitionId === calcField.id);
        if (fv?.decimalValue !== undefined && fv?.decimalValue !== null) {
          sum += fv.decimalValue;
        } else if (fv?.stringValue) {
          const p = parseFloat(fv.stringValue);
          if (!isNaN(p)) sum += p;
        }
      }

      const idx = updatedFieldValues.findIndex((fv) => fv.fieldDefinitionId === calcField.id);
      const rounded = round2(sum);
      if (idx !== -1) {
        updatedFieldValues[idx] = {
          ...updatedFieldValues[idx],
          decimalValue: rounded,
          stringValue: rounded.toString(),
        };
      } else {
        updatedFieldValues.push({
          id: `comp_sum_${Date.now()}_${calcField.id}`,
          fieldDefinitionId: calcField.id,
          fieldType: ft,
          fieldScope: FieldScope.ItemCalculated,
          fieldName: calcField.fieldName,
          fieldLabel: calcField.label,
          decimalValue: rounded,
          stringValue: rounded.toString(),
        });
      }
    }

    return { ...item, fieldValues: updatedFieldValues };
  };

  const addComponentToItem = (groupId: string, itemId: string) => {
    if (!onDataChange) return;

    const calcFieldIds = new Set(
      (templateStructure.calculatedFields || []).map((f: any) => f.id as string)
    );

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            const existingComponents = item.components || [];
            const isFirstComponent = existingComponents.length === 0;

            const newComponent: CostEstimateItemWeb = {
              id: `temp_comp_${Date.now()}`,
              groupId: groupId,
              parentItemId: itemId,
              relationType: 2,
              order: existingComponents.length,
              fieldValues: [],
              options: undefined,
              components: undefined,
              createdAt: new Date().toISOString(),
              updatedAt: undefined,
            };

            const updatedFieldValues = isFirstComponent
              ? item.fieldValues.map(fv =>
                  calcFieldIds.has(fv.fieldDefinitionId)
                    ? { ...fv, decimalValue: undefined, stringValue: undefined }
                    : fv
                )
              : item.fieldValues;

            return {
              ...item,
              fieldValues: updatedFieldValues,
              components: [...existingComponents, newComponent],
            };
          }
          return item;
        });
        return { ...group, items };
      }
      return { ...group, childGroups: (group.childGroups || []).map(updateGroup) };
    };

    onDataChange({ ...details, rootGroups: details.rootGroups.map(updateGroup) });
  };

  const removeComponentFromItem = (groupId: string, itemId: string, componentId: string) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            const updatedComponents = (item.components || []).filter(c => c.id !== componentId);
            let updatedItem: CostEstimateItemWeb = { ...item, components: updatedComponents };
            if (updatedComponents.length > 0) {
              updatedItem = sumComponentValuesToItem(updatedItem);
            }
            return updatedItem;
          }
          return item;
        });
        return { ...group, items };
      }
      return { ...group, childGroups: (group.childGroups || []).map(updateGroup) };
    };

    onDataChange({ ...details, rootGroups: details.rootGroups.map(updateGroup) });
  };

  const updateComponentFieldValue = (
    groupId: string,
    itemId: string,
    componentId: string,
    fieldId: string,
    fieldSource: 'system' | 'calculated' | 'generic',
    value: string | undefined
  ) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            const updatedComponents = (item.components || []).map((comp) => {
              if (comp.id === componentId) {
                const existingIndex = comp.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
                let newFieldValues = [...comp.fieldValues];

                const sysDef = templateStructure.systemFields?.find((f: SystemFieldWeb) => f.id === fieldId);
                const calcDef = templateStructure.calculatedFields?.find((f: CalculatedFieldWeb) => f.id === fieldId);
                const genDef = templateStructure.genericFields?.find((f: GenericFieldWeb) => f.id === fieldId);
                const def: any = sysDef || calcDef || genDef;

                const scopeMap: Record<typeof fieldSource, FieldScope> = {
                  system: FieldScope.ItemSystem,
                  calculated: FieldScope.ItemCalculated,
                  generic: FieldScope.ItemGeneric,
                };

                if (existingIndex >= 0) {
                  if (value === undefined || value === '') {
                    newFieldValues.splice(existingIndex, 1);
                  } else {
                    newFieldValues[existingIndex] = createFieldValueWithTypedValue(
                      newFieldValues[existingIndex],
                      def || { id: fieldId },
                      scopeMap[fieldSource],
                      value
                    );
                  }
                } else if (value !== undefined && value !== '') {
                  newFieldValues.push(createFieldValueWithTypedValue(
                    undefined,
                    def || { id: fieldId },
                    scopeMap[fieldSource],
                    value
                  ));
                }

                let updatedComp: CostEstimateItemWeb = { ...comp, fieldValues: newFieldValues };
                const changedFieldType = def?.fieldType ?? def?.fieldTypeConfig?.fieldType;
                if (SOURCE_FIELD_TYPES.has(changedFieldType)) {
                  updatedComp = recalculateItem(updatedComp, templateStructure);
                } else if (CALCULATED_FIELD_TYPES.has(changedFieldType)) {
                  updatedComp = recalculateItem(updatedComp, templateStructure, changedFieldType);
                }

                return updatedComp;
              }
              return comp;
            });

            let updatedItem: CostEstimateItemWeb = { ...item, components: updatedComponents };
            updatedItem = sumComponentValuesToItem(updatedItem);
            return updatedItem;
          }
          return item;
        });
        return { ...group, items };
      }
      return { ...group, childGroups: (group.childGroups || []).map(updateGroup) };
    };

    onDataChange({ ...details, rootGroups: details.rootGroups.map(updateGroup) });
  };

  // ========== AKTUALIZACJA PÓL OPCJI ==========

  const updateOptionFieldValue = (
    groupId: string,
    itemId: string,
    optionId: string,
    fieldId: string,
    fieldSource: 'system' | 'calculated' | 'generic',
    value: string | undefined
  ) => {
    if (!onDataChange) return;

    // Znajdź definicję pola — w głównych polach i w childFields
    let def: any = null;
    let fieldType: number | undefined = undefined;
    
    const sysDef = templateStructure.systemFields?.find((f: SystemFieldWeb) => f.id === fieldId);
    const calcDef = templateStructure.calculatedFields?.find((f: CalculatedFieldWeb) => f.id === fieldId);
    const genDef = templateStructure.genericFields?.find((f: GenericFieldWeb) => f.id === fieldId);
    def = sysDef || calcDef || genDef;
    
    if (!def) {
      for (const sysField of (templateStructure.systemFields || [])) {
        if (sysField.childFields) {
          const childDef = sysField.childFields.find((cf: any) => cf.id === fieldId);
          if (childDef) {
            def = childDef;
            fieldType = childDef.fieldType ?? childDef.fieldTypeConfig?.fieldType;
            break;
          }
        }
      }
    }
    
    if (!fieldType && def) {
      fieldType = def.fieldType ?? def.fieldTypeConfig?.fieldType;
    }
    
    const isSelectingOption = fieldType === 104 && value === 'true';

    const updateOwnerOptions = (owner: CostEstimateItemWeb, parentItemForCalc: CostEstimateItemWeb): CostEstimateItemWeb => {
            let updatedOptionFieldValues: any[] = [];
            
            const options = (owner.options || []).map((opt) => {
              if (opt.id === optionId) {
                const existingIndex = opt.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
                const newFieldValues = [...opt.fieldValues];

                const scopeMap: Record<typeof fieldSource, FieldScope> = {
                  system: FieldScope.ItemSystem,
                  calculated: FieldScope.ItemCalculated,
                  generic: FieldScope.ItemGeneric,
                };

                if (existingIndex >= 0) {
                  if (value === undefined || value === '') {
                    newFieldValues.splice(existingIndex, 1);
                  } else {
                    newFieldValues[existingIndex] = createFieldValueWithTypedValue(
                      newFieldValues[existingIndex],
                      def || { id: fieldId },
                      scopeMap[fieldSource],
                      value
                    );
                  }
                } else if (value !== undefined && value !== '') {
                  newFieldValues.push(createFieldValueWithTypedValue(
                    undefined,
                    def || { id: fieldId },
                    scopeMap[fieldSource],
                    value
                  ));
                }

                updatedOptionFieldValues = newFieldValues;
                
                const changedFieldType = def?.fieldType ?? def?.fieldTypeConfig?.fieldType;
                let recalculated: any[];
                if (SOURCE_FIELD_TYPES.has(changedFieldType)) {
                  recalculated = recalculateOption(newFieldValues, templateStructure, parentItemForCalc);
                } else if (CALCULATED_FIELD_TYPES.has(changedFieldType)) {
                  recalculated = recalculateOption(newFieldValues, templateStructure, parentItemForCalc, changedFieldType);
                } else {
                  recalculated = newFieldValues;
                }
                
                updatedOptionFieldValues = recalculated;
                return { ...opt, fieldValues: recalculated };
              } else if (isSelectingOption) {
                // Radio behavior — odznacz pozostałe opcje
                const selectedFieldIdx = opt.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
                if (selectedFieldIdx !== -1 && opt.fieldValues[selectedFieldIdx].boolValue === true) {
                  const newFieldValues = [...opt.fieldValues];
                  newFieldValues[selectedFieldIdx] = {
                    ...newFieldValues[selectedFieldIdx],
                    boolValue: false,
                  };
                  return { ...opt, fieldValues: newFieldValues };
                }
              }
              return opt;
            });
            
            // Kopiuj wartości pól kalkulowanych z wybranej opcji do właściciela
            if (isSelectingOption) {
              const updatedFieldValues = [...owner.fieldValues];
              
              const optionsField = (templateStructure.systemFields || []).find(
                (f: any) => f.fieldTypeConfig?.isCollection && f.childFields?.length > 0
              );
              const childFieldDefs = optionsField?.childFields || [];
              
              for (const childFieldDef of childFieldDefs) {
                const childFieldType = childFieldDef.fieldType ?? childFieldDef.fieldTypeConfig?.fieldType;
                if (childFieldType === undefined || childFieldType < 200 || childFieldType > 206) continue;
                
                const mainCalcField = (templateStructure.calculatedFields || []).find(
                  (cf: any) => (cf.fieldType ?? cf.fieldTypeConfig?.fieldType) === childFieldType
                );
                if (!mainCalcField) continue;
                
                const optFv = updatedOptionFieldValues.find(
                  (fv: any) => fv.fieldDefinitionId === childFieldDef.id
                );
                
                const existingIdx = updatedFieldValues.findIndex(
                  (fv) => fv.fieldDefinitionId === mainCalcField.id
                );
                
                if (existingIdx !== -1) {
                  updatedFieldValues[existingIdx] = {
                    ...updatedFieldValues[existingIdx],
                    stringValue: optFv?.stringValue,
                    decimalValue: optFv?.decimalValue,
                    boolValue: optFv?.boolValue,
                    dateTimeValue: optFv?.dateTimeValue,
                  };
                } else if (optFv) {
                  updatedFieldValues.push({
                    id: `temp_${Date.now()}_${mainCalcField.id}`,
                    fieldDefinitionId: mainCalcField.id,
                    fieldType: childFieldType,
                    fieldScope: FieldScope.ItemCalculated,
                    fieldName: mainCalcField.fieldName,
                    fieldLabel: mainCalcField.label,
                    stringValue: optFv.stringValue,
                    decimalValue: optFv.decimalValue,
                    boolValue: optFv.boolValue,
                    dateTimeValue: optFv.dateTimeValue,
                  });
                }
              }
              
              return { ...owner, options, fieldValues: updatedFieldValues };
            }
            
            return { ...owner, options };
    };

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            return updateOwnerOptions(item, item);
          }
          const comp = (item.components || []).find(c => c.id === itemId);
          if (comp) {
            const updatedComponents = (item.components || []).map(c =>
              c.id === itemId ? updateOwnerOptions(c, item) : c
            );
            let updatedItem: CostEstimateItemWeb = { ...item, components: updatedComponents };
            updatedItem = sumComponentValuesToItem(updatedItem);
            return updatedItem;
          }
          return item;
        });
        return { ...group, items };
      }
      return { ...group, childGroups: (group.childGroups || []).map(updateGroup) };
    };

    onDataChange({ ...details, rootGroups: details.rootGroups.map(updateGroup) });
  };

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

  // ========== NAGŁÓWEK TABELI ==========

  const hasActiveFilters = Object.keys(filters).length > 0;

  const renderTableHeader = () => {
    return (
      <Thead bgGradient="linear(to-r, blue.600, blue.700)" position="sticky" top={0} zIndex={10}>
        <Tr>
          {editable && (
            <Th
              color="white"
              fontSize="xs"
              py={4}
              w="120px"
              minW="120px"
              maxW="120px"
              textAlign="center"
              position="sticky"
              left={0}
              zIndex={11}
              bg="blue.600"
            >
              Akcje
            </Th>
          )}

          <Th
            color="white"
            fontSize="xs"
            py={4}
            w={`${POSITION_COL_MIN_WIDTH}px`}
            minW={`${POSITION_COL_MIN_WIDTH}px`}
            textAlign="center"
            position="sticky"
            left={editable ? '120px' : 0}
            zIndex={11}
            bg="blue.600"
            whiteSpace="nowrap"
          >
            Pozycja
          </Th>

          {expandedColumns.map((col) => {
            const isSorted = sortConfig?.fieldId === col.fieldId;
            const sortDirection = isSorted ? sortConfig?.direction : null;
            const filterValue = filters[col.fieldId] || '';
            const colWidth = getColumnWidth(col.fieldId, col.width, col.label);
            
            return (
              <Th
                key={col.fieldId}
                color="white"
                fontSize="sm"
                py={2}
                w={`${colWidth}px`}
                minW={`${colWidth}px`}
                maxW={`${colWidth}px`}
                verticalAlign="top"
                position="relative"
                userSelect="none"
              >
                <VStack spacing={1} align="stretch">
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
                            sortDirection === 'asc' ? <ArrowUp size={14} /> :
                            sortDirection === 'desc' ? <ArrowDown size={14} /> :
                            <ArrowUpDown size={14} />
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
                  
                  {col.isFilterable && (
                    col.isBoolean ? (
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
                        sx={{ '> option': { bg: 'gray.700', color: 'white' } }}
                      >
                        <option value="">Wszystkie</option>
                        <option value="true">Tak</option>
                        <option value="false">Nie</option>
                      </Select>
                    ) : col.isNumeric ? (
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
                          <Tooltip label="Wyczyść filtr">
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
                          </Tooltip>
                        )}
                      </InputGroup>
                    ) : (
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
                          <Tooltip label="Wyczyść filtr">
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
                          </Tooltip>
                        )}
                      </InputGroup>
                    )
                  )}
                </VStack>
                
                {/* Uchwyt do zmiany szerokości kolumny */}
                <Box
                  position="absolute"
                  right={0}
                  top={0}
                  bottom={0}
                  w="6px"
                  cursor="col-resize"
                  bg="transparent"
                  _hover={{ bg: 'whiteAlpha.400' }}
                  onMouseDown={(e) => handleResizeStart(e, col.fieldId, colWidth, calculateWidthFromLabel(col.label))}
                  zIndex={12}
                />
              </Th>
            );
          })}
        </Tr>
      </Thead>
    );
  };

  // ========== RENDER ==========

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
      
      <Box 
        overflowX="auto"
        sx={{
          position: 'relative',
          maxHeight: maxTableHeight,
          overflowY: 'auto',
          '&::-webkit-scrollbar': { height: '12px', width: '10px' },
          '&::-webkit-scrollbar-track': { background: 'gray.100', borderRadius: '6px' },
          '&::-webkit-scrollbar-thumb': {
            background: 'gray.400',
            borderRadius: '6px',
            '&:hover': { background: 'gray.500' },
          },
          scrollbarWidth: 'auto',
          scrollbarColor: '#A0AEC0 #EDF2F7',
        }}
      >
        {flatRows.length === 0 && !hasActiveFilters ? (
          <Box p={8} textAlign="center">
            <Text fontSize="lg" fontWeight="medium" color="gray.600" mb={4}>
              Brak etapów w kosztorysie
            </Text>
            <Text fontSize="sm" color="gray.500" mb={6}>
              Rozpocznij tworzenie kosztorysu dodając pierwszy etap
            </Text>
            {editable && onAddGroup && (
              <Tooltip label="Dodaj etap">
                <IconButton
                  aria-label="Dodaj etap"
                  icon={<FolderPlus size={20} />}
                  colorScheme="green"
                  size="lg"
                  onClick={handleAddGroupWithExpand}
                />
              </Tooltip>
            )}
          </Box>
        ) : (
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragStart={handleDragStart}
            onDragEnd={handleDragEnd}
          >
            <SortableContext items={getSortableIds} strategy={verticalListSortingStrategy}>
              <Table size="sm" variant="simple" sx={{ 
                tableLayout: 'fixed', 
                minWidth: `${(editable ? 120 : 0) + POSITION_COL_MIN_WIDTH + expandedColumns.reduce((sum, col) => sum + getColumnWidth(col.fieldId, col.width, col.label), 0)}px`,
                width: `${(editable ? 120 : 0) + POSITION_COL_MIN_WIDTH + expandedColumns.reduce((sum, col) => sum + getColumnWidth(col.fieldId, col.width, col.label), 0)}px`,
              }}>
              <colgroup>
                {editable && <col style={{ width: '120px' }} />}
                <col style={{ width: `${POSITION_COL_MIN_WIDTH}px` }} />
                {expandedColumns.map((col) => (
                  <col key={col.fieldId} style={{ width: `${getColumnWidth(col.fieldId, col.width, col.label)}px` }} />
                ))}
              </colgroup>
              {renderTableHeader()}
              <Tbody>
              {flatRows.map((row) => {
                const indent = row.level * 24;

                if (row.type === 'group' && row.group) {
                  const group = row.group;
                  const isCollapsed = collapsedGroups.has(group.id);
                  const sortableId = `group-${group.id}`;

                  return (
                    <SortableGroupRow
                      key={sortableId}
                      id={sortableId}
                      group={group}
                      level={row.level}
                      indent={indent}
                      groupNumber={row.groupNumber || ''}
                      isCollapsed={isCollapsed}
                      editable={editable}
                      templateStructure={templateStructure}
                      showGroupSummary={showGroupSummary}
                      groupSummaryFields={groupSummaryFields}
                      currencySymbol={details.selectedCurrencySymbol || details.selectedCurrencyCode || ''}
                      expandedColumns={expandedColumns}
                      getColumnWidth={getColumnWidth}
                      getGroupFieldValue={getGroupFieldValue}
                      updateGroupFieldValue={updateGroupFieldValue}
                      renderFieldInput={renderFieldInput}
                      formatDisplayValue={formatDisplayValue}
                      toggleGroupCollapse={toggleGroupCollapse}
                      onAddItem={onAddItem ? handleAddItemWithExpand : undefined}
                      onAddSubGroup={onAddSubGroup ? handleAddSubGroupWithExpand : undefined}
                      onDeleteGroup={onDeleteGroup}
                    />
                  );
                }

                if (row.type === 'item' && row.item && row.groupId) {
                  const item = row.item;
                  const sortableId = `item-${row.groupId}-${item.id}`;

                  return (
                    <SortableItemRow
                      key={sortableId}
                      id={sortableId}
                      item={item}
                      groupId={row.groupId}
                      level={row.level}
                      indent={indent}
                      itemNumber={(row.itemIndex ?? 0) + 1}
                      editable={editable}
                      templateStructure={templateStructure}
                      expandedColumns={expandedColumns}
                      getColumnWidth={getColumnWidth}
                      getItemFieldValue={getItemFieldValue}
                      updateItemFieldValue={updateItemFieldValue}
                      updateOptionFieldValue={updateOptionFieldValue}
                      updateComponentFieldValue={updateComponentFieldValue}
                      removeOptionFromItem={removeOptionFromItem}
                      removeComponentFromItem={removeComponentFromItem}
                      renderFieldInput={renderFieldInput}
                      formatDisplayValue={formatDisplayValue}
                      onDeleteItem={onDeleteItem}
                      onAddOption={collectionFields.length > 0 ? addOptionToItem : undefined}
                      onAddComponent={addComponentToItem}
                    />
                  );
                }

                return null;
              })}
              </Tbody>
              
              {/* Stopka z podsumowaniem całkowitym */}
              {showTotalSummary && (
                <tfoot>
                  <Tr bg="purple.100" borderTopWidth="3px" borderTopColor="purple.500">
                    {editable && (
                      <Td p={2} w="120px" minW="120px" maxW="120px">
                        <Badge colorScheme="purple" fontSize="xs">SUMA</Badge>
                      </Td>
                    )}
                    <Td p={2} w={`${POSITION_COL_MIN_WIDTH}px`} minW={`${POSITION_COL_MIN_WIDTH}px`}>
                      <Text fontSize="sm" fontWeight="bold" color="purple.700" whiteSpace="nowrap">
                        PODSUMOWANIE KOSZTORYSU
                      </Text>
                    </Td>
                    {expandedColumns.map((col) => {
                      const colWidth = getColumnWidth(col.fieldId, col.width, col.label);
                      
                      const calcField = templateStructure.calculatedFields?.find(
                        (f: any) => f.id === col.fieldId || f.fieldName === col.originalColumn?.fieldName
                      );
                      const fieldDef = calcField || col.fieldDef;
                      
                      if (fieldDef) {
                        const hasSumInTotalFlag = fieldDef.sumInTotal === true;
                        const isInSummaryFields = totalSummaryFields.length > 0 && 
                          totalSummaryFields.some((sf: any) => sf.fieldId === col.fieldId || sf.fieldId === fieldDef.id);
                        
                        const shouldSum = hasSumInTotalFlag || isInSummaryFields;

                        if (shouldSum) {
                          const fieldName = fieldDef.fieldName;
                          const ft = fieldDef.fieldType ?? fieldDef.fieldTypeConfig?.fieldType;
                          let sumValue: number | undefined;
                          
                          if (fieldName === 'valueNet' || ft === 203) {
                            sumValue = details.totalNet;
                          } else if (fieldName === 'valueGross' || ft === 204) {
                            sumValue = details.totalGross;
                          } else if (fieldName === 'totalVat' || ft === 206) {
                            sumValue = details.totalVat;
                          } else {
                            sumValue = (details as any).summaryValues?.[fieldDef.id];
                          }
                          
                          // Fallback: oblicz z pozycji na żywo
                          if (sumValue === undefined) {
                            const collectAllItems = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb[] => {
                              let items: CostEstimateItemWeb[] = [];
                              for (const g of groups) {
                                if (g.items) items = items.concat(g.items);
                                if (g.childGroups) items = items.concat(collectAllItems(g.childGroups));
                              }
                              return items;
                            };
                            const allItems = collectAllItems(details.rootGroups);
                            sumValue = 0;
                            for (const itm of allItems) {
                              const fv = itm.fieldValues?.find((v: any) => v.fieldDefinitionId === fieldDef.id);
                              if (fv?.decimalValue !== undefined && fv?.decimalValue !== null) {
                                sumValue += fv.decimalValue;
                              } else if (fv?.stringValue) {
                                const parsed = parseFloat(fv.stringValue);
                                if (!isNaN(parsed)) sumValue += parsed;
                              }
                            }
                          }
                          
                          const currencySymbol = details.selectedCurrencySymbol || details.selectedCurrencyCode || '';
                          return (
                            <Td key={col.fieldId} p={2} textAlign="center" bg="purple.100" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
                              <Text fontSize="sm" fontWeight="bold" color="purple.700">
                                Σ {(sumValue ?? 0).toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {currencySymbol}
                              </Text>
                            </Td>
                          );
                        }
                      }
                      
                      return (
                        <Td key={col.fieldId} p={2} bg="purple.100" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
                          <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">—</Text>
                        </Td>
                      );
                    })}
                  </Tr>
                </tfoot>
              )}
            </Table>
          </SortableContext>
        </DndContext>
      )}
      </Box>
      
      {/* Przycisk dodawania grupy */}
      {editable && onAddGroup && flatRows.length > 0 && (
        <Box px={4} py={3} borderTopWidth="1px" borderTopColor="gray.200">
          <Button
            leftIcon={<FolderPlus size={16} />}
            colorScheme="green"
            variant="outline"
            size="sm"
            onClick={handleAddGroupWithExpand}
          >
            Dodaj etap
          </Button>
        </Box>
      )}
    </Box>
  );
};
