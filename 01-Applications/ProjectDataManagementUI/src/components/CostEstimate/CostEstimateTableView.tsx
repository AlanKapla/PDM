import React, { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import ReactDOM from 'react-dom';
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

// Minimalna szerokość kolumny "Pozycja" — dopasowuje się do zawartości (np. zagnieżdżone podgrupy)
const POSITION_COL_MIN_WIDTH = 260;

// ============================================================================
// SYSTEM OBLICZEŃ KOSZTORYSU
// ============================================================================

/**
 * Pola źródłowe (zawsze edytowalne, wpisywane ręcznie):
 *   101 - Quantity (ilość)
 *   200 - UnitPriceNet (cena jednostkowa netto)
 *   201 - VatRate (stawka VAT %)
 * 
 * Pola obliczane (readonly gdy WSZYSTKIE wymagane pola źródłowe są wypełnione):
 *   202 - UnitPriceGross = netto × (1 + VAT/100)     wymaga: netto + VAT
 *   203 - ValueNet = netto × ilość                     wymaga: netto + ilość
 *   204 - ValueGross = brutto_jedn × ilość             wymaga: netto + VAT + ilość
 *   205 - UnitVat = netto × (VAT/100)                  wymaga: netto + VAT
 *   206 - TotalVat = VAT_jedn × ilość                  wymaga: netto + VAT + ilość
 * 
 * ZASADY:
 * - Przeliczenie odpala się TYLKO przy edycji pola źródłowego (101, 200, 201)
 * - Edycja pola obliczanego NIE odpala przeliczenia
 * - Gdy brakuje danych źródłowych → pole obliczane jest edytowalne
 * - Gdy pojawiają się dane źródłowe → stare ręczne wartości są NADPISYWANE obliczonymi
 * - Gdy znikają dane źródłowe → obliczone wartości są USUWANE (pole staje się puste i edytowalne)
 */

const SOURCE_FIELD_TYPES = new Set([101, 200, 201]);
const CALCULATED_FIELD_TYPES = new Set([202, 203, 204, 205, 206]);

const round2 = (v: number): number => Math.round(v * 100) / 100;

/**
 * Wartości ŹRÓDŁOWE — do decyzji czy triggerować przeliczenie
 */
interface ItemCalcValues {
  quantity?: number;
  unitPriceNet?: number;
  vatRate?: number;
}

/**
 * WSZYSTKIE wartości pozycji — do sprawdzenia readonly i ścieżek alternatywnych
 */
interface AllItemValues extends ItemCalcValues {
  unitPriceGross?: number;  // 202
  valueNet?: number;        // 203
  valueGross?: number;      // 204
  unitVat?: number;         // 205
  totalVat?: number;        // 206
}

/**
 * Helper: czyta wartość liczbową z fieldValues pozycji
 */
const readFieldValue = (
  item: CostEstimateItemWeb,
  fieldType: number,
  fields: any[]
): number | undefined => {
  const def = fields.find((f: any) => (f.fieldType ?? f.fieldTypeConfig?.fieldType) === fieldType);
  if (!def) return undefined;
  const fv = item.fieldValues?.find((v) => v.fieldDefinitionId === def.id);
  if (!fv) return undefined;
  if (fv.decimalValue !== null && fv.decimalValue !== undefined) {
    return !isNaN(fv.decimalValue) ? fv.decimalValue : undefined;
  }
  if (fv.stringValue) {
    const p = parseFloat(fv.stringValue);
    return !isNaN(p) ? p : undefined;
  }
  return undefined;
};

/**
 * Pobiera TYLKO wartości pól źródłowych (101, 200, 201)
 */
const getSourceValues = (
  item: CostEstimateItemWeb,
  templateStructure: any
): ItemCalcValues => {
  const sys = templateStructure.systemFields || [];
  const calc = templateStructure.calculatedFields || [];
  return {
    quantity: readFieldValue(item, 101, sys),
    unitPriceNet: readFieldValue(item, 200, calc),
    vatRate: readFieldValue(item, 201, calc),
  };
};

/**
 * Pobiera WSZYSTKIE wartości pozycji (źródłowe + obliczane/ręczne)
 */
const getAllValues = (
  item: CostEstimateItemWeb,
  templateStructure: any
): AllItemValues => {
  const sys = templateStructure.systemFields || [];
  const calc = templateStructure.calculatedFields || [];
  return {
    quantity: readFieldValue(item, 101, sys),
    unitPriceNet: readFieldValue(item, 200, calc),
    vatRate: readFieldValue(item, 201, calc),
    unitPriceGross: readFieldValue(item, 202, calc),
    valueNet: readFieldValue(item, 203, calc),
    valueGross: readFieldValue(item, 204, calc),
    unitVat: readFieldValue(item, 205, calc),
    totalVat: readFieldValue(item, 206, calc),
  };
};

/**
 * Ścieżki obliczania — każde pole może być obliczone na kilka sposobów.
 * Pierwsza pasująca ścieżka jest używana.
 * 
 * Pole jest READONLY gdy jakakolwiek ścieżka ma wszystkie wymagane wartości.
 */
type ValueKey = keyof AllItemValues;

interface ComputePath {
  requires: ValueKey[];
  compute: (v: AllItemValues) => number;
}

const COMPUTE_PATHS: Record<number, ComputePath[]> = {
  // UnitPriceGross = netto × (1 + VAT/100)
  202: [
    { requires: ['unitPriceNet', 'vatRate'], compute: v => round2(v.unitPriceNet! * (1 + v.vatRate! / 100)) },
  ],
  // ValueNet = netto × ilość
  203: [
    { requires: ['unitPriceNet', 'quantity'], compute: v => round2(v.unitPriceNet! * v.quantity!) },
  ],
  // ValueGross — 4 ścieżki (ostatnia: brak VAT → brutto = netto)
  204: [
    { requires: ['unitPriceNet', 'vatRate', 'quantity'], compute: v => round2(v.unitPriceNet! * (1 + v.vatRate! / 100) * v.quantity!) },
    { requires: ['unitPriceGross', 'quantity'], compute: v => round2(v.unitPriceGross! * v.quantity!) },
    { requires: ['valueNet', 'totalVat'], compute: v => round2(v.valueNet! + v.totalVat!) },
    { requires: ['valueNet'], compute: v => round2(v.valueNet!) },
  ],
  // UnitVat = netto × (VAT/100)
  205: [
    { requires: ['unitPriceNet', 'vatRate'], compute: v => round2(v.unitPriceNet! * (v.vatRate! / 100)) },
  ],
  // TotalVat — 3 ścieżki
  206: [
    { requires: ['unitPriceNet', 'vatRate', 'quantity'], compute: v => round2(v.unitPriceNet! * v.quantity! * (v.vatRate! / 100)) },
    { requires: ['unitVat', 'quantity'], compute: v => round2(v.unitVat! * v.quantity!) },
    { requires: ['valueNet', 'vatRate'], compute: v => round2(v.valueNet! * (v.vatRate! / 100)) },
  ],
};

/**
 * Czy pole może być obliczone z dostępnych wartości?
 * Używane do sprawdzenia readonly — sprawdza WSZYSTKIE ścieżki (nie tylko źródłowe).
 */
const canComputeFromAvailable = (fieldType: number, vals: AllItemValues): boolean => {
  const paths = COMPUTE_PATHS[fieldType];
  if (!paths) return false;
  return paths.some(path => path.requires.every(key => vals[key] !== undefined));
};

/**
 * Oblicz wartość pola pierwszą dostępną ścieżką.
 * Używane przy przeliczeniu po edycji pola źródłowego.
 */
const computeFieldFromAvailable = (fieldType: number, vals: AllItemValues): number | undefined => {
  const paths = COMPUTE_PATHS[fieldType];
  if (!paths) return undefined;
  for (const path of paths) {
    if (path.requires.every(key => vals[key] !== undefined)) {
      return path.compute(vals);
    }
  }
  return undefined;
};

/**
 * Przelicza pozycję po zmianie pola.
 * Dla każdego pola obliczanego:
 * - jeśli MOŻNA obliczyć (z dowolnej ścieżki) → zapisz wartość
 * - jeśli NIE MOŻNA obliczyć → USUŃ wartość (pole staje się puste i edytowalne)
 * 
 * UWAGA: Nie nadpisuj pola, które właśnie zostało ręcznie zmienione.
 */
const recalculateItem = (
  item: CostEstimateItemWeb,
  templateStructure: any,
  skipFieldType?: number
): CostEstimateItemWeb => {
  const calculatedFields = templateStructure.calculatedFields || [];
  let fieldValues = [...(item.fieldValues || [])];

  // Obliczaj w odpowiedniej kolejności: najpierw pola bazowe (202, 205, 203),
  // potem pochodne (206, 204) — bo pochodne mogą zależeć od bazowych
  const calcOrder = [202, 205, 203, 206, 204];

  for (const calcFieldType of calcOrder) {
    // Nie nadpisuj pola, które właśnie użytkownik ręcznie edytował
    if (calcFieldType === skipFieldType) continue;

    const def = calculatedFields.find((f: any) =>
      (f.fieldType ?? f.fieldTypeConfig?.fieldType) === calcFieldType
    );
    if (!def) continue;

    // Pobierz aktualne wartości (z uwzględnieniem już obliczonych w tej iteracji)
    const currentItem: CostEstimateItemWeb = { ...item, fieldValues };
    const vals = getAllValues(currentItem, templateStructure);
    const computed = computeFieldFromAvailable(calcFieldType, vals);

    const idx = fieldValues.findIndex((fv) => fv.fieldDefinitionId === def.id);

    if (computed !== undefined) {
      if (idx !== -1) {
        fieldValues[idx] = {
          ...fieldValues[idx],
          decimalValue: computed,
          stringValue: computed.toString(),
        };
      } else {
        fieldValues.push({
          id: `calc_${Date.now()}_${def.id}`,
          fieldDefinitionId: def.id,
          fieldType: calcFieldType,
          fieldScope: FieldScope.ItemCalculated,
          fieldName: def.fieldName,
          fieldLabel: def.label,
          decimalValue: computed,
          stringValue: computed.toString(),
        });
      }
    }
    // Jeśli nie można obliczyć → NIE usuwaj istniejącej wartości
    // (mogła być wpisana ręcznie przez użytkownika)
  }

  return { ...item, fieldValues };
};

// ============================================================================
// OBLICZENIA DLA OPCJI/WARIANTÓW
// ============================================================================

/**
 * Pobiera childField definitions z pola collection (Options) w templateStructure.
 */
const getChildFieldDefs = (templateStructure: any): any[] => {
  const optionsField = (templateStructure.systemFields || []).find(
    (f: any) => f.fieldTypeConfig?.isCollection && f.childFields?.length > 0
  );
  return optionsField?.childFields || [];
};

/**
 * Helper: czyta wartość liczbową z fieldValues opcji po fieldType,
 * używając childField definitions.
 */
const readOptionFieldValue = (
  optionFieldValues: any[],
  fieldType: number,
  childFieldDefs: any[]
): number | undefined => {
  const def = childFieldDefs.find((f: any) => (f.fieldType ?? f.fieldTypeConfig?.fieldType) === fieldType);
  if (!def) return undefined;
  const fv = optionFieldValues.find((v: any) => v.fieldDefinitionId === def.id);
  if (!fv) return undefined;
  if (fv.decimalValue !== null && fv.decimalValue !== undefined) {
    return !isNaN(fv.decimalValue) ? fv.decimalValue : undefined;
  }
  if (fv.stringValue) {
    const p = parseFloat(fv.stringValue);
    return !isNaN(p) ? p : undefined;
  }
  return undefined;
};

/**
 * Pobiera WSZYSTKIE wartości opcji (źródłowe + obliczane/ręczne) z childField definitions.
 * Ilość (quantity, fieldType 101) jest brana z pozycji nadrzędnej — opcje nie mają własnego pola ilości.
 */
const getAllOptionValues = (
  optionFieldValues: any[],
  templateStructure: any,
  parentItem?: CostEstimateItemWeb
): AllItemValues => {
  const childFieldDefs = getChildFieldDefs(templateStructure);
  // Ilość pochodzi z pozycji nadrzędnej (systemField 101), nie z opcji
  const parentQuantity = parentItem
    ? readFieldValue(parentItem, 101, templateStructure.systemFields || [])
    : undefined;
  return {
    quantity: parentQuantity,
    unitPriceNet: readOptionFieldValue(optionFieldValues, 200, childFieldDefs),
    vatRate: readOptionFieldValue(optionFieldValues, 201, childFieldDefs),
    unitPriceGross: readOptionFieldValue(optionFieldValues, 202, childFieldDefs),
    valueNet: readOptionFieldValue(optionFieldValues, 203, childFieldDefs),
    valueGross: readOptionFieldValue(optionFieldValues, 204, childFieldDefs),
    unitVat: readOptionFieldValue(optionFieldValues, 205, childFieldDefs),
    totalVat: readOptionFieldValue(optionFieldValues, 206, childFieldDefs),
  };
};

/**
 * Przelicza opcję/wariant po zmianie pola — analogicznie do recalculateItem,
 * ale działa na childField definitions zamiast templateStructure.calculatedFields.
 */
const recalculateOption = (
  optionFieldValues: any[],
  templateStructure: any,
  parentItem?: CostEstimateItemWeb,
  skipFieldType?: number
): any[] => {
  const childFieldDefs = getChildFieldDefs(templateStructure);
  let fieldValues = [...optionFieldValues];

  const calcOrder = [202, 205, 203, 206, 204];

  for (const calcFieldType of calcOrder) {
    if (calcFieldType === skipFieldType) continue;

    const def = childFieldDefs.find((f: any) =>
      (f.fieldType ?? f.fieldTypeConfig?.fieldType) === calcFieldType
    );
    if (!def) continue;

    // Pobierz aktualne wartości z fieldValues (z uwzględnieniem już obliczonych)
    // Ilość (quantity) pobierana z pozycji nadrzędnej
    const vals = getAllOptionValues(fieldValues, templateStructure, parentItem);
    const computed = computeFieldFromAvailable(calcFieldType, vals);

    const idx = fieldValues.findIndex((fv: any) => fv.fieldDefinitionId === def.id);

    if (computed !== undefined) {
      if (idx !== -1) {
        fieldValues[idx] = {
          ...fieldValues[idx],
          decimalValue: computed,
          stringValue: computed.toString(),
        };
      } else {
        fieldValues.push({
          id: `calc_opt_${Date.now()}_${def.id}`,
          fieldDefinitionId: def.id,
          fieldType: calcFieldType,
          fieldScope: FieldScope.ItemCalculated,
          fieldName: def.fieldName,
          fieldLabel: def.label,
          decimalValue: computed,
          stringValue: computed.toString(),
        });
      }
    }
    // Jeśli nie można obliczyć → NIE usuwaj (mogło być wpisane ręcznie)
  }

  return fieldValues;
};

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

// ========== FORMATTED NUMERIC INPUT — Input z lokalnym stanem, min 2 miejsca po przecinku ==========
interface FormattedNumericInputProps {
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  disabled?: boolean;
}

// Formatuje wartość: min 2 miejsca po przecinku, trimuje zbędne zera powyżej 2
// 12 → "12,00", 12.3 → "12,30", 12.333 → "12,333", 12.33300 → "12,333"
function formatNumericDisplay(val: string): string {
  if (!val || val === '' || val === '-') return val;
  const dotVal = val.replace(',', '.');
  const num = parseFloat(dotVal);
  if (isNaN(num)) return val;
  const parts = dotVal.split('.');
  const decimals = parts[1]?.length || 0;
  if (decimals <= 2) {
    return num.toFixed(2).replace('.', ',');
  }
  let formatted = num.toFixed(decimals);
  // Trimuj trailing zeros, ale zostaw min 2
  while (formatted.endsWith('0') && formatted.split('.')[1].length > 2) {
    formatted = formatted.slice(0, -1);
  }
  return formatted.replace('.', ',');
}

const FormattedNumericInput: React.FC<FormattedNumericInputProps> = ({ value, onChange, disabled }) => {
  // Lokalny stan — przechowuje dokładnie to co user wpisuje (z przecinkiem, z trailing zeros)
  const [localValue, setLocalValue] = useState(() => {
    if (!value || value === '') return '';
    return formatNumericDisplay(value);
  });
  const [isFocused, setIsFocused] = useState(false);

  // Sync z parent TYLKO gdy input nie jest aktywny
  useEffect(() => {
    if (!isFocused) {
      if (!value || value === '') {
        setLocalValue('');
      } else {
        setLocalValue(formatNumericDisplay(value));
      }
    }
  }, [value, isFocused]);

  return (
    <Input
      type="text"
      inputMode="decimal"
      value={localValue}
      onChange={(e) => {
        const v = e.target.value;
        // Pozwól na puste, minus, albo poprawny format liczbowy z przecinkiem
        if (v === '' || v === '-' || /^-?\d*,?\d*$/.test(v)) {
          setLocalValue(v);
          // Wyślij do parenta z kropką (format wewnętrzny)
          const dotVal = v.replace(',', '.');
          onChange(dotVal || undefined);
        }
      }}
      onFocus={() => setIsFocused(true)}
      onBlur={() => {
        setIsFocused(false);
        // Sformatuj wartość: min 2 miejsca po przecinku
        if (localValue && localValue !== '' && localValue !== '-') {
          const formatted = formatNumericDisplay(localValue);
          setLocalValue(formatted);
          onChange(formatted.replace(',', '.') || undefined);
        }
      }}
      isDisabled={disabled}
      size="sm"
      textAlign="right"
      variant="outline"
      bg="white"
      borderColor="gray.300"
      _hover={{ borderColor: 'blue.400' }}
      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px var(--chakra-colors-blue-500)' }}
    />
  );
};

// ========== UNIT COMBOBOX — Input z podpowiedziami jednostek z szablonu ==========
interface UnitComboBoxProps {
  units: { id: string; code: string; name: string; symbol: string }[];
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  disabled?: boolean;
}

const UnitComboBox: React.FC<UnitComboBoxProps> = ({ units, value, onChange, disabled }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [inputValue, setInputValue] = useState(value || '');
  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});

  // Synchronizuj inputValue z zewnętrzną wartością
  useEffect(() => {
    setInputValue(value || '');
  }, [value]);

  // Filtruj jednostki po wpisanym tekście
  const filtered = units.filter(u => {
    const search = inputValue.toLowerCase();
    return u.code.toLowerCase().includes(search)
      || u.name.toLowerCase().includes(search)
      || (u.symbol && u.symbol.toLowerCase().includes(search));
  });

  // Oblicz pozycję dropdowna na ekranie (portal renderuje poza overflow:hidden)
  const updateDropdownPosition = useCallback(() => {
    if (inputRef.current) {
      const rect = inputRef.current.getBoundingClientRect();
      setDropdownStyle({
        position: 'fixed',
        top: rect.bottom + 2,
        left: rect.left,
        width: rect.width,
        zIndex: 9999,
      });
    }
  }, []);

  const openDropdown = useCallback(() => {
    updateDropdownPosition();
    setIsOpen(true);
  }, [updateDropdownPosition]);

  // Zamknij dropdown przy kliknięciu poza inputem i dropdownem
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as Node;
      if (
        inputRef.current && !inputRef.current.contains(target) &&
        dropdownRef.current && !dropdownRef.current.contains(target)
      ) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Zamknij dropdown przy scrollu (pozycja się zmieni)
  useEffect(() => {
    if (!isOpen) return;
    const handleScroll = () => setIsOpen(false);
    window.addEventListener('scroll', handleScroll, true);
    return () => window.removeEventListener('scroll', handleScroll, true);
  }, [isOpen]);

  const dropdown = isOpen && filtered.length > 0 ? ReactDOM.createPortal(
    <Box
      ref={dropdownRef}
      style={dropdownStyle}
      bg="white"
      border="1px solid"
      borderColor="gray.200"
      borderRadius="md"
      boxShadow="lg"
      maxH="180px"
      overflowY="auto"
    >
      {filtered.map((unit) => (
        <Box
          key={unit.id}
          px={3}
          py={1.5}
          fontSize="sm"
          cursor="pointer"
          _hover={{ bg: 'blue.50' }}
          bg={value === unit.code ? 'blue.100' : undefined}
          onClick={() => {
            onChange(unit.code);
            setInputValue(unit.code);
            setIsOpen(false);
          }}
        >
          <Text fontWeight="medium">{unit.code}</Text>
          {unit.name !== unit.code && (
            <Text fontSize="xs" color="gray.500">{unit.name}{unit.symbol ? ` (${unit.symbol})` : ''}</Text>
          )}
        </Box>
      ))}
    </Box>,
    document.body
  ) : null;

  return (
    <>
      <Input
        ref={inputRef}
        value={inputValue}
        onChange={(e) => {
          const v = e.target.value;
          setInputValue(v);
          onChange(v || undefined);
          openDropdown();
        }}
        onClick={openDropdown}
        onFocus={openDropdown}
        onKeyDown={(e) => {
          if (e.key === 'Escape') setIsOpen(false);
        }}
        isDisabled={disabled}
        size="sm"
        variant="outline"
        placeholder="Jednostka..."
        bg="white"
        borderColor="gray.300"
        _hover={{ borderColor: 'blue.400' }}
        _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px var(--chakra-colors-blue-500)' }}
      />
      {dropdown}
    </>
  );
};

