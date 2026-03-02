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
  Link,
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
  ChevronsDown,
  ChevronsUp,
  ExternalLink,
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
  isTemporaryId,
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
  isSourceFieldType,
  isCalculatedFieldType,
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
import { FileFieldRenderer } from './FileFieldRenderer';
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
  onAddGroup?: () => Promise<string | undefined>;
  onDeleteGroup?: (groupId: string) => void;
  onAddSubGroup?: (parentGroupId: string) => Promise<string | undefined>;
  onAddItem?: (groupId: string) => Promise<string | undefined>;
  onDeleteItem?: (groupId: string, itemId: string) => void;
  /**
   * Callback do dodania opcji (relationType=1) lub komponentu (relationType=2) do pozycji
   * @param groupId ID grupy
   * @param parentItemId ID pozycji nadrzędnej
   * @param relationType 1=Option, 2=Component
   */
  onAddChildItem?: (groupId: string, parentItemId: string, relationType: 1 | 2) => Promise<string | undefined>;
  /** Callback do uploadu plików do pola typu ItemSystemFiles (Replace All strategy) */
  onUploadFiles?: (itemId: string, fieldDefinitionId: string, files: File[]) => Promise<string[]>;
  /** Callback wywoływany po pomyślnym uploadzie plików — do odświeżenia danych */
  onUploadSuccess?: () => void;
  /** 
   * Callback do autosave pojedynczego pola (z debounce po stronie rodzica)
   * Jeśli podany, zmiany pól będą wysyłane przez ten callback zamiast tylko przez onDataChange
   */
  onFieldAutosave?: (params: {
    entityType: 'group' | 'item';
    entityId: string;
    fieldValueId: string | null;
    fieldDefinitionId: string;
    fieldType: number;
    /** Typ wartości pola - określa które pole DTO wypełnić */
    valueType: 'string' | 'numeric' | 'boolean' | 'date';
    value: string | undefined;
  }) => void;
  /**
   * Callback do zmiany kolejności pozycji w grupie (drag & drop)
   * @param groupId ID grupy
   * @param itemOrders Tablica { itemId, order } z nową kolejnością
   */
  onReorderItems?: (groupId: string, itemOrders: Array<{ itemId: string; order: number }>) => Promise<void>;
  /**
   * Callback do zmiany kolejności grup (drag & drop) — obsługuje też przenoszenie między parentami
   * @param groupOrders Tablica { groupId, parentGroupId, order } z nową strukturą
   */
  onReorderGroups?: (groupOrders: Array<{ groupId: string; parentGroupId: string | null; order: number }>) => Promise<void>;
  /**
   * Callback do przenoszenia pozycji między grupami (drag & drop)
   * @param itemId ID przenoszonej pozycji
   * @param targetGroupId ID grupy docelowej
   */
  onMoveItem?: (itemId: string, targetGroupId: string) => Promise<void>;
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
  onAddChildItem,
  onUploadFiles,
  onUploadSuccess,
  onFieldAutosave,
  onReorderItems,
  onReorderGroups,
  onMoveItem,
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

  /** Regex do wykrywania URL-i w tekście */
  const URL_REGEX = /(https?:\/\/[^\s<>"{}|\\^`[\]]+)/gi;

  /** 
   * Renderuje tekst z wykrytymi linkami jako klikalne elementy.
   * Linki otwierają się w nowym oknie.
   */
  const renderTextWithLinks = useCallback((text: string): React.ReactNode => {
    if (!text) return text;
    
    // Reset regex lastIndex przed użyciem
    URL_REGEX.lastIndex = 0;
    const parts = text.split(URL_REGEX);
    if (parts.length === 1) return text; // Brak linków
    
    return parts.map((part, index) => {
      URL_REGEX.lastIndex = 0;
      if (URL_REGEX.test(part)) {
        URL_REGEX.lastIndex = 0;
        return (
          <Link
            key={index}
            href={part}
            isExternal
            color="blue.500"
            textDecoration="underline"
            _hover={{ color: 'blue.600' }}
            onClick={(e) => e.stopPropagation()}
            display="inline-flex"
            alignItems="center"
            gap={1}
          >
            {part.length > 50 ? `${part.slice(0, 50)}...` : part}
            <ExternalLink size={10} />
          </Link>
        );
      }
      return part;
    });
  }, []);

  /** Sprawdza czy tekst zawiera URL */
  const containsUrl = useCallback((text: string | undefined): boolean => {
    if (!text) return false;
    URL_REGEX.lastIndex = 0;
    return URL_REGEX.test(text);
  }, []);

  // Funkcja formatująca wartość do wyświetlania w trybie podglądu (z obsługą linków)
  const formatDisplayValue = useCallback((value: string | undefined, fieldDef?: any): React.ReactNode => {
    if (value === undefined || value === null || value === '') {
      return '—';
    }
    
    const cfg = fieldDef?.fieldTypeConfig as {
      isNumeric?: boolean;
      isBoolean?: boolean;
      isDate?: boolean;
      isText?: boolean;
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
    
    // Tekst - sprawdź czy zawiera link
    if (containsUrl(value)) {
      return renderTextWithLinks(value);
    }
    
    return value;
  }, [containsUrl, renderTextWithLinks]);

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

  // Copilot: Renderowanie pól zawsze na podstawie definicji z templateStructure.
  // Powód: Po dodaniu grupy/pozycji nie ma już fieldValues[] – UI musi korzystać z definicji pól z szablonu.
  // suggested change: uproszczenie logiki, obsługa null/undefined fieldValues
  const getItemFieldValueForColumn = (item: CostEstimateItemWeb, col: { fieldId: string; fieldDef?: any; childField?: any; type: string }): string | number | boolean | undefined => {
    if (col.type === 'childField' && col.childField) {
      // Szukaj wartości opcji po definicji, ale nie zakładaj obecności fieldValues
      // Copilot: zabezpieczenie przed błędem gdy fieldValues jest undefined
      const foundOption = item.options?.find(opt =>
        Array.isArray(opt.fieldValues) && opt.fieldValues.some(fv => fv.fieldDefinitionId === col.childField.id)
      );
      const optionFieldValue = foundOption && Array.isArray(foundOption.fieldValues)
        ? foundOption.fieldValues.find(fv => fv.fieldDefinitionId === col.childField.id)
        : undefined;
      return getFieldValueAsString(optionFieldValue);
    }
    const fieldDef = col.fieldDef;
    if (!fieldDef) return undefined;
    // Jeśli fieldValues nie istnieje (np. po dodaniu) – zwróć pustą wartość
    if (!item.fieldValues) return undefined;
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

  /** Zbiera rekurencyjnie ID wszystkich grup */
  const collectAllGroupIds = useCallback((groups: CostEstimateGroupWeb[]): string[] => {
    const ids: string[] = [];
    for (const g of groups) {
      ids.push(g.id);
      if (g.childGroups?.length) {
        ids.push(...collectAllGroupIds(g.childGroups));
      }
    }
    return ids;
  }, []);

  const expandAll = useCallback(() => {
    setCollapsedGroups(new Set());
  }, []);

  const collapseAll = useCallback(() => {
    const allIds = collectAllGroupIds(details.rootGroups);
    setCollapsedGroups(new Set(allIds));
  }, [details.rootGroups, collectAllGroupIds]);

  const handleAddGroupWithExpand = async () => {
    if (onAddGroup) {
      const newGroupId = await onAddGroup();
      if (newGroupId) {
        expandGroup(newGroupId);
      }
    }
  };

  const handleAddSubGroupWithExpand = async (parentGroupId: string) => {
    if (onAddSubGroup) {
      expandGroup(parentGroupId);
      const newSubGroupId = await onAddSubGroup(parentGroupId);
      if (newSubGroupId) {
        expandGroup(newSubGroupId);
      }
    }
  };

  /** Dodaj pozycję i automatycznie rozwiń grupę nadrzędną */
  const handleAddItemWithExpand = async (groupId: string) => {
    if (onAddItem) {
      expandGroup(groupId);
      await onAddItem(groupId);
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

    // Separator '::' — odporny na myślniki w GUID-ach
    const isGroupDrag = activeIdStr.startsWith('group::');
    const isItemDrag = activeIdStr.startsWith('item::');
    const isOptionDrag = activeIdStr.startsWith('option::');

    if (isGroupDrag && overIdStr.startsWith('group::')) {
      const activeGroupId = activeIdStr.replace('group::', '');
      const overGroupId = overIdStr.replace('group::', '');
      handleReorderGroups(activeGroupId, overGroupId);
    } else if (isOptionDrag && overIdStr.startsWith('option::')) {
      const activeParts = activeIdStr.replace('option::', '').split('::');
      const overParts = overIdStr.replace('option::', '').split('::');
      
      if (activeParts.length >= 3 && overParts.length >= 3) {
        const activeGroupId = activeParts[0];
        const activeItemId = activeParts[1];
        const activeOptionId = activeParts[2];
        const overGroupId = overParts[0];
        const overItemId = overParts[1];
        const overOptionId = overParts[2];
        
        if (activeGroupId === overGroupId && activeItemId === overItemId) {
          handleReorderOptions(activeGroupId, activeItemId, activeOptionId, overOptionId);
        }
      }
    } else if (isItemDrag) {
      const activeParts = activeIdStr.replace('item::', '').split('::');
      
      if (activeParts.length >= 2) {
        const activeGroupId = activeParts[0];
        const activeItemId = activeParts[1];
        
        if (overIdStr.startsWith('item::')) {
          const overParts = overIdStr.replace('item::', '').split('::');
          if (overParts.length >= 2) {
            const overGroupId = overParts[0];
            const overItemId = overParts[1];
            
            if (activeGroupId === overGroupId) {
              handleReorderItems(activeGroupId, activeItemId, overItemId);
            } else {
              handleMoveItemToGroup(activeGroupId, activeItemId, overGroupId, overItemId);
            }
          }
        } else if (overIdStr.startsWith('group::')) {
          const overGroupId = overIdStr.replace('group::', '');
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

  const handleMoveItemToGroup = async (
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
          // Zachowaj oryginalne ID — backend tylko zmienia groupId pozycji
          const updatedItem: CostEstimateItemWeb = {
            ...movedItem,
            groupId: targetGroupId,
          };
          
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

    // Optimistic update lokalne
    onDataChange({ ...details, rootGroups: afterAdd });

    // Wywołaj API dla przeniesienia pozycji
    if (onMoveItem) {
      try {
        await onMoveItem(itemId, targetGroupId);
      } catch (error) {
        // Rollback — przywróć oryginalny stan
        onDataChange(details);
      }
    }
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

      const updatedRootGroups = activeInfo.parent === null 
        ? updatedSiblings.map(g => ({ ...g, childGroups: g.childGroups || [] }))
        : updateGroupsInTree(details.rootGroups, undefined);

      // Optimistic update lokalne
      onDataChange({
        ...details,
        rootGroups: updatedRootGroups,
      });

      // Wywołaj API dla reorder grup
      if (onReorderGroups) {
        const collectAllGroupOrders = (groups: CostEstimateGroupWeb[], parentId: string | null): Array<{ groupId: string; parentGroupId: string | null; order: number }> => {
          const result: Array<{ groupId: string; parentGroupId: string | null; order: number }> = [];
          groups.forEach((g, idx) => {
            result.push({ groupId: g.id, parentGroupId: parentId, order: idx });
            result.push(...collectAllGroupOrders(g.childGroups || [], g.id));
          });
          return result;
        };
        const allGroupOrders = collectAllGroupOrders(updatedRootGroups, null);
        onReorderGroups(allGroupOrders).catch((error) => {
          // Rollback — przywróć oryginalny stan
          onDataChange(details);
        });
      }
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

    // Optimistic update lokalne
    onDataChange({ ...details, rootGroups: afterAdd });

    // Wywołaj API dla reorder grup (ze wszystkimi grupami i ich nowymi parentami)
    if (onReorderGroups) {
      const collectAllGroupOrders = (groups: CostEstimateGroupWeb[], parentId: string | null): Array<{ groupId: string; parentGroupId: string | null; order: number }> => {
        const result: Array<{ groupId: string; parentGroupId: string | null; order: number }> = [];
        groups.forEach((g, idx) => {
          result.push({ groupId: g.id, parentGroupId: parentId, order: idx });
          result.push(...collectAllGroupOrders(g.childGroups || [], g.id));
        });
        return result;
      };
      const allGroupOrders = collectAllGroupOrders(afterAdd, null);
      onReorderGroups(allGroupOrders).catch((error) => {
        // Rollback — przywróć oryginalny stan
        onDataChange(details);
      });
    }
  };

  const handleReorderItems = async (groupId: string, activeItemId: string, overItemId: string) => {
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

    const updatedRootGroups = updateGroupItems(details.rootGroups);
    
    // Optimistic update lokalne
    onDataChange({
      ...details,
      rootGroups: updatedRootGroups,
    });

    // Wywołaj API dla reorder pozycji w grupie
    if (onReorderItems) {
      // Znajdź grupę i pobierz nową kolejność itemów
      const findGroup = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb | null => {
        for (const g of groups) {
          if (g.id === groupId) return g;
          const found = findGroup(g.childGroups || []);
          if (found) return found;
        }
        return null;
      };
      const targetGroup = findGroup(updatedRootGroups);
      if (targetGroup) {
        const itemOrders = (targetGroup.items || []).map((item, idx) => ({
          itemId: item.id,
          order: idx,
        }));
        try {
          await onReorderItems(groupId, itemOrders);
        } catch (error) {
          // Rollback — przywróć oryginalny stan
          onDataChange(details);
        }
      }
    }
  };

  // ========== SORTABLE IDS ==========

  const getSortableIds = useMemo(() => {
    const ids: string[] = [];
    
    flatRows.forEach(row => {
      if (row.type === 'group' && row.group) {
        ids.push(`group::${row.group.id}`);
      } else if (row.type === 'item' && row.item && row.groupId) {
        ids.push(`item::${row.groupId}::${row.item.id}`);
        const itemComponents = row.item.components || [];
        itemComponents.forEach((comp: CostEstimateItemWeb) => {
          ids.push(`comp::${row.groupId}::${row.item!.id}::${comp.id}`);
          const compOptions = comp.options || [];
          compOptions.forEach((option: any) => {
            ids.push(`comp-option::${row.groupId}::${comp.id}::${option.id}`);
          });
        });
        const itemOptions = row.item.options || [];
        itemOptions.forEach((option: any) => {
          ids.push(`option::${row.groupId}::${row.item!.id}::${option.id}`);
        });
      }
    });
    
    return ids;
  }, [flatRows]);

  // ========== FIELD VALUE GETTERS / SETTERS ==========

  /**
   * Określa typ wartości pola na podstawie fieldTypeConfig lub fieldType
   * Używane do autosave - mówi jakiego pola w DTO użyć (stringValue/decimalValue/boolValue/dateTimeValue)
   */
  const getFieldValueType = (fieldDef: { 
    fieldType?: number; 
    fieldTypeConfig?: { isNumeric?: boolean; isBoolean?: boolean; isDate?: boolean; isText?: boolean } 
  }): 'string' | 'numeric' | 'boolean' | 'date' => {
    const cfg = fieldDef?.fieldTypeConfig;
    
    // Preferuj fieldTypeConfig jeśli dostępny
    if (cfg) {
      if (cfg.isNumeric) return 'numeric';
      if (cfg.isBoolean) return 'boolean';
      if (cfg.isDate) return 'date';
      return 'string';
    }
    
    // Fallback na fieldType gdy fieldTypeConfig niedostępny
    const ft = fieldDef?.fieldType;
    if (ft === undefined) return 'string';
    
    // Numeric types:
    // - ItemCalculated: 200-206
    // - ItemGeneric: 300 (Integer), 301 (Decimal)
    // - ItemSystem: 101 (Quantity)
    // - GroupHeader: 8 (Budget)
    if ((ft >= 200 && ft <= 206) || ft === 300 || ft === 301 || ft === 101 || ft === 8) {
      return 'numeric';
    }
    
    // Boolean types:
    // - ItemSystem: 104 (Selected)
    // - ItemGeneric: 303 (Boolean)
    if (ft === 104 || ft === 303) {
      return 'boolean';
    }
    
    // Date types:
    // - ItemGeneric: 304 (Date), 305 (DateTime)
    // - GroupHeader: 3 (StartDate), 4 (EndDate) - ale tylko w kontekście grup
    if (ft === 304 || ft === 305) {
      return 'date';
    }
    
    // Wszystko inne to string
    return 'string';
  };

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

  /** Zwraca pełne CostEstimateFieldValueWeb dla podanego fieldId — potrzebne dla pól z plikami */
  const getItemFieldValueFull = (
    item: CostEstimateItemWeb,
    fieldId: string
  ): CostEstimateFieldValueWeb | undefined => {
    return item.fieldValues.find(
      (fv) => fv.fieldDefinitionId === fieldId
    );
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

    // Znajdź definicję pola
    const ghDef = templateStructure.groupHeaderFields.find((f: GroupHeaderFieldWeb) => f.id === fieldId);
    const fieldType = ghDef?.fieldType ?? ghDef?.fieldTypeConfig?.fieldType ?? 2; // default: string
    const valueType = getFieldValueType({ fieldType, fieldTypeConfig: ghDef?.fieldTypeConfig });

    // Znajdź grupę i istniejący fieldValueId
    const findGroup = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb | undefined => {
      for (const g of groups) {
        if (g.id === groupId) return g;
        const found = findGroup(g.childGroups || []);
        if (found) return found;
      }
      return undefined;
    };
    const group = findGroup(details.rootGroups);
    const existingFieldValue = group?.fieldValues.find(fv => fv.fieldDefinitionId === fieldId);

    // Wywołaj autosave jeśli dostępne, grupa jest zapisana (nie temp_) i fieldValue istnieje w bazie
    if (onFieldAutosave && !isTemporaryId(groupId) && existingFieldValue?.id && !isTemporaryId(existingFieldValue.id)) {
      onFieldAutosave({
        entityType: 'group',
        entityId: groupId,
        fieldValueId: existingFieldValue.id,
        fieldDefinitionId: fieldId,
        fieldType,
        valueType,
        value,
      });
    }

    // Aktualizuj lokalny stan (optimistic update)
    const updateGroup = (g: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (g.id === groupId) {
        const existingIndex = g.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
        const newFieldValues = [...g.fieldValues];

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

        return { ...g, fieldValues: newFieldValues };
      }
      return {
        ...g,
        childGroups: (g.childGroups || []).map(updateGroup),
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

    // Znajdź definicję pola i istniejący fieldValue
    const sysDef = templateStructure.systemFields.find((f: SystemFieldWeb) => f.id === fieldId);
    const calcDef = templateStructure.calculatedFields.find((f: CalculatedFieldWeb) => f.id === fieldId);
    const genDef = templateStructure.genericFields.find((f: GenericFieldWeb) => f.id === fieldId);
    const def: any = sysDef || calcDef || genDef;
    const fieldType = def?.fieldType ?? def?.fieldTypeConfig?.fieldType ?? 2; // default: string
    const valueType = getFieldValueType({ fieldType, fieldTypeConfig: def?.fieldTypeConfig });

    // Znajdź item i istniejący fieldValueId
    const findItem = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb | undefined => {
      for (const g of groups) {
        if (g.id === groupId) {
          return g.items?.find(i => i.id === itemId);
        }
        const found = findItem(g.childGroups || []);
        if (found) return found;
      }
      return undefined;
    };
    const item = findItem(details.rootGroups);
    const existingFieldValue = item?.fieldValues.find(fv => fv.fieldDefinitionId === fieldId);

    // Copilot: obsługa autosave dla nowego pola (fieldValueId === null)
    // Powód: zgodnie z nowym API, jeśli pole nie istnieje w bazie, należy wysłać PATCH /fields z fieldValueId: null i fieldDefinitionId
    if (onFieldAutosave && !isTemporaryId(itemId)) {
      if (existingFieldValue?.id && !isTemporaryId(existingFieldValue.id)) {
        // Aktualizacja istniejącej wartości pola
        onFieldAutosave({
          entityType: 'item',
          entityId: itemId,
          fieldValueId: existingFieldValue.id,
          fieldDefinitionId: fieldId,
          fieldType,
          valueType,
          value,
        });
      } else {
        // Tworzenie nowej wartości pola
        onFieldAutosave({
          entityType: 'item',
          entityId: itemId,
          fieldValueId: null,
          fieldDefinitionId: fieldId,
          fieldType,
          valueType,
          value,
        });
      }
    }

    // Aktualizuj lokalny stan (optimistic update)
    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((it) => {
          if (it.id === itemId) {
            const existingIndex = it.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
            let newFieldValues = [...it.fieldValues];

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

            let updatedItem: CostEstimateItemWeb = { ...it, fieldValues: newFieldValues };

            const changedFieldType = def?.fieldType ?? def?.fieldTypeConfig?.fieldType;
            const changedFieldScope = def?.fieldScope ?? def?.fieldTypeConfig?.fieldScope;
            if (isSourceFieldType(changedFieldType, changedFieldScope)) {
              updatedItem = recalculateItem(updatedItem, templateStructure);
            } else if (isCalculatedFieldType(changedFieldType, changedFieldScope)) {
              updatedItem = recalculateItem(updatedItem, templateStructure, changedFieldType);
            }

            // Gdy zmieniono ilość (101 lub legacy 1) → przelicz opcje/warianty
            const isQuantityField = changedFieldType === 101 || (changedFieldType === 1 && changedFieldScope === 1);
            if (isQuantityField && updatedItem.options && updatedItem.options.length > 0) {
              const recalculatedOptions = updatedItem.options.map((opt) => ({
                ...opt,
                fieldValues: recalculateOption(opt.fieldValues || [], templateStructure, updatedItem),
              }));
              updatedItem = { ...updatedItem, options: recalculatedOptions };
            }

            return updatedItem;
          }
          return it;
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
    itemAllValues?: AllItemValues,
    itemId?: string,
    fieldDefinitionId?: string,
    files?: import('../../types/costEstimate.types.new').CostEstimateFieldFileWeb[] | null
  ) => {
     const cfg = fieldDef.fieldTypeConfig as
       | { isNumeric: boolean; isText: boolean; isDate: boolean; isBoolean: boolean; isCollection: boolean; isFile?: boolean; valueTypeName?: string }
       | undefined;

     const calcFieldType = fieldDef?.fieldType ?? fieldDef?.fieldTypeConfig?.fieldType;
     const calcFieldScope = fieldDef?.fieldScope ?? fieldDef?.fieldTypeConfig?.fieldScope;
     const isCalcField = isCalculatedFieldType(calcFieldType, calcFieldScope);
     const shouldBeReadonly = isCalcField && itemAllValues != null && canComputeFromAvailable(calcFieldType, itemAllValues);

     // Pola typu pliki (ItemSystemFiles, fieldType = 105)
     if (cfg?.isFile || calcFieldType === 105) {
       // Upload dostępny tylko dla zapisanych pozycji (nie temp_)
       const isSavedItem = itemId && !isTemporaryId(itemId);
       const canUpload = isSavedItem && onUploadFiles && fieldDefinitionId;
       
       return (
         <FileFieldRenderer
           files={files}
           onUpload={canUpload 
             ? (filesToUpload: File[]) => onUploadFiles(itemId, fieldDefinitionId, filesToUpload) 
             : undefined}
           onUploadSuccess={onUploadSuccess}
           readOnly={disabled || !editable || !isSavedItem}
           compact
         />
       );
     }

     // Pola z isCollection są obsługiwane przez expandedColumns jako osobne kolumny childFields
     if (cfg?.isCollection) {
       return <Text fontSize="xs" color="gray.400">—</Text>;
     }

     // Readonly — pole obliczane lub zablokowane przez komponenty
     if (shouldBeReadonly || disabled) {
       const isNumForDisplay = cfg?.isNumeric || [0, 1].includes(fieldDef.fieldType);
       const isTextWithLink = !isNumForDisplay && value && containsUrl(value);
       const displayValue = value !== undefined && value !== ''
         ? (isNumForDisplay
             ? parseFloat(value).toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
             : (isTextWithLink ? renderTextWithLinks(value) : value))
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
           wordBreak="break-word"
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

     // String (default) - z wykrywaniem linków
     const hasLink = containsUrl(value);
     return (
       <HStack spacing={1} w="100%">
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
           flex={1}
         />
         {hasLink && (
           <Tooltip label="Otwórz link">
             <IconButton
               aria-label="Otwórz link"
               icon={<ExternalLink size={14} />}
               size="xs"
               variant="ghost"
               colorScheme="blue"
               onClick={() => {
                 const match = value?.match(URL_REGEX);
                 if (match && match[0]) {
                   window.open(match[0], '_blank', 'noopener,noreferrer');
                 }
               }}
             />
           </Tooltip>
         )}
       </HStack>
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

  const addOptionToItem = async (groupId: string, itemId: string) => {
    // Wywołaj API jeśli dostępne
    if (onAddChildItem) {
      await onAddChildItem(groupId, itemId, 1); // 1 = Option
      return;
    }
    
    // Fallback: tylko lokalna zmiana (legacy)
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

  const addComponentToItem = async (groupId: string, itemId: string) => {
    // Wywołaj API jeśli dostępne
    if (onAddChildItem) {
      await onAddChildItem(groupId, itemId, 2); // 2 = Component
      return;
    }
    
    // Fallback: tylko lokalna zmiana (legacy)
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

    // Znajdź definicję pola i istniejący fieldValue dla autosave
    const sysDef = templateStructure.systemFields?.find((f: SystemFieldWeb) => f.id === fieldId);
    const calcDef = templateStructure.calculatedFields?.find((f: CalculatedFieldWeb) => f.id === fieldId);
    const genDef = templateStructure.genericFields?.find((f: GenericFieldWeb) => f.id === fieldId);
    const def: any = sysDef || calcDef || genDef;
    const fieldType = def?.fieldType ?? def?.fieldTypeConfig?.fieldType ?? 2;
    const valueType = getFieldValueType({ fieldType, fieldTypeConfig: def?.fieldTypeConfig });

    // Znajdź komponent i istniejący fieldValueId dla autosave
    const findComponent = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb | undefined => {
      for (const g of groups) {
        if (g.id === groupId) {
          const parentItem = g.items?.find(i => i.id === itemId);
          return parentItem?.components?.find(c => c.id === componentId);
        }
        const found = findComponent(g.childGroups || []);
        if (found) return found;
      }
      return undefined;
    };
    const component = findComponent(details.rootGroups);
    const existingFieldValue = component?.fieldValues.find(fv => fv.fieldDefinitionId === fieldId);

    // Wywołaj autosave jeśli dostępne, komponent jest zapisany (nie temp_) i fieldValue istnieje w bazie
    // Komponenty to itemy, więc używamy entityType: 'item' z componentId jako entityId
    if (onFieldAutosave && !isTemporaryId(componentId) && existingFieldValue?.id && !isTemporaryId(existingFieldValue.id)) {
      onFieldAutosave({
        entityType: 'item',
        entityId: componentId,
        fieldValueId: existingFieldValue.id,
        fieldDefinitionId: fieldId,
        fieldType,
        valueType,
        value,
      });
    }

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            const updatedComponents = (item.components || []).map((comp) => {
              if (comp.id === componentId) {
                const existingIndex = comp.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
                let newFieldValues = [...comp.fieldValues];

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
                const changedFieldScope = def?.fieldScope ?? def?.fieldTypeConfig?.fieldScope;
                if (isSourceFieldType(changedFieldType, changedFieldScope)) {
                  updatedComp = recalculateItem(updatedComp, templateStructure);
                } else if (isCalculatedFieldType(changedFieldType, changedFieldScope)) {
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
    
    // Znajdź opcję i istniejący fieldValueId dla autosave
    const findOption = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb | undefined => {
      for (const g of groups) {
        if (g.id === groupId) {
          // Szukaj w itemach
          for (const item of (g.items || [])) {
            if (item.id === itemId) {
              return item.options?.find(o => o.id === optionId);
            }
            // Szukaj w komponentach
            for (const comp of (item.components || [])) {
              if (comp.id === itemId) {
                return comp.options?.find(o => o.id === optionId);
              }
            }
          }
        }
        const found = findOption(g.childGroups || []);
        if (found) return found;
      }
      return undefined;
    };
    const option = findOption(details.rootGroups);
    const existingFieldValue = option?.fieldValues.find(fv => fv.fieldDefinitionId === fieldId);

    // Wywołaj autosave jeśli dostępne, opcja jest zapisana (nie temp_) i fieldValue istnieje w bazie
    // Opcje to itemy, więc używamy entityType: 'item' z optionId jako entityId
    const valueType = getFieldValueType({ fieldType, fieldTypeConfig: def?.fieldTypeConfig });
    if (onFieldAutosave && !isTemporaryId(optionId) && existingFieldValue?.id && !isTemporaryId(existingFieldValue.id) && fieldType !== undefined) {
      onFieldAutosave({
        entityType: 'item',
        entityId: optionId,
        fieldValueId: existingFieldValue.id,
        fieldDefinitionId: fieldId,
        fieldType,
        valueType,
        value,
      });
    }

    const isSelectingOption = fieldType === 104 && value === 'true';
    
    // Znajdź pole Selected w systemFields (fieldType 104)
    const selectedFieldDef = (templateStructure.systemFields || []).find(
      (f: any) => f.fieldName === 'selected' || (f.fieldType ?? f.fieldTypeConfig?.fieldType) === 104
    );

    const updateOwnerOptions = (owner: CostEstimateItemWeb, parentItemForCalc: CostEstimateItemWeb): CostEstimateItemWeb => {
            let updatedOptionFieldValues: any[] = [];
            let isThisOptionSelected = false; // czy ta opcja (optionId) jest/będzie zaznaczona
            
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
                const changedFieldScope = def?.fieldScope ?? def?.fieldTypeConfig?.fieldScope;
                let recalculated: any[];
                if (isSourceFieldType(changedFieldType, changedFieldScope)) {
                  recalculated = recalculateOption(newFieldValues, templateStructure, parentItemForCalc);
                } else if (isCalculatedFieldType(changedFieldType, changedFieldScope)) {
                  recalculated = recalculateOption(newFieldValues, templateStructure, parentItemForCalc, changedFieldType);
                } else {
                  recalculated = newFieldValues;
                }
                
                updatedOptionFieldValues = recalculated;
                
                // Sprawdź czy ta opcja jest zaznaczona (po aktualizacji)
                // Szukamy po fieldType 104 (Selected), nie po id - bo opcje używają childFields z innymi id
                const selectedFv = recalculated.find(
                  (fv: any) => fv.fieldType === 104
                );
                // Jeśli nie ma pola fieldType, sprawdź przez definicję w childFields
                if (!selectedFv && selectedFieldDef) {
                  // Znajdź childField odpowiadający Selected
                  const optionsField = (templateStructure.systemFields || []).find(
                    (f: any) => f.fieldTypeConfig?.isCollection && f.childFields?.length > 0
                  );
                  const selectedChildField = optionsField?.childFields?.find(
                    (cf: any) => (cf.fieldType ?? cf.fieldTypeConfig?.fieldType) === 104
                  );
                  if (selectedChildField) {
                    const selectedFvByChildId = recalculated.find(
                      (fv: any) => fv.fieldDefinitionId === selectedChildField.id
                    );
                    isThisOptionSelected = selectedFvByChildId?.boolValue === true;
                  }
                } else {
                  isThisOptionSelected = selectedFv?.boolValue === true;
                }
                
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
                  
                  // Autosave dla odznaczanej opcji (radio behavior)
                  const deselectedFieldValue = opt.fieldValues[selectedFieldIdx];
                  if (onFieldAutosave && !isTemporaryId(opt.id) && deselectedFieldValue?.id && !isTemporaryId(deselectedFieldValue.id) && fieldType !== undefined) {
                    onFieldAutosave({
                      entityType: 'item',
                      entityId: opt.id,
                      fieldValueId: deselectedFieldValue.id,
                      fieldDefinitionId: fieldId,
                      fieldType,
                      valueType: 'boolean', // fieldType 104 = Selected jest zawsze boolean
                      value: 'false',
                    });
                  }
                  
                  return { ...opt, fieldValues: newFieldValues };
                }
              }
              return opt;
            });
            
            // Kopiuj wartości pól kalkulowanych z wybranej opcji do właściciela
            // - gdy właśnie zaznaczamy opcję (isSelectingOption)
            // - LUB gdy zmieniamy wartość w opcji która jest już zaznaczona (isThisOptionSelected)
            if (isSelectingOption || isThisOptionSelected) {
              const updatedFieldValues = [...owner.fieldValues];
              
              const optionsField = (templateStructure.systemFields || []).find(
                (f: any) => f.fieldTypeConfig?.isCollection && f.childFields?.length > 0
              );
              const childFieldDefs = optionsField?.childFields || [];
              
              // Zbierz pola do autosave (owner = item lub component)
              const fieldsToAutosave: Array<{
                fieldValueId: string;
                fieldDefinitionId: string;
                fieldType: number;
                valueType: 'string' | 'numeric' | 'boolean' | 'date';
                value: string | undefined;
              }> = [];
              
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
                  const existingFv = updatedFieldValues[existingIdx];
                  updatedFieldValues[existingIdx] = {
                    ...existingFv,
                    stringValue: optFv?.stringValue,
                    decimalValue: optFv?.decimalValue,
                    boolValue: optFv?.boolValue,
                    dateTimeValue: optFv?.dateTimeValue,
                  };
                  
                  // Dodaj do autosave jeśli pole istnieje i owner jest zapisany
                  if (existingFv.id && !isTemporaryId(existingFv.id)) {
                    fieldsToAutosave.push({
                      fieldValueId: existingFv.id,
                      fieldDefinitionId: mainCalcField.id,
                      fieldType: childFieldType,
                      valueType: 'numeric', // Pola kalkulowane 200-206 są zawsze numeryczne
                      value: optFv?.decimalValue?.toString() ?? optFv?.stringValue,
                    });
                  }
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
              
              // Wywołaj autosave dla skopiowanych pól pozycji/komponentu
              if (onFieldAutosave && !isTemporaryId(owner.id)) {
                for (const field of fieldsToAutosave) {
                  onFieldAutosave({
                    entityType: 'item',
                    entityId: owner.id,
                    fieldValueId: field.fieldValueId,
                    fieldDefinitionId: field.fieldDefinitionId,
                    fieldType: field.fieldType,
                    valueType: field.valueType,
                    value: field.value,
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
      {/* Pasek narzędziowy: Rozwiń/Zwiń + filtry */}
      {(flatRows.length > 0 || hasActiveFilters) && (
        <Box px={4} py={2} borderBottomWidth="1px" borderBottomColor="gray.200">
          <HStack justify="space-between" align="center">
            {/* Lewa strona: Rozwiń / Zwiń */}
            <HStack spacing={1}>
              <Button
                size="xs"
                variant="ghost"
                leftIcon={<ChevronsDown size={14} />}
                onClick={expandAll}
              >
                Rozwiń
              </Button>
              <Button
                size="xs"
                variant="ghost"
                leftIcon={<ChevronsUp size={14} />}
                onClick={collapseAll}
              >
                Zwiń
              </Button>
            </HStack>

            {/* Prawa strona: info o filtrach */}
            {hasActiveFilters && (
              <HStack spacing={2}>
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
            )}
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
                  const sortableId = `group::${group.id}`;

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
                  const sortableId = `item::${row.groupId}::${item.id}`;

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
                      getItemFieldValueFull={getItemFieldValueFull}
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