interface FlatRow {
  type: 'group' | 'item';
  level: number;
  groupId?: string;
  group?: CostEstimateGroupWeb;
  groupNumber?: string; // Hierarchiczny numer grupy (np. "1", "1.1", "1.1.1")
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

  // Funkcje obsługi zmiany szerokości kolumn
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

  // Oblicz szerokość kolumny na podstawie długości tekstu nagłówka
  const calculateWidthFromLabel = useCallback((label: string): number => {
    // Średnia szerokość znaku dla fontu ~12-14px to ok. 9px
    // Dodajemy padding na margines, ikony sortowania/filtra, uchwyt resize
    const charWidth = 9;
    const basePadding = 50; // na padding, ikony itp.
    const minWidth = 80;
    const maxWidth = 300;
    
    const calculatedWidth = label.length * charWidth + basePadding;
    return Math.min(Math.max(calculatedWidth, minWidth), maxWidth);
  }, []);

  // Pobierz szerokość kolumny - priorytet: state użytkownika > obliczona z etykiety
  const getColumnWidth = useCallback((fieldId: string, defaultWidth?: string, label?: string): number => {
    // 1. Jeśli użytkownik zmienił szerokość ręcznie (drag resize)
    if (columnWidths[fieldId]) {
      return columnWidths[fieldId];
    }
    // 2. Zawsze oblicz na podstawie etykiety jeśli dostępna
    if (label) {
      return calculateWidthFromLabel(label);
    }
    // 3. Fallback do defaultWidth z konfiguracji
    if (defaultWidth) {
      const parsed = parseInt(defaultWidth.replace('px', ''), 10);
      if (!isNaN(parsed)) return parsed;
    }
    return 150; // domyślna szerokość
  }, [columnWidths, calculateWidthFromLabel]);

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
          const childLabel = childCfg?.namePl || childField.label || childField.fieldName || 'Pole';
          result.push({
            type: 'childField',
            originalColumn: col,
            childField,
            parentFieldDef: fieldDef,
            label: childLabel,
            fieldId: `${col.fieldId}_${childField.fieldName}`,
            width: undefined, // brak domyślnej - będzie obliczone z label
            isSortable: childField.isSortable ?? false,
            isFilterable: childField.isFilterable ?? false,
            isBoolean: childCfg?.isBoolean ?? false,
            isNumeric: childCfg?.isNumeric ?? false,
          });
        }
      } else {
        // Zwykła kolumna
        const fieldCfg = fieldDef?.fieldTypeConfig;
        const label = col.fieldLabel || fieldDef?.label || fieldDef?.fieldTypeConfig?.namePl || col.fieldName || 'Kolumna';
        result.push({
          type: 'regular',
          originalColumn: col,
          fieldDef,
          label,
          fieldId: col.fieldId,
          width: undefined, // szerokość obliczana automatycznie z label
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
      // Dla childField szukaj w opcjach - ale to nie jest używane do filtrowania pozycji
      // Filtrowanie wariantów jest osobne
      const optionFieldValue = item.options?.find(opt => 
        opt.fieldValues.some(fv => fv.fieldDefinitionId === col.childField.id)
      )?.fieldValues.find(fv => fv.fieldDefinitionId === col.childField.id);
      return getFieldValueAsString(optionFieldValue);
    }
    
    // Dla zwykłej kolumny
    const fieldDef = col.fieldDef;
    if (!fieldDef) return undefined;
    
    const fieldValue = item.fieldValues.find(fv => fv.fieldDefinitionId === fieldDef.id);
    return getFieldValueAsString(fieldValue);
  };

  // Funkcja pomocnicza do filtrowania wariantów
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
        
        // Specjalna obsługa dla pól boolean
        if (col.isBoolean) {
          if (filterValue === 'true') return value === 'true';
          if (filterValue === 'false') return value === 'false' || value === undefined || value === null;
          return true;
        }
        
        if (value === undefined || value === null) return false;
        
        const strValue = String(value).toLowerCase();
        return strValue.includes(filterValue.toLowerCase());
      });
    });
  }, [expandedColumns]);

  // Filtruj i sortuj pozycje w grupie
  const filterAndSortItems = useCallback((items: CostEstimateItemWeb[]): CostEstimateItemWeb[] => {
    let result = [...items];
    
    // Rozdziel filtry na te dotyczące pozycji i te dotyczące wariantów (childField)
    const activeFilters = Object.entries(filters);
    const itemFilters = activeFilters.filter(([fieldId]) => {
      const col = expandedColumns.find(c => c.fieldId === fieldId);
      return col && col.type !== 'childField';
    });
    const optionFilters = activeFilters.filter(([fieldId]) => {
      const col = expandedColumns.find(c => c.fieldId === fieldId);
      return col && col.type === 'childField';
    });
    
    // Filtrowanie pozycji (tylko po polach nie-childField)
    if (itemFilters.length > 0) {
      result = result.filter(item => {
        return itemFilters.every(([fieldId, filterValue]) => {
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
    
    // Filtrowanie wariantów wewnątrz pozycji (dla filtrów childField)
    if (optionFilters.length > 0) {
      result = result.map(item => {
        if (!item.options || item.options.length === 0) return item;
        
        const filteredOptions = filterOptions(item.options, optionFilters);
        return { ...item, options: filteredOptions };
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

    const processGroup = (group: CostEstimateGroupWeb, level: number, parentNumber: string, indexInParent: number) => {
      // Oblicz hierarchiczny numer grupy
      const groupNumber = parentNumber ? `${parentNumber}.${indexInParent + 1}` : `${indexInParent + 1}`;
      
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
        groupNumber,
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
        (group.childGroups || []).forEach((child, childIndex) => {
          processGroup(child, level + 1, groupNumber, childIndex);
        });
      }
    };

    (details.rootGroups || []).forEach((group, index) => processGroup(group, 0, '', index));
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

  // Rozwiń grupę (usuń z collapsedGroups jeśli jest zwinięta)
  const expandGroup = (groupId: string) => {
    setCollapsedGroups((prev) => {
      const next = new Set(prev);
      next.delete(groupId);
      return next;
    });
  };

  // Handler dodawania grupy z automatycznym rozwinięciem
  const handleAddGroupWithExpand = () => {
    if (onAddGroup) {
      const newGroupId = onAddGroup();
      if (newGroupId) {
        expandGroup(newGroupId);
      }
    }
  };

  // Handler dodawania podgrupy z automatycznym rozwinięciem rodzica i nowej podgrupy
  const handleAddSubGroupWithExpand = (parentGroupId: string) => {
    if (onAddSubGroup) {
      // Rozwiń grupę rodzica
      expandGroup(parentGroupId);
      const newSubGroupId = onAddSubGroup(parentGroupId);
      if (newSubGroupId) {
        expandGroup(newSubGroupId);
      }
    }
  };

  // ========== DRAG AND DROP ==========
  
  // Handler rozpoczęcia przeciągania
  const handleDragStart = (event: DragStartEvent) => {
    setActiveId(event.active.id as string);
  };

  // Handler zakończenia przeciągania
  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveId(null);

    if (!over || active.id === over.id || !onDataChange) {
      return;
    }

    const activeIdStr = active.id as string;
    const overIdStr = over.id as string;

    // Sprawdź czy to przeciąganie grupy, pozycji czy opcji
    const isGroupDrag = activeIdStr.startsWith('group-');
    const isItemDrag = activeIdStr.startsWith('item-');
    const isOptionDrag = activeIdStr.startsWith('option-');

    if (isGroupDrag && overIdStr.startsWith('group-')) {
      // Przeciąganie grupy
      const activeGroupId = activeIdStr.replace('group-', '');
      const overGroupId = overIdStr.replace('group-', '');
      
      handleReorderGroups(activeGroupId, overGroupId);
    } else if (isOptionDrag && overIdStr.startsWith('option-')) {
      // Przeciąganie opcji
      // Format: option-{groupId}-{itemId}-{optionId}
      const activeParts = activeIdStr.replace('option-', '').split('-');
      const overParts = overIdStr.replace('option-', '').split('-');
      
      if (activeParts.length >= 3 && overParts.length >= 3) {
        const activeGroupId = activeParts[0];
        const activeItemId = activeParts[1];
        const activeOptionId = activeParts.slice(2).join('-');
        
        const overGroupId = overParts[0];
        const overItemId = overParts[1];
        const overOptionId = overParts.slice(2).join('-');
        
        // Opcje można przeciągać tylko w ramach tej samej pozycji
        if (activeGroupId === overGroupId && activeItemId === overItemId) {
          handleReorderOptions(activeGroupId, activeItemId, activeOptionId, overOptionId);
        }
      }
    } else if (isItemDrag) {
      // Przeciąganie pozycji
      // Format: item-{groupId}-{itemId}
      const activeParts = activeIdStr.replace('item-', '').split('-');
      
      if (activeParts.length >= 2) {
        const activeGroupId = activeParts[0];
        const activeItemId = activeParts.slice(1).join('-');
        
        // Cel może być pozycją lub grupą
        if (overIdStr.startsWith('item-')) {
          const overParts = overIdStr.replace('item-', '').split('-');
          if (overParts.length >= 2) {
            const overGroupId = overParts[0];
            const overItemId = overParts.slice(1).join('-');
            
            if (activeGroupId === overGroupId) {
              // Przeciąganie w tej samej grupie
              handleReorderItems(activeGroupId, activeItemId, overItemId);
            } else {
              // Przenoszenie pozycji do innej grupy
              handleMoveItemToGroup(activeGroupId, activeItemId, overGroupId, overItemId);
            }
          }
        } else if (overIdStr.startsWith('group-')) {
          // Upuszczenie pozycji na grupę - dodaj na koniec tej grupy
          const overGroupId = overIdStr.replace('group-', '');
          if (activeGroupId !== overGroupId) {
            handleMoveItemToGroup(activeGroupId, activeItemId, overGroupId, null);
          }
        }
      }
    }
  };

  // Zmiana kolejności opcji w ramach pozycji
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
              return {
                ...group,
                items: newItems,
              };
            }
          }
        }
        return {
          ...group,
          childGroups: reorderOptionsInGroups(group.childGroups || []),
        };
      });
    };

    const updatedDetails = {
      ...details,
      rootGroups: reorderOptionsInGroups(details.rootGroups),
    };

    onDataChange(updatedDetails);
  };

  // Przenoszenie pozycji do innej grupy
  const handleMoveItemToGroup = (
    sourceGroupId: string, 
    itemId: string, 
    targetGroupId: string, 
    targetItemId: string | null
  ) => {
    if (!onDataChange) return;

    let movedItem: CostEstimateItemWeb | null = null;

    // Krok 1: Usuń pozycję ze źródłowej grupy i zapisz ją
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

    // Krok 2: Dodaj pozycję do docelowej grupy
    const addItemToTarget = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map(group => {
        if (group.id === targetGroupId && movedItem) {
          const items = group.items || [];
          const updatedItem = { ...movedItem, groupId: targetGroupId };
          
          if (targetItemId) {
            // Wstaw przed/po konkretnej pozycji
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
          
          // Dodaj na koniec
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

    const updatedDetails = {
      ...details,
      rootGroups: afterAdd,
    };

    onDataChange(updatedDetails);
  };

  // Zmiana kolejności grup - obsługuje zarówno reorder jak i przenoszenie między rodzicami
  const handleReorderGroups = (activeGroupId: string, overGroupId: string) => {
    if (!onDataChange) return;

    // Znajdź grupy i ich rodzica
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

    // Sprawdź czy przenoszona grupa nie jest rodzicem docelowej (zapobiegaj cyklom)
    const isDescendant = (parentGroup: CostEstimateGroupWeb, childId: string): boolean => {
      if (parentGroup.id === childId) return true;
      return (parentGroup.childGroups || []).some(child => isDescendant(child, childId));
    };
    if (isDescendant(activeInfo.group, overGroupId)) return;

    // Sprawdź czy obie grupy są na tym samym poziomie (mają tego samego rodzica)
    const sameParent = activeInfo.parent?.id === overInfo.parent?.id;
    
    if (sameParent) {
      // Reorder w ramach tego samego rodzica
      const siblings = activeInfo.siblings;
      const activeIndex = siblings.findIndex(g => g.id === activeGroupId);
      const overIndex = siblings.findIndex(g => g.id === overGroupId);

      if (activeIndex === -1 || overIndex === -1) return;

      const reorderedSiblings = arrayMove(siblings, activeIndex, overIndex);
      
      // Zaktualizuj order dla wszystkich grup
      const updatedSiblings = reorderedSiblings.map((g, idx) => ({ ...g, order: idx }));

      // Zaktualizuj strukturę
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

      const updatedDetails = {
        ...details,
        rootGroups: activeInfo.parent === null 
          ? updatedSiblings.map(g => ({ ...g, childGroups: g.childGroups || [] }))
          : updateGroupsInTree(details.rootGroups, undefined),
      };

      onDataChange(updatedDetails);
    } else {
      // Przenoszenie grupy do innego rodzica
      handleMoveGroupToNewParent(activeGroupId, overGroupId, activeInfo, overInfo);
    }
  };

  // Przenoszenie grupy do nowego rodzica (obok docelowej grupy)
  const handleMoveGroupToNewParent = (
    activeGroupId: string,
    overGroupId: string,
    activeInfo: { group: CostEstimateGroupWeb; parent: CostEstimateGroupWeb | null; siblings: CostEstimateGroupWeb[] },
    overInfo: { group: CostEstimateGroupWeb; parent: CostEstimateGroupWeb | null; siblings: CostEstimateGroupWeb[] }
  ) => {
    if (!onDataChange) return;

    const movedGroup = { ...activeInfo.group };
    const newParentId = overInfo.parent?.id || null;

    // Krok 1: Usuń grupę ze źródłowego miejsca
    const removeGroupFromSource = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups
        .filter(g => g.id !== activeGroupId)
        .map((g, idx) => ({
          ...g,
          order: idx,
          childGroups: removeGroupFromSource(g.childGroups || []),
        }));
    };

    // Krok 2: Dodaj grupę do nowego rodzica obok docelowej grupy
    const addGroupToTarget = (groups: CostEstimateGroupWeb[], parentId: string | null): CostEstimateGroupWeb[] => {
      // Jeśli to poziom docelowy (ten sam rodzic co overGroup)
      if (parentId === newParentId) {
        const targetIndex = groups.findIndex(g => g.id === overGroupId);
        if (targetIndex !== -1) {
          const newGroups = [...groups];
          const updatedMovedGroup = { ...movedGroup, parentGroupId: newParentId || undefined };
          newGroups.splice(targetIndex, 0, updatedMovedGroup);
          return newGroups.map((g, idx) => ({ ...g, order: idx }));
        }
        // Dodaj na koniec jeśli nie znaleziono
        return [...groups, { ...movedGroup, parentGroupId: newParentId || undefined, order: groups.length }];
      }
      
      return groups.map(g => ({
        ...g,
        childGroups: addGroupToTarget(g.childGroups || [], g.id),
      }));
    };

    const afterRemove = removeGroupFromSource(details.rootGroups);
    const afterAdd = addGroupToTarget(afterRemove, null);

    const updatedDetails = {
      ...details,
      rootGroups: afterAdd,
    };

    onDataChange(updatedDetails);
  };

  // Zmiana kolejności pozycji w grupie
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
          const updatedItems = reorderedItems.map((item, idx) => ({ ...item, order: idx }));

          return {
            ...group,
            items: updatedItems,
          };
        }
        return {
          ...group,
          childGroups: updateGroupItems(group.childGroups || []),
        };
      });
    };

    const updatedDetails = {
      ...details,
      rootGroups: updateGroupItems(details.rootGroups),
    };

    onDataChange(updatedDetails);
  };

  // Pobierz ID dla sortowania
  const getSortableIds = useMemo(() => {
    const ids: string[] = [];
    
    flatRows.forEach(row => {
      if (row.type === 'group' && row.group) {
        ids.push(`group-${row.group.id}`);
      } else if (row.type === 'item' && row.item && row.groupId) {
        ids.push(`item-${row.groupId}-${row.item.id}`);
        // Dodaj ID komponentów i ich opcji
        const itemComponents = row.item.components || [];
        itemComponents.forEach((comp: CostEstimateItemWeb) => {
          ids.push(`comp-${row.groupId}-${row.item!.id}-${comp.id}`);
          const compOptions = comp.options || [];
          compOptions.forEach((option: any) => {
            ids.push(`comp-option-${row.groupId}-${comp.id}-${option.id}`);
          });
        });
        // Dodaj ID opcji dla pozycji
        const itemOptions = row.item.options || [];
        itemOptions.forEach((option: any) => {
          ids.push(`option-${row.groupId}-${row.item!.id}-${option.id}`);
        });
      }
    });
    
    return ids;
  }, [flatRows]);

  // Pobierz wartość pola grupy
  const getGroupFieldValue = (group: CostEstimateGroupWeb, fieldId: string): string | undefined => {
    const fieldValue = group.fieldValues.find((fv) => fv.fieldDefinitionId === fieldId);
    return getFieldValueAsString(fieldValue);
  };

  // Pobierz wartość pola pozycji
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
    
    // Ustal wartości typowane
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

  // Aktualizuj wartość pola grupy
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

        return {
          ...group,
          fieldValues: newFieldValues,
        };
      }

      return {
        ...group,
        childGroups: (group.childGroups || []).map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };

    onDataChange(updatedDetails);
  };

  // Aktualizuj wartość pola pozycji (z automatycznymi obliczeniami)
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

            let newFieldValues = [...item.fieldValues];

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

            // Stwórz tymczasową pozycję z zaktualizowanymi wartościami
            let updatedItem: CostEstimateItemWeb = {
              ...item,
              fieldValues: newFieldValues,
            };

            // Przelicz pochodne pola.
            // Przy edycji pola źródłowego → przelicz wszystko.
            // Przy edycji pola obliczanego → przelicz pochodne, ale NIE nadpisuj pola które użytkownik właśnie edytował.
            const changedFieldType = def?.fieldType ?? def?.fieldTypeConfig?.fieldType;
            if (SOURCE_FIELD_TYPES.has(changedFieldType)) {
              updatedItem = recalculateItem(updatedItem, templateStructure);
            } else if (CALCULATED_FIELD_TYPES.has(changedFieldType)) {
              updatedItem = recalculateItem(updatedItem, templateStructure, changedFieldType);
            }

            // Gdy zmieniono ilość (101) na pozycji → przelicz też opcje/warianty,
            // bo ich wartości (valueNet, valueGross, totalVat) zależą od quantity z pozycji
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

        return {
          ...group,
          items,
        };
      }

      return {
        ...group,
        childGroups: (group.childGroups || []).map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };

    onDataChange(updatedDetails);
  };

  // Renderuj input dla pola
  // itemAllValues - wszystkie wartości pozycji (źródłowe + obliczane/ręczne) do sprawdzenia readonly
  const renderFieldInput = (
    fieldDef: any,
    value: string | undefined,
    onChange: (value: string | undefined) => void,
    disabled: boolean = false,
    itemAllValues?: AllItemValues
  ) => {
     // Prefer new FieldTypeConfig flags when available
     const cfg = fieldDef.fieldTypeConfig as
       | { isNumeric: boolean; isText: boolean; isDate: boolean; isBoolean: boolean; isCollection: boolean; valueTypeName?: string }
       | undefined;

     // Sprawdź czy pole jest obliczane i czy da się je obliczyć z DOSTĘPNYCH wartości
     // (źródłowych LUB ręcznie wpisanych obliczanych — np. unitVat + quantity → totalVat)
     const calcFieldType = fieldDef?.fieldType ?? fieldDef?.fieldTypeConfig?.fieldType;
     const isCalcField = CALCULATED_FIELD_TYPES.has(calcFieldType);
     const shouldBeReadonly = isCalcField && itemAllValues != null && canComputeFromAvailable(calcFieldType, itemAllValues);

     // Pola z isCollection są obsługiwane przez expandedColumns jako osobne kolumny childFields
     // więc tutaj pomijamy (nie powinno się zdarzyć, ale dla pewności)
     if (cfg?.isCollection) {
       return <Text fontSize="xs" color="gray.400">—</Text>;
     }

     // Jeśli pole jest obliczane I wszystkie pola źródłowe wypełnione, wyświetl jako readonly z szarym tłem
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

     // Specjalna obsługa pola jednostki — Select z listą z szablonu + opcja wpisania własnej wartości
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
                 _checked: {
                   bg: 'blue.500',
                   borderColor: 'blue.500',
                 },
                 _hover: {
                   borderColor: 'blue.400',
                 },
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
                 _checked: {
                   bg: 'blue.500',
                   borderColor: 'blue.500',
                 },
                 _hover: {
                   borderColor: 'blue.400',
                 },
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

  // Funkcja do dodawania opcji dla pozycji LUB komponentu
  // itemId może wskazywać na pozycję lub komponent (szukamy w obu)
  const addOptionToItem = (groupId: string, itemId: string) => {
    if (!onDataChange) return;

    const addOptionTo = (target: CostEstimateItemWeb): CostEstimateItemWeb => {
      const newOption: CostEstimateItemWeb = {
        id: `temp_opt_${Date.now()}`,
        groupId: groupId,
        parentItemId: target.id,
        relationType: 1, // Option
        order: (target.options || []).length,
        fieldValues: [],
        options: undefined,
        createdAt: new Date().toISOString(),
        updatedAt: undefined,
      };
      return {
        ...target,
        options: [...(target.options || []), newOption],
      };
    };

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            return addOptionTo(item);
          }
          // Szukaj w komponentach pozycji
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
      return {
        ...group,
        childGroups: (group.childGroups || []).map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };
    onDataChange(updatedDetails);
  };

  // Funkcja do usuwania opcji z pozycji LUB komponentu
  const removeOptionFromItem = (groupId: string, itemId: string, optionId: string) => {
    if (!onDataChange) return;

    const removeOption = (target: CostEstimateItemWeb): CostEstimateItemWeb => ({
      ...target,
      options: (target.options || []).filter(opt => opt.id !== optionId),
    });

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            return removeOption(item);
          }
          // Szukaj w komponentach pozycji
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
      return {
        ...group,
        childGroups: (group.childGroups || []).map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };
    onDataChange(updatedDetails);
  };

  // ========== ZARZĄDZANIE KOMPONENTAMI ==========

  /**
   * Sumuje wartości pól kalkulowanych z komponentów i wstawia do pozycji nadrzędnej.
   * Pola o fieldType 203 (valueNet), 204 (valueGross), 206 (totalVat) są sumowane.
   * Pola źródłowe (200, 201) i jednostkowe (202, 205) NIE są sumowane.
   */
  const sumComponentValuesToItem = (item: CostEstimateItemWeb): CostEstimateItemWeb => {
    const components = item.components || [];
    if (components.length === 0) return item;

    const calcFields = templateStructure.calculatedFields || [];
    // Pola do sumowania: valueNet (203), valueGross (204), totalVat (206)
    const summableFieldTypes = new Set([203, 204, 206]);
    let updatedFieldValues = [...item.fieldValues];

    for (const calcField of calcFields) {
      const ft = calcField.fieldType ?? calcField.fieldTypeConfig?.fieldType;
      if (!summableFieldTypes.has(ft)) continue;

      // Sumuj wartość tego pola ze wszystkich komponentów
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

  // Dodaj komponent do pozycji
  const addComponentToItem = (groupId: string, itemId: string) => {
    if (!onDataChange) return;

    // Id-ki pól kalkulowanych z szablonu — do wyczyszczenia przy dodaniu pierwszego komponentu
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
              relationType: 2, // Component
              order: existingComponents.length,
              fieldValues: [],
              options: undefined,
              components: undefined,
              createdAt: new Date().toISOString(),
              updatedAt: undefined,
            };

            // Przy dodaniu pierwszego komponentu — wyzeruj pola kalkulowane w pozycji
            // (odtąd wartości będą sumowane z komponentów)
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
      return {
        ...group,
        childGroups: (group.childGroups || []).map(updateGroup),
      };
    };

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };
    onDataChange(updatedDetails);
  };

  // Usuń komponent z pozycji
  const removeComponentFromItem = (groupId: string, itemId: string, componentId: string) => {
    if (!onDataChange) return;

    const updateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      if (group.id === groupId) {
        const items = (group.items || []).map((item) => {
          if (item.id === itemId) {
            const updatedComponents = (item.components || []).filter(c => c.id !== componentId);
            let updatedItem: CostEstimateItemWeb = { ...item, components: updatedComponents };
            // Przelicz sumy po usunięciu komponentu
            if (updatedComponents.length > 0) {
              updatedItem = sumComponentValuesToItem(updatedItem);
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

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };
    onDataChange(updatedDetails);
  };

  // Aktualizuj pole komponentu (analogicznie do updateItemFieldValue)
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

                // Przelicz komponent jak zwykłą pozycję
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

            // Po zmianie komponentu → przelicz sumy w pozycji nadrzędnej
            let updatedItem: CostEstimateItemWeb = { ...item, components: updatedComponents };
            updatedItem = sumComponentValuesToItem(updatedItem);
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

    const updatedDetails = {
      ...details,
      rootGroups: details.rootGroups.map(updateGroup),
    };
    onDataChange(updatedDetails);
  };

  // Funkcja do kopiowania wartości z opcji do pozycji nadrzędnej
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

    // Znajdź definicję pola - szukamy zarówno w głównych polach jak i w childFields
    let def: any = null;
    let fieldType: number | undefined = undefined;
    
    // Szukaj w głównych polach
    const sysDef = templateStructure.systemFields?.find((f: SystemFieldWeb) => f.id === fieldId);
    const calcDef = templateStructure.calculatedFields?.find((f: CalculatedFieldWeb) => f.id === fieldId);
    const genDef = templateStructure.genericFields?.find((f: GenericFieldWeb) => f.id === fieldId);
    def = sysDef || calcDef || genDef;
    
    // Jeśli nie znaleziono, szukaj w childFields (dla pól Options)
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
    
    // Sprawdź czy to pole Selected (fieldType 104) i czy jest zaznaczane na true
    const isSelectingOption = fieldType === 104 && value === 'true';

    // Helper: aktualizuje opcje w danym "właścicielu" (pozycji lub komponencie)
    const updateOwnerOptions = (owner: CostEstimateItemWeb, parentItemForCalc: CostEstimateItemWeb): CostEstimateItemWeb => {
            // Zaktualizuj opcję i zbierz nowe fieldValues
            let updatedOptionFieldValues: any[] = [];
            
            const options = (owner.options || []).map((opt) => {
              if (opt.id === optionId) {
                // To jest zaznaczana opcja
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

                // Zachowaj zaktualizowane fieldValues dla późniejszego kopiowania
                updatedOptionFieldValues = newFieldValues;
                
                // Przelicz pola obliczane opcji (analogicznie do recalculateItem)
                const changedFieldType = def?.fieldType ?? def?.fieldTypeConfig?.fieldType;
                let recalculated: any[];
                if (SOURCE_FIELD_TYPES.has(changedFieldType)) {
                  // Edycja pola źródłowego → przelicz wszystkie obliczane
                  recalculated = recalculateOption(newFieldValues, templateStructure, parentItemForCalc);
                } else if (CALCULATED_FIELD_TYPES.has(changedFieldType)) {
                  // Edycja pola obliczanego → przelicz pochodne, ale nie nadpisuj edytowanego
                  recalculated = recalculateOption(newFieldValues, templateStructure, parentItemForCalc, changedFieldType);
                } else {
                  recalculated = newFieldValues;
                }
                
                updatedOptionFieldValues = recalculated;
                return { ...opt, fieldValues: recalculated };
              } else if (isSelectingOption) {
                // Jeśli zaznaczamy jedną opcję, odznacz pozostałe (radio behavior)
                // Znajdź pole Selected w tej opcji i ustaw na false
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
            
            // Jeśli zaznaczamy opcję jako Selected, kopiuj wartości pól kalkulowanych do właściciela po fieldType
            if (isSelectingOption) {
              const updatedFieldValues = [...owner.fieldValues];
              
              // Znajdź pole Options i jego childFields
              const optionsField = (templateStructure.systemFields || []).find(
                (f: any) => f.fieldTypeConfig?.isCollection && f.childFields?.length > 0
              );
              const childFieldDefs = optionsField?.childFields || [];
              
              // Iteruj po definicjach childFields (nie po wartościach opcji)
              for (const childFieldDef of childFieldDefs) {
                const childFieldType = childFieldDef.fieldType ?? childFieldDef.fieldTypeConfig?.fieldType;
                
                // Kopiuj tylko pola kalkulowane (fieldType 200-206)
                if (childFieldType === undefined || childFieldType < 200 || childFieldType > 206) continue;
                
                // Znajdź główne pole kalkulowane o tym samym fieldType
                const mainCalcField = (templateStructure.calculatedFields || []).find(
                  (cf: any) => (cf.fieldType ?? cf.fieldTypeConfig?.fieldType) === childFieldType
                );
                
                if (!mainCalcField) continue;
                
                // Znajdź wartość tego pola w wariancie (może być undefined jeśli puste)
                const optFv = updatedOptionFieldValues.find(
                  (fv: any) => fv.fieldDefinitionId === childFieldDef.id
                );
                
                // Znajdź lub utwórz wartość w pozycji dla głównego pola kalkulowanego
                const existingIdx = updatedFieldValues.findIndex(
                  (fv) => fv.fieldDefinitionId === mainCalcField.id
                );
                
                if (existingIdx !== -1) {
                  // Aktualizuj istniejącą wartość (kopiuj z wariantu lub wyczyść)
                  updatedFieldValues[existingIdx] = {
                    ...updatedFieldValues[existingIdx],
                    stringValue: optFv?.stringValue,
                    decimalValue: optFv?.decimalValue,
                    boolValue: optFv?.boolValue,
                    dateTimeValue: optFv?.dateTimeValue,
                  };
                } else if (optFv) {
                  // Utwórz nową wartość w pozycji tylko jeśli wariant ma wartość
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
          // Szukaj w komponentach pozycji
          const comp = (item.components || []).find(c => c.id === itemId);
          if (comp) {
            const updatedComponents = (item.components || []).map(c =>
              c.id === itemId ? updateOwnerOptions(c, item) : c
            );
            // Po kopiowaniu wartości z opcji do komponentu → przelicz sumy w pozycji nadrzędnej
            let updatedItem: CostEstimateItemWeb = { ...item, components: updatedComponents };
            updatedItem = sumComponentValuesToItem(updatedItem);
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

          {/* Kolumna pozycji - zamrożona, auto-skalowanie */}
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

          {/* Kolumny według rozszerzonej konfiguracji (childFields jako osobne kolumny) */}
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
          // Suwak poziomy zawsze widoczny na dole widoku (nie na dole tabeli)
          position: 'relative',
          maxHeight: maxTableHeight,
          overflowY: 'auto',
          '&::-webkit-scrollbar': {
            height: '12px',
            width: '10px',
          },
          '&::-webkit-scrollbar-track': {
            background: 'gray.100',
            borderRadius: '6px',
          },
          '&::-webkit-scrollbar-thumb': {
            background: 'gray.400',
            borderRadius: '6px',
            '&:hover': {
              background: 'gray.500',
            },
          },
          scrollbarWidth: 'auto',
          scrollbarColor: '#A0AEC0 #EDF2F7',
        }}
      >
        {flatRows.length === 0 && !hasActiveFilters ? (
          // Brak grup - wyświetl komunikat i przycisk dodawania (tylko gdy nie ma filtrów)
          <Box p={8} textAlign="center">
            <Text fontSize="lg" fontWeight="medium" color="gray.600" mb={4}>
              Brak grup w kosztorysie
            </Text>
            <Text fontSize="sm" color="gray.500" mb={6}>
              Rozpocznij tworzenie kosztorysu dodając pierwszą grupę
            </Text>
            {editable && onAddGroup && (
              <Tooltip label="Dodaj grupę">
                <IconButton
                  aria-label="Dodaj grupę"
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
              {/* Colgroup dla precyzyjnej kontroli szerokości */}
              <colgroup>
                {editable && <col style={{ width: '120px' }} />}
                <col style={{ width: `${POSITION_COL_MIN_WIDTH}px` }} />
                {expandedColumns.map((col) => (
                  <col key={col.fieldId} style={{ width: `${getColumnWidth(col.fieldId, col.width, col.label)}px` }} />
                ))}
              </colgroup>
              {renderTableHeader()}
              <Tbody>
              {flatRows.map((row, idx) => {
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
                      onAddItem={onAddItem}
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
                      
                      // Znajdź definicję pola kalkulowanego
                      const calcField = templateStructure.calculatedFields?.find(
                        (f: any) => f.id === col.fieldId || f.fieldName === col.originalColumn?.fieldName
                      );
                      const fieldDef = calcField || col.fieldDef;
                      
                      if (fieldDef) {
                        // Sprawdź sumInTotal bezpośrednio na definicji pola LUB w totalSummaryFields
                        const hasSumInTotalFlag = fieldDef.sumInTotal === true;
                        const isInSummaryFields = totalSummaryFields.length > 0 && 
                          totalSummaryFields.some((sf: any) => sf.fieldId === col.fieldId || sf.fieldId === fieldDef.id);
                        
                        const shouldSum = hasSumInTotalFlag || isInSummaryFields;

                        if (shouldSum) {
                          // Użyj wartości z details (już obliczonych przez recalculateCostEstimate)
                          // Mapuj fieldName LUB fieldType na odpowiednią wartość z details
                          const fieldName = fieldDef.fieldName;
                          const fieldType = fieldDef.fieldType ?? fieldDef.fieldTypeConfig?.fieldType;
                          let sumValue: number | undefined;
                          
                          // Sprawdź standardowe pola - po fieldName LUB fieldType
                          if (fieldName === 'valueNet' || fieldType === 203) {
                            sumValue = details.totalNet;
                          } else if (fieldName === 'valueGross' || fieldType === 204) {
                            sumValue = details.totalGross;
                          } else if (fieldName === 'totalVat' || fieldType === 206) {
                            sumValue = details.totalVat;
                          } else {
                            // Sprawdź w summaryValues (dla pól z totalSummaryFields / sumInTotal)
                            sumValue = (details as any).summaryValues?.[fieldDef.id];
                          }
                          
                          // Fallback: jeśli brak prekalkulowanej sumy, oblicz z pozycji na żywo
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
                          <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">
                            —
                          </Text>
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
      
      {/* Przycisk dodawania grupy — widoczny zawsze w trybie edycji */}
      {editable && onAddGroup && flatRows.length > 0 && (
        <Box px={4} py={3} borderTopWidth="1px" borderTopColor="gray.200">
          <Button
            leftIcon={<FolderPlus size={16} />}
            colorScheme="green"
            variant="outline"
            size="sm"
            onClick={handleAddGroupWithExpand}
          >
            Dodaj grupę
          </Button>
        </Box>
      )}
    </Box>
  );
};

// ========== SORTABLE COMPONENTS ==========

interface SortableGroupRowProps {
  id: string;
  group: CostEstimateGroupWeb;
  level: number;
  indent: number;
  groupNumber: string;
  isCollapsed: boolean;
  editable: boolean;
  templateStructure: any;
  showGroupSummary: boolean;
  groupSummaryFields: any[];
  currencySymbol: string;
  expandedColumns: any[];
  getColumnWidth: (fieldId: string, defaultWidth?: string, label?: string) => number;
  getGroupFieldValue: (group: CostEstimateGroupWeb, fieldId: string) => string | undefined;
  updateGroupFieldValue: (groupId: string, fieldId: string, value: string | undefined) => void;
  renderFieldInput: (fieldDef: any, value: string | undefined, onChange: (value: string | undefined) => void, disabled?: boolean, itemAllValues?: AllItemValues) => React.ReactNode;
  formatDisplayValue: (value: string | undefined, fieldDef?: any) => string;
  toggleGroupCollapse: (groupId: string) => void;
  onAddItem?: (groupId: string) => void;
  onAddSubGroup?: (parentGroupId: string) => void;
  onDeleteGroup?: (groupId: string) => void;
}

const SortableGroupRow: React.FC<SortableGroupRowProps> = ({
  id,
  group,
  level,
  indent,
  groupNumber,
  isCollapsed,
  editable,
  templateStructure,
  showGroupSummary,
  groupSummaryFields,
  currencySymbol,
  expandedColumns,
  getColumnWidth,
  getGroupFieldValue,
  updateGroupFieldValue,
  renderFieldInput,
  formatDisplayValue,
  toggleGroupCollapse,
  onAddItem,
  onAddSubGroup,
  onDeleteGroup,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <Tr
      ref={setNodeRef}
      style={style}
      bgGradient={level === 0 ? 'linear(to-r, blue.50, blue.100)' : 'linear(to-r, teal.50, teal.100)'}
      borderTopWidth={level === 0 ? '3px' : '2px'}
      borderTopColor={level === 0 ? 'blue.400' : 'teal.300'}
    >
      {/* Akcje grupy - zamrożona kolumna */}
      {editable && (
        <Td
          px={3}
          py={2}
          textAlign="center"
          position="sticky"
          left={0}
          zIndex={5}
          bg={level === 0 ? 'blue.50' : 'teal.50'}
          minW="120px"
          maxW="120px"
        >
          <HStack spacing={1} justify="center">
            {/* Uchwyt do przeciągania */}
            <Tooltip label="Przeciągnij aby zmienić kolejność">
              <IconButton
                aria-label="Przeciągnij"
                icon={<GripVertical size={14} />}
                size="xs"
                variant="ghost"
                cursor="grab"
                {...attributes}
                {...listeners}
              />
            </Tooltip>
            {onAddItem && (
              <Tooltip label="Dodaj pozycję">
                <IconButton
                  aria-label="Dodaj pozycję"
                  icon={<ListPlus size={14} />}
                  size="xs"
                  colorScheme="green"
                  variant="ghost"
                  onClick={() => onAddItem(group.id)}
                />
              </Tooltip>
            )}
            {onAddSubGroup && (templateStructure?.canBranchGroups !== false) && (
              <Tooltip label="Dodaj podgrupę">
                <IconButton
                  aria-label="Dodaj podgrupę"
                  icon={<FolderPlus size={14} />}
                  size="xs"
                  colorScheme="blue"
                  variant="ghost"
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
                  variant="ghost"
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
        left={editable ? '120px' : 0}
        zIndex={5}
        bg={level === 0 ? 'blue.50' : 'teal.50'}
        w={`${POSITION_COL_MIN_WIDTH}px`}
        minW={`${POSITION_COL_MIN_WIDTH}px`}
        whiteSpace="nowrap"
      >
        <HStack spacing={2}>
          <Tooltip label={isCollapsed ? 'Rozwiń grupę' : 'Zwiń grupę'}>
            <IconButton
              aria-label={isCollapsed ? 'Rozwiń' : 'Zwiń'}
              icon={isCollapsed ? <ChevronRight size={16} /> : <ChevronDown size={16} />}
              size="xs"
              variant="ghost"
              onClick={() => toggleGroupCollapse(group.id)}
            />
          </Tooltip>
          <Badge colorScheme={level === 0 ? 'blue' : 'teal'} px={3} py={1}>
            Grupa {groupNumber}
          </Badge>
        </HStack>
      </Td>

      {/* Kolumny pól grup */}
      {expandedColumns.map((col: any) => {
        const colWidth = getColumnWidth(col.fieldId, col.width, col.label);
        
        if (col.type === 'childField') {
          return (
            <Td key={col.fieldId} p={2} bg={level === 0 ? 'blue.50' : 'teal.50'} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
              <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">
                —
              </Text>
            </Td>
          );
        }

        const groupHeaderField = templateStructure.groupHeaderFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
        
        if (groupHeaderField) {
          const value = getGroupFieldValue(group, groupHeaderField.id);
          return (
            <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
              {editable ? (
                renderFieldInput(groupHeaderField, value, (newValue) =>
                  updateGroupFieldValue(group.id, groupHeaderField.id, newValue)
                )
              ) : (
                <Text fontSize="sm" fontWeight="medium">{formatDisplayValue(value, groupHeaderField)}</Text>
              )}
            </Td>
          );
        }
        
        // Sprawdź czy pole ma ustawione sumowanie w grupie
        const systemField = templateStructure.systemFields?.find((f: any) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName);
        const calcField = templateStructure.calculatedFields?.find((f: any) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName);
        const genericField = templateStructure.genericFields?.find((f: any) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName);
        const fieldDef = systemField || calcField || genericField;
        
        if (fieldDef) {
          // Sprawdź sumInGroup bezpośrednio na definicji pola LUB w groupSummaryFields LUB showGroupSummary + domyślne pola
          const hasSumInGroupFlag = fieldDef.sumInGroup === true;
          const isInSummaryFields = groupSummaryFields.length > 0 && 
            groupSummaryFields.some((sf: any) => sf.fieldId === col.fieldId || sf.fieldId === fieldDef.id);
          const isDefaultSumField = showGroupSummary && 
            (fieldDef.fieldName === 'valueNet' || fieldDef.fieldName === 'valueGross' || fieldDef.fieldName === 'totalVat');
          
          const shouldSum = hasSumInGroupFlag || isInSummaryFields || isDefaultSumField;

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
            } else {
              // Oblicz sumę z pozycji grupy (włącznie z podgrupami)
              const collectGroupItems = (g: CostEstimateGroupWeb): CostEstimateItemWeb[] => {
                let items: CostEstimateItemWeb[] = [];
                if (g.items) {
                  items = items.concat(g.items);
                }
                if (g.childGroups) {
                  for (const childGroup of g.childGroups) {
                    items = items.concat(collectGroupItems(childGroup));
                  }
                }
                return items;
              };
              
              const groupItems = collectGroupItems(group);
              sumValue = 0;
              
              for (const item of groupItems) {
                const fieldValue = item.fieldValues?.find(
                  (fv: any) => fv.fieldDefinitionId === fieldDef.id
                );
                if (fieldValue?.decimalValue !== undefined && fieldValue?.decimalValue !== null) {
                  sumValue += fieldValue.decimalValue;
                } else if (fieldValue?.stringValue) {
                  const parsed = parseFloat(fieldValue.stringValue);
                  if (!isNaN(parsed)) {
                    sumValue += parsed;
                  }
                }
              }
            }

            return (
              <Td key={col.fieldId} p={2} textAlign="center" bg={level === 0 ? 'blue.50' : 'teal.50'} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
                <Text fontSize="sm" fontWeight="bold" color={level === 0 ? 'blue.700' : 'teal.700'}>
                  {sumValue !== undefined ? `Σ ${sumValue.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currencySymbol}` : '—'}
                </Text>
              </Td>
            );
          }
        }
        
        return (
          <Td key={col.fieldId} p={2} bg={level === 0 ? 'blue.50' : 'teal.50'} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
            <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">
              —
            </Text>
          </Td>
        );
      })}
    </Tr>
  );
};

// Komponent wiersza opcji z obsługą przeciągania
interface SortableOptionRowProps {
  id: string;
  option: any;
  optIndex: number;
  item: CostEstimateItemWeb;
  groupId: string;
  indent: number;
  editable: boolean;
  templateStructure: any;
  expandedColumns: any[];
  getColumnWidth: (fieldId: string, defaultWidth?: string, label?: string) => number;
  updateOptionFieldValue: (groupId: string, itemId: string, optionId: string, fieldId: string, fieldSource: 'system' | 'calculated' | 'generic', value: string | undefined) => void;
  removeOptionFromItem: (groupId: string, itemId: string, optionId: string) => void;
  renderFieldInput: (fieldDef: any, value: string | undefined, onChange: (value: string | undefined) => void, disabled?: boolean, itemAllValues?: AllItemValues) => React.ReactNode;
  formatDisplayValue: (value: string | undefined, fieldDef?: any) => string;
}

const SortableOptionRow: React.FC<SortableOptionRowProps> = ({
  id,
  option,
  optIndex,
  item,
  groupId,
  indent,
  editable,
  templateStructure,
  expandedColumns,
  getColumnWidth,
  updateOptionFieldValue,
  removeOptionFromItem,
  renderFieldInput,
  formatDisplayValue,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <Tr ref={setNodeRef} style={style} bg="purple.50" _hover={{ bg: 'purple.100' }}>
      {editable && (
        <Td
          px={3}
          py={2}
          textAlign="center"
          position="sticky"
          left={0}
          zIndex={5}
          bg="purple.50"
          minW="120px"
          maxW="120px"
        >
          <HStack spacing={1} justify="center">
            {/* Uchwyt do przeciągania */}
            <Tooltip label="Przeciągnij aby zmienić kolejność">
              <IconButton
                aria-label="Przeciągnij"
                icon={<GripVertical size={14} />}
                size="xs"
                variant="ghost"
                cursor="grab"
                {...attributes}
                {...listeners}
              />
            </Tooltip>
            <Tooltip label="Usuń opcję">
              <IconButton
                aria-label="Usuń opcję"
                icon={<Trash2 size={14} />}
                size="xs"
                colorScheme="red"
                variant="ghost"
                onClick={() => removeOptionFromItem(groupId, item.id, option.id)}
              />
            </Tooltip>
          </HStack>
        </Td>
      )}

      <Td
        p={2}
        pl={`${indent + 48}px`}
        position="sticky"
        left={editable ? '120px' : 0}
        zIndex={5}
        bg="purple.50"
        w={`${POSITION_COL_MIN_WIDTH}px`}
        minW={`${POSITION_COL_MIN_WIDTH}px`}
        whiteSpace="nowrap"
      >
        <Badge colorScheme="purple" size="sm">
          Opcja {optIndex + 1}
        </Badge>
      </Td>

      {expandedColumns.map((col: any) => {
        const colWidth = getColumnWidth(col.fieldId, col.width, col.label);
        
        // Dla opcji renderujemy tylko kolumny childField, reszta to puste komórki
        if (col.type !== 'childField' || !col.childField) {
          return (
            <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
              <Text fontSize="xs" color="gray.300" textAlign="center">—</Text>
            </Td>
          );
        }

        const optionFieldValue = option.fieldValues?.find(
          (fv: any) => fv.fieldDefinitionId === col.childField.id
        );
        const childValue = getFieldValueAsString(optionFieldValue) ?? '';
        const childCfg = col.childField.fieldTypeConfig;

        let fieldSource: 'system' | 'calculated' | 'generic' = 'system';
        if (templateStructure.calculatedFields?.find((f: any) => f.id === col.childField.id)) {
          fieldSource = 'calculated';
        } else if (templateStructure.genericFields?.find((f: any) => f.id === col.childField.id)) {
          fieldSource = 'generic';
        }

        // Oblicz wartości opcji do sprawdzenia readonly pól kalkulowanych
        // Ilość (quantity) pochodzi z pozycji nadrzędnej (item)
        const optionAllValues = getAllOptionValues(option.fieldValues || [], templateStructure, item);

        return (
          <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
            {editable ? (
              renderFieldInput(
                col.childField,
                childValue || undefined,
                (newValue) => updateOptionFieldValue(
                  groupId,
                  item.id,
                  option.id,
                  col.childField.id,
                  fieldSource,
                  newValue
                ),
                false,
                optionAllValues
              )
            ) : (
              <Text fontSize="sm" textAlign="center" isTruncated>
                {formatDisplayValue(childValue, col.childField)}
              </Text>
            )}
          </Td>
        );
      })}
    </Tr>
  );
};

// Komponent wiersza komponentu (sub-pozycja pozycji)
interface SortableComponentRowProps {
  id: string;
  component: CostEstimateItemWeb;
  compIndex: number;
  parentItem: CostEstimateItemWeb;
  groupId: string;
  indent: number;
  editable: boolean;
  templateStructure: any;
  expandedColumns: any[];
  getColumnWidth: (fieldId: string, defaultWidth?: string, label?: string) => number;
  getItemFieldValue: (item: CostEstimateItemWeb, fieldId: string) => string | undefined;
  updateComponentFieldValue: (groupId: string, itemId: string, componentId: string, fieldId: string, fieldSource: 'system' | 'calculated' | 'generic', value: string | undefined) => void;
  removeComponentFromItem: (groupId: string, itemId: string, componentId: string) => void;
  updateOptionFieldValue: (groupId: string, itemId: string, optionId: string, fieldId: string, fieldSource: 'system' | 'calculated' | 'generic', value: string | undefined) => void;
  removeOptionFromItem: (groupId: string, itemId: string, optionId: string) => void;
  renderFieldInput: (fieldDef: any, value: string | undefined, onChange: (value: string | undefined) => void, disabled?: boolean, itemAllValues?: AllItemValues) => React.ReactNode;
  formatDisplayValue: (value: string | undefined, fieldDef?: any) => string;
  onAddOption?: (groupId: string, itemId: string) => void;
}

const SortableComponentRow: React.FC<SortableComponentRowProps> = ({
  id,
  component,
  compIndex,
  parentItem,
  groupId,
  indent,
  editable,
  templateStructure,
  expandedColumns,
  getColumnWidth,
  getItemFieldValue,
  updateComponentFieldValue,
  removeComponentFromItem,
  updateOptionFieldValue,
  removeOptionFromItem,
  renderFieldInput,
  formatDisplayValue,
  onAddOption,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  // Komponent ma te same pola co pozycja — renderuj ALL fields (system, calculated, generic)
  const componentAllValues = getAllValues(component, templateStructure);
  const componentOptions = component.options || [];

  return (
    <React.Fragment>
    <Tr ref={setNodeRef} style={style} bg="green.50" _hover={{ bg: 'green.100' }}>
      {/* Akcje komponentu */}
      {editable && (
        <Td
          px={3}
          py={2}
          textAlign="center"
          position="sticky"
          left={0}
          zIndex={5}
          bg="green.50"
          minW="120px"
          maxW="120px"
        >
          <HStack spacing={1} justify="center">
            <Tooltip label="Przeciągnij aby zmienić kolejność">
              <IconButton
                aria-label="Przeciągnij"
                icon={<GripVertical size={14} />}
                size="xs"
                variant="ghost"
                cursor="grab"
                {...attributes}
                {...listeners}
              />
            </Tooltip>
            <Tooltip label="Usuń komponent">
              <IconButton
                aria-label="Usuń komponent"
                icon={<Trash2 size={14} />}
                size="xs"
                colorScheme="red"
                variant="ghost"
                onClick={() => removeComponentFromItem(groupId, parentItem.id, component.id)}
              />
            </Tooltip>
            {onAddOption && (
              <Tooltip label="Dodaj opcję/wariant">
                <IconButton
                  aria-label="Dodaj opcję"
                  icon={<GitBranch size={14} />}
                  size="xs"
                  colorScheme="purple"
                  variant="ghost"
                  onClick={() => onAddOption(groupId, component.id)}
                />
              </Tooltip>
            )}
          </HStack>
        </Td>
      )}

      {/* Etykieta komponentu */}
      <Td
        p={2}
        pl={`${indent + 48}px`}
        position="sticky"
        left={editable ? '120px' : 0}
        zIndex={5}
        bg="green.50"
        w={`${POSITION_COL_MIN_WIDTH}px`}
        minW={`${POSITION_COL_MIN_WIDTH}px`}
        whiteSpace="nowrap"
      >
        <Badge colorScheme="green" size="sm">
          Komponent {compIndex + 1}
        </Badge>
      </Td>

      {/* Kolumny pól komponentu — renderujemy pola pozycji (nie childField) */}
      {expandedColumns.map((col: any) => {
        const colWidth = getColumnWidth(col.fieldId, col.width, col.label);

        // Pola nagłówka grupy — puste
        const groupHeaderField = templateStructure.groupHeaderFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
        if (groupHeaderField) {
          return (
            <Td key={col.fieldId} p={2} bg="green.50" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
              <Text fontSize="xs" color="gray.300" textAlign="center">—</Text>
            </Td>
          );
        }

        // Pola opcji (childField) — pokaż liczbę opcji komponentu, jeśli istnieją
        if (col.type === 'childField') {
          return (
            <Td key={col.fieldId} p={2} bg="purple.50" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
              <Text fontSize="xs" color="purple.400" fontStyle="italic" textAlign="center">
                {componentOptions.length > 0 ? `${componentOptions.length} opcji` : '—'}
              </Text>
            </Td>
          );
        }

        // Rozpoznaj definicję pola i źródło
        let fieldDef: any = col.fieldDef;
        let fieldSource: 'system' | 'calculated' | 'generic' = 'generic';

        if (!fieldDef) {
          fieldDef = templateStructure.systemFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
          if (fieldDef) {
            fieldSource = 'system';
          } else {
            fieldDef = templateStructure.calculatedFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
            if (fieldDef) {
              fieldSource = 'calculated';
            } else {
              fieldDef = templateStructure.genericFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
              if (fieldDef) {
                fieldSource = 'generic';
              }
            }
          }
        } else {
          if (templateStructure.systemFields?.find((f: any) => f.id === fieldDef.id)) {
            fieldSource = 'system';
          } else if (templateStructure.calculatedFields?.find((f: any) => f.id === fieldDef.id)) {
            fieldSource = 'calculated';
          }
        }

        if (fieldDef) {
          const value = getItemFieldValue(component, fieldDef.id);
          return (
            <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
              {editable ? (
                renderFieldInput(
                  fieldDef,
                  value,
                  (newValue) => updateComponentFieldValue(
                    groupId,
                    parentItem.id,
                    component.id,
                    fieldDef.id,
                    fieldSource,
                    newValue
                  ),
                  false,
                  componentAllValues
                )
              ) : (
                <Text fontSize="sm" textAlign="center" isTruncated>
                  {formatDisplayValue(value, fieldDef)}
                </Text>
              )}
            </Td>
          );
        }

        return <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">-</Td>;
      })}
    </Tr>

    {/* Wiersze opcji komponentu */}
    {componentOptions.map((option: any, optIndex: number) => (
      <SortableOptionRow
        key={`comp-option-${component.id}-${option.id}`}
        id={`comp-option-${groupId}-${component.id}-${option.id}`}
        option={option}
        optIndex={optIndex}
        item={component}
        groupId={groupId}
        indent={indent + 24}
        editable={editable}
        templateStructure={templateStructure}
        expandedColumns={expandedColumns}
        getColumnWidth={getColumnWidth}
        updateOptionFieldValue={updateOptionFieldValue}
        removeOptionFromItem={removeOptionFromItem}
        renderFieldInput={renderFieldInput}
        formatDisplayValue={formatDisplayValue}
      />
    ))}
    </React.Fragment>
  );
};

interface SortableItemRowProps {
  id: string;
  item: CostEstimateItemWeb;
  groupId: string;
  level: number;
  indent: number;
  itemNumber: number;
  editable: boolean;
  templateStructure: any;
  expandedColumns: any[];
  getColumnWidth: (fieldId: string, defaultWidth?: string, label?: string) => number;
  getItemFieldValue: (item: CostEstimateItemWeb, fieldId: string) => string | undefined;
  updateItemFieldValue: (groupId: string, itemId: string, fieldId: string, fieldSource: 'system' | 'calculated' | 'generic', value: string | undefined) => void;
  updateOptionFieldValue: (groupId: string, itemId: string, optionId: string, fieldId: string, fieldSource: 'system' | 'calculated' | 'generic', value: string | undefined) => void;
  updateComponentFieldValue: (groupId: string, itemId: string, componentId: string, fieldId: string, fieldSource: 'system' | 'calculated' | 'generic', value: string | undefined) => void;
  removeOptionFromItem: (groupId: string, itemId: string, optionId: string) => void;
  removeComponentFromItem: (groupId: string, itemId: string, componentId: string) => void;
  renderFieldInput: (fieldDef: any, value: string | undefined, onChange: (value: string | undefined) => void, disabled?: boolean, itemAllValues?: AllItemValues) => React.ReactNode;
  formatDisplayValue: (value: string | undefined, fieldDef?: any) => string;
  onDeleteItem?: (groupId: string, itemId: string) => void;
  onAddOption?: (groupId: string, itemId: string) => void;
  onAddComponent?: (groupId: string, itemId: string) => void;
}

const SortableItemRow: React.FC<SortableItemRowProps> = ({
  id,
  item,
  groupId,
  level,
  indent,
  itemNumber,
  editable,
  templateStructure,
  expandedColumns,
  getColumnWidth,
  getItemFieldValue,
  updateItemFieldValue,
  updateOptionFieldValue,
  updateComponentFieldValue,
  removeOptionFromItem,
  removeComponentFromItem,
  renderFieldInput,
  formatDisplayValue,
  onDeleteItem,
  onAddOption,
  onAddComponent,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  const itemOptions = item.options || [];
  const itemComponents = item.components || [];
  const hasComponents = itemComponents.length > 0;

  // Stan zwijania komponentów — domyślnie rozwinięte
  const [componentsExpanded, setComponentsExpanded] = useState(true);

  return (
    <React.Fragment>
      {/* Główny wiersz pozycji */}
      <Tr ref={setNodeRef} style={style} bg="gray.50" _hover={{ bg: 'gray.100' }}>
        {/* Akcje pozycji - zamrożona kolumna */}
        {editable && (
          <Td
            px={3}
            py={2}
            textAlign="center"
            position="sticky"
            left={0}
            zIndex={5}
            bg="gray.50"
            minW="120px"
            maxW="120px"
            _groupHover={{ bg: 'gray.100' }}
          >
            <HStack spacing={1} justify="center">
              {/* Uchwyt do przeciągania */}
              <Tooltip label="Przeciągnij aby zmienić kolejność">
                <IconButton
                  aria-label="Przeciągnij"
                  icon={<GripVertical size={14} />}
                  size="xs"
                  variant="ghost"
                  cursor="grab"
                  {...attributes}
                  {...listeners}
                />
              </Tooltip>
              {onDeleteItem && (
                <Tooltip label="Usuń pozycję">
                  <IconButton
                    aria-label="Usuń pozycję"
                    icon={<Trash2 size={14} />}
                    size="xs"
                    colorScheme="red"
                    variant="ghost"
                    onClick={() => onDeleteItem(groupId, item.id)}
                  />
                </Tooltip>
              )}
              {onAddOption && !hasComponents && (
                <Tooltip label="Dodaj opcję/wariant">
                  <IconButton
                    aria-label="Dodaj opcję"
                    icon={<GitBranch size={14} />}
                    size="xs"
                    colorScheme="purple"
                    variant="ghost"
                    onClick={() => onAddOption(groupId, item.id)}
                  />
                </Tooltip>
              )}
              {onAddComponent && (
                <Tooltip label="Dodaj komponent (składnik pozycji)">
                  <IconButton
                    aria-label="Dodaj komponent"
                    icon={<Layers size={14} />}
                    size="xs"
                    colorScheme="green"
                    variant="ghost"
                    onClick={() => onAddComponent(groupId, item.id)}
                  />
                </Tooltip>
              )}
            </HStack>
          </Td>
        )}

        {/* Pozycja - zamrożona kolumna */}
        <Td
          p={3}
          pl={`${indent + 24}px`}
          position="sticky"
          left={editable ? '120px' : 0}
          zIndex={5}
          bg="gray.50"
          w={`${POSITION_COL_MIN_WIDTH}px`}
          minW={`${POSITION_COL_MIN_WIDTH}px`}
          whiteSpace="nowrap"
          _groupHover={{ bg: 'gray.100' }}
        >
          <HStack spacing={1}>
            {hasComponents && (
              <Tooltip label={componentsExpanded ? 'Zwiń komponenty' : 'Rozwiń komponenty'}>
                <IconButton
                  aria-label={componentsExpanded ? 'Zwiń komponenty' : 'Rozwiń komponenty'}
                  icon={componentsExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                  size="xs"
                  variant="ghost"
                  onClick={() => setComponentsExpanded(prev => !prev)}
                  minW="auto"
                  h="auto"
                  p={0}
                />
              </Tooltip>
            )}
            <Text fontSize="sm" color="gray.600" fontWeight="medium">
              POZYCJA {itemNumber}
            </Text>
          </HStack>
        </Td>

        {/* Kolumny pól pozycji */}
        {expandedColumns.map((col: any) => {
          const colWidth = getColumnWidth(col.fieldId, col.width, col.label);
          
          const groupHeaderField = templateStructure.groupHeaderFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
          if (groupHeaderField) {
            return (
              <Td key={col.fieldId} p={2} bg="gray.50" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
                <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">
                  —
                </Text>
              </Td>
            );
          }

          if (col.type === 'childField') {
            return (
              <Td key={col.fieldId} p={2} bg="purple.50" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
                <Text fontSize="xs" color="purple.400" fontStyle="italic" textAlign="center">
                  {itemOptions.length > 0 ? `${itemOptions.length} opcji` : '—'}
                </Text>
              </Td>
            );
          }

          let fieldDef: any = col.fieldDef;
          let fieldSource: 'system' | 'calculated' | 'generic' = 'generic';

          if (!fieldDef) {
            fieldDef = templateStructure.systemFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
            if (fieldDef) {
              fieldSource = 'system';
            } else {
              fieldDef = templateStructure.calculatedFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
              if (fieldDef) {
                fieldSource = 'calculated';
              } else {
                fieldDef = templateStructure.genericFields?.find((f: any) => f.fieldName === col.originalColumn.fieldName);
                if (fieldDef) {
                  fieldSource = 'generic';
                }
              }
            }
          } else {
            if (templateStructure.systemFields?.find((f: any) => f.id === fieldDef.id)) {
              fieldSource = 'system';
            } else if (templateStructure.calculatedFields?.find((f: any) => f.id === fieldDef.id)) {
              fieldSource = 'calculated';
            }
          }

          if (fieldDef) {
            const value = getItemFieldValue(item, fieldDef.id);
            const isNumericField = fieldDef.fieldTypeConfig?.isNumeric || fieldDef.fieldType === 0 || fieldDef.fieldType === 1;
            // Pobierz TYLKO wartości pól źródłowych (do sprawdzenia readonly)
            const itemAllValues = getAllValues(item, templateStructure);
            // Gdy pozycja ma komponenty — blokuj TYLKO pola kalkulowane (sumy z komponentów)
            const isCalcFieldForDisable = fieldSource === 'calculated';
            const disabledByComponents = hasComponents && isCalcFieldForDisable;
            return (
              <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden"
                bg={hasComponents && isCalcFieldForDisable ? 'green.50' : undefined}
              >
                {editable ? (
                  renderFieldInput(fieldDef, value, (newValue) =>
                    updateItemFieldValue(groupId, item.id, fieldDef.id, fieldSource, newValue),
                    disabledByComponents, // zablokowane tylko kalkulowane gdy są komponenty
                    itemAllValues // wszystkie wartości do sprawdzenia readonly
                  )
                ) : (
                  <Text fontSize="sm" textAlign="center" isTruncated>
                    {formatDisplayValue(value, fieldDef)}
                  </Text>
                )}
              </Td>
            );
          }

          return <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">-</Td>;
        })}
      </Tr>

      {/* Wiersze komponentów (składniki pozycji) — ukryte gdy pozycja zwinięta */}
      {componentsExpanded && itemComponents.map((comp: CostEstimateItemWeb, compIndex: number) => (
        <SortableComponentRow
          key={`comp-${item.id}-${comp.id}`}
          id={`comp-${groupId}-${item.id}-${comp.id}`}
          component={comp}
          compIndex={compIndex}
          parentItem={item}
          groupId={groupId}
          indent={indent}
          editable={editable}
          templateStructure={templateStructure}
          expandedColumns={expandedColumns}
          getColumnWidth={getColumnWidth}
          getItemFieldValue={getItemFieldValue}
          updateComponentFieldValue={updateComponentFieldValue}
          removeComponentFromItem={removeComponentFromItem}
          updateOptionFieldValue={updateOptionFieldValue}
          removeOptionFromItem={removeOptionFromItem}
          onAddOption={onAddOption}
          renderFieldInput={renderFieldInput}
          formatDisplayValue={formatDisplayValue}
        />
      ))}

      {/* Wiersze opcji */}
      {itemOptions.map((option: any, optIndex: number) => (
        <SortableOptionRow
          key={`option-${item.id}-${option.id}`}
          id={`option-${groupId}-${item.id}-${option.id}`}
          option={option}
          optIndex={optIndex}
          item={item}
          groupId={groupId}
          indent={indent}
          editable={editable}
          templateStructure={templateStructure}
          expandedColumns={expandedColumns}
          getColumnWidth={getColumnWidth}
          updateOptionFieldValue={updateOptionFieldValue}
          removeOptionFromItem={removeOptionFromItem}
          renderFieldInput={renderFieldInput}
          formatDisplayValue={formatDisplayValue}
        />
      ))}
    </React.Fragment>
  );
};