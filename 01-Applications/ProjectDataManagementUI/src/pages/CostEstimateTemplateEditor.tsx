import React, { useState, useEffect, useRef, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Button,
  VStack,
  HStack,
  Input,
  FormControl,
  FormLabel,
  FormHelperText,
  Select,
  Checkbox,
  IconButton,
  Box,
  Text,
  Textarea,
  NumberInput,
  NumberInputField,
  useToast,
  Badge,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  Divider,
  Tooltip,
  Collapse,
  useDisclosure,
  Icon,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  Heading,
  Alert,
  AlertIcon,
  Card,
  CardBody,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionPanel,
  AccordionIcon,
  Switch,
  useBreakpointValue,
  Skeleton,
  SkeletonText,
} from "@chakra-ui/react";
import {
  Plus,
  Trash2,
  ChevronUp,
  ChevronDown,
  Settings,
  List,
  BookOpen,
  Calculator,
  Tag,
  EyeOff,
  AlertCircle,
  HelpCircle,
  FileText,
  Layers,
  GripVertical,
  Layout,
  Check,
  History,
  Eye,
  ArrowLeft,
  Save,
  CheckCircle,
} from "lucide-react";
import type {
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  SystemFieldDefinition,
  CostEstimateTemplateStructure,
  CostEstimateGroupDefinition,
  CostEstimateWorkScopeFieldsDefinition,
  CostEstimateSummaryConfiguration,
  CostEstimateUiConfiguration,
  GroupHeaderFieldDefinition,
  CrossFieldValidationRule,
  SummaryFieldWeb,
  ColumnConfigurationWeb,
  CostEstimateTemplateStructureWeb,
} from "../types/costEstimate.types";
import {
  FieldType,
  FieldScope,
  SummaryScope,
  CalculatedFieldType,
  GenericFieldType,
  GroupHeaderFieldType,
  SystemFieldType,
} from "../types/costEstimate.types";
import { fieldTypeLabels, fieldScopeLabels, isSummableField, getFieldScope, convertFieldTypeToLegacy } from "../utils/fieldTypeLabels";
import { getDefaultGroupHeaderLabel } from "../components/FieldRenderer";
import type { CostEstimateTemplateDetails } from "../api/costEstimateTemplateApi";
import { costEstimateTemplateApi } from "../api/costEstimateTemplateApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { CostEstimateTableView } from "../components/CostEstimate/CostEstimateTableView";
import type { 
  CostEstimateDetailsWeb, 
  CostEstimateGroupWeb, 
  CostEstimateItemWeb,
  CostEstimateFieldValueWeb,
} from "../types/costEstimate.types.new";
import { CostEstimateStatus, CostEstimateAccessLevel } from "../types/costEstimate.types.new";

// Aliasy dla kompatybilności wstecznej (jeśli używane w starym kodzie)
type CostEstimateItemFieldValueWeb = CostEstimateFieldValueWeb;
type CostEstimateGroupFieldValueWeb = CostEstimateFieldValueWeb;
import MainLayout from "../layout/MainLayout";
import { useTouchReorder } from "../hooks/useTouchReorder";
import { LoadingSpinner } from "../components/common";

// Funkcja generująca unikalne GUID dla fieldName
  const generateFieldGuid = (): string => {
    return crypto.randomUUID();
  };

// Backward compatibility labels
const calculatedFieldTypeLabels: Record<CalculatedFieldType, string> = {
  [CalculatedFieldType.UnitPriceNet]: fieldTypeLabels[FieldType.ItemCalculatedUnitPriceNet],
  [CalculatedFieldType.VatRate]: fieldTypeLabels[FieldType.ItemCalculatedVatRate],
  [CalculatedFieldType.UnitPriceGross]: fieldTypeLabels[FieldType.ItemCalculatedUnitPriceGross],
  [CalculatedFieldType.ValueNet]: fieldTypeLabels[FieldType.ItemCalculatedValueNet],
  [CalculatedFieldType.ValueGross]: fieldTypeLabels[FieldType.ItemCalculatedValueGross],
  [CalculatedFieldType.UnitVat]: fieldTypeLabels[FieldType.ItemCalculatedUnitVat],
  [CalculatedFieldType.TotalVat]: fieldTypeLabels[FieldType.ItemCalculatedTotalVat],
  // Discount (207) usunięte — pole ItemCalculatedDiscount zostało wycofane
};

const genericFieldTypeLabels: Record<GenericFieldType, string> = {
  [GenericFieldType.Integer]: fieldTypeLabels[FieldType.ItemGenericInteger],
  [GenericFieldType.Decimal]: fieldTypeLabels[FieldType.ItemGenericDecimal],
  [GenericFieldType.String]: fieldTypeLabels[FieldType.ItemGenericString],
  [GenericFieldType.Boolean]: fieldTypeLabels[FieldType.ItemGenericBoolean],
  [GenericFieldType.Date]: fieldTypeLabels[FieldType.ItemGenericDate],
  [GenericFieldType.DateTime]: fieldTypeLabels[FieldType.ItemGenericDateTime],
  [GenericFieldType.Collection]: 'Kolekcja',
};

const groupHeaderFieldTypeLabels: Record<GroupHeaderFieldType, string> = {
  [GroupHeaderFieldType.GroupName]: fieldTypeLabels[FieldType.GroupName],
  [GroupHeaderFieldType.GroupDescription]: fieldTypeLabels[FieldType.GroupDescription],
  [GroupHeaderFieldType.GroupNumber]: fieldTypeLabels[FieldType.GroupNumber],
  [GroupHeaderFieldType.StartDate]: fieldTypeLabels[FieldType.GroupStartDate],
  [GroupHeaderFieldType.EndDate]: fieldTypeLabels[FieldType.GroupEndDate],
  [GroupHeaderFieldType.Status]: fieldTypeLabels[FieldType.GroupStatus],
  [GroupHeaderFieldType.Notes]: fieldTypeLabels[FieldType.GroupNotes],
  [GroupHeaderFieldType.Responsible]: fieldTypeLabels[FieldType.GroupResponsible],
  [GroupHeaderFieldType.Budget]: fieldTypeLabels[FieldType.GroupBudget],
  [GroupHeaderFieldType.Priority]: fieldTypeLabels[FieldType.GroupPriority],
};

const summaryScopeLabels: Record<SummaryScope, string> = {
  [SummaryScope.Group]: "W etapie",
  [SummaryScope.Total]: "W całości",
  [SummaryScope.Both]: "W etapie i całości",
};

export default function CostEstimateTemplateEditor() {
  const { templateId } = useParams<{ templateId: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  
  const [loading, setLoading] = useState(true);
  const [template, setTemplate] = useState<CostEstimateTemplateDetails | null>(null);
  const [templateName, setTemplateName] = useState("");
  const [templateDescription, setTemplateDescription] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  // Waluty i jednostki — domyślnie PLN, żeby zawsze była waluta polska
  const DEFAULT_PLN_CURRENCY = { code: 'PLN', name: 'Polski Złoty', symbol: 'zł', isDefault: true, order: 0 };
  const [currencies, setCurrencies] = useState<Array<{
    code: string;
    name: string;
    symbol?: string;
    isDefault: boolean;
    order: number;
  }>>([DEFAULT_PLN_CURRENCY]);
  const [units, setUnits] = useState<Array<{
    code: string;
    name: string;
    symbol: string;
    category?: string;
    isDefault: boolean;
    order: number;
  }>>([]);

  const [categories, setCategories] = useState<Array<{
    name: string;
    symbol: string | null;
    order: number;
  }>>([]);

  const { isOpen: isConfirmSaveOpen, onOpen: onConfirmSaveOpen, onClose: onConfirmSaveClose } = useDisclosure();
  const { isOpen: isUnsavedOpen, onOpen: onUnsavedOpen, onClose: onUnsavedClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);
  const unsavedCancelRef = useRef<HTMLButtonElement>(null);

  // Preview state
  const { isOpen: isPreviewOpen, onOpen: onPreviewOpen, onClose: onPreviewClose } = useDisclosure();
  const [previewData, setPreviewData] = useState<CostEstimateDetailsWeb | null>(null);

  // Template Structure State
  const [canAddGroups, setCanAddGroups] = useState(true);
  const [canBranchGroups, setCanBranchGroups] = useState(true);
  const [maxGroupLevel, setMaxGroupLevel] = useState<number | undefined>(undefined);

  // Group Definition State
  const [groupAutoNumbered, setGroupAutoNumbered] = useState(true);
  const [groupNumberFormat, setGroupNumberFormat] = useState("");
  const [headerFields, setHeaderFields] = useState<GroupHeaderFieldDefinition[]>([
    {
      type: GroupHeaderFieldType.GroupName,
      required: true,
      visible: true,
      order: 0,
      readOnly: false,
    },
  ]);

  // Work Scope Fields State
  const [systemFields, setSystemFields] = useState<SystemFieldDefinition[]>([]);
  const [calculatedFields, setCalculatedFields] = useState<CalculatedFieldDefinition[]>([]);
  const [genericFields, setGenericFields] = useState<GenericFieldDefinition[]>([]);
  const [validationRules, setValidationRules] = useState<CrossFieldValidationRule[]>([]);

  // Summary Configuration State
  const [showGroupSummary, setShowGroupSummary] = useState(true);
  const [showTotalSummary, setShowTotalSummary] = useState(true);
  const [groupSummaryFields, setGroupSummaryFields] = useState<string[]>([]);
  const [totalSummaryFields, setTotalSummaryFields] = useState<string[]>([]);

  // UI Configuration State
  const [columns, setColumns] = useState<ColumnConfigurationWeb[]>([]);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);

  // Mobile tab navigation
  const [activeTab, setActiveTab] = useState(0);
  const isMobile = useBreakpointValue({ base: true, md: false });

  // Obsługa przeciągania na urządzeniach dotykowych (smartfony, tablety)
  const { createTouchHandlers } = useTouchReorder();

  // Field Type Configurations (loaded from BE)
  const [fieldTypeConfigs, setFieldTypeConfigs] = useState<Record<string, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>>({});
  const [configsLoaded, setConfigsLoaded] = useState(false);

  // ========== RĘCZNA OBSŁUGA NAWIGACJI Z OSTRZEŻENIEM ==========
  const [pendingNavigation, setPendingNavigation] = useState<string | null>(null);
  const [isBackNavigation, setIsBackNavigation] = useState(false);
  
  // Ref do przechowywania aktualnej wartości hasChanges (unikamy problemów z closure)
  const hasChangesRef = useRef(hasChanges);
  useEffect(() => {
    hasChangesRef.current = hasChanges;
  }, [hasChanges]);

  const handleConfirmLeave = useCallback(() => {
    onUnsavedClose();
    setHasChanges(false);
    hasChangesRef.current = false;
    
    if (isBackNavigation) {
      // Nawigacja przyciskiem wstecz - użyj history.back()
      setIsBackNavigation(false);
      setPendingNavigation(null);
      window.history.back();
    } else if (pendingNavigation) {
      // Normalna nawigacja przez safeNavigate
      navigate(pendingNavigation);
      setPendingNavigation(null);
    }
  }, [pendingNavigation, isBackNavigation, navigate, onUnsavedClose]);

  const handleCancelLeave = useCallback(() => {
    onUnsavedClose();
    setPendingNavigation(null);
    setIsBackNavigation(false);
  }, [onUnsavedClose]);

  /** Bezpieczna nawigacja - pokazuje modal jeśli są niezapisane zmiany */
  const safeNavigate = useCallback((to: string) => {
    if (hasChangesRef.current) {
      setPendingNavigation(to);
      onUnsavedOpen();
    } else {
      navigate(to);
    }
  }, [navigate, onUnsavedOpen]);

  // ========== BEFOREUNLOAD (zamykanie karty / odświeżanie) ==========
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (hasChangesRef.current) {
        e.preventDefault();
      }
    };
    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, []);

  // ========== POPSTATE (przycisk wstecz/dalej przeglądarki) ==========
  useEffect(() => {
    // Zapisz aktualną pozycję w historii
    const currentPath = window.location.pathname + window.location.search;
    
    const handlePopState = () => {
      if (hasChangesRef.current) {
        // Przywróć poprzedni URL (anuluj nawigację)
        window.history.pushState(null, '', currentPath);
        // Oznacz jako nawigację wstecz i pokaż modal
        setIsBackNavigation(true);
        onUnsavedOpen();
      }
    };
    
    // Dodaj wpis do historii, aby móc przechwycić przycisk wstecz
    window.history.pushState(null, '', currentPath);
    window.addEventListener('popstate', handlePopState);
    
    return () => {
      window.removeEventListener('popstate', handlePopState);
    };
  }, [onUnsavedOpen]);

  // ========== ŚLEDZENIE ZMIAN ==========
  // Ref do sprawdzenia czy dane początkowe zostały załadowane
  const initializedRef = useRef(false);

  // Śledzenie zmian w polach szablonu (po początkowym załadowaniu)
  useEffect(() => {
    // Nie ustawiaj hasChanges podczas ładowania
    if (loading || !initializedRef.current) return;
    setHasChanges(true);
  }, [
    templateName,
    templateDescription,
    currencies,
    units,
    canAddGroups,
    canBranchGroups,
    maxGroupLevel,
    groupAutoNumbered,
    groupNumberFormat,
    headerFields,
    systemFields,
    calculatedFields,
    genericFields,
    validationRules,
    showGroupSummary,
    showTotalSummary,
    groupSummaryFields,
    totalSummaryFields,
    columns,
  ]);

  // Fetch field type configurations on mount
  useEffect(() => {
    loadFieldConfigurations();
  }, []);

  const loadFieldConfigurations = async () => {
    try {
      const configs = await costEstimateApi.getFieldTypeConfigurations();
      setFieldTypeConfigs(configs);
      setConfigsLoaded(true);
    } catch (err) {
      setConfigsLoaded(true); // Continue anyway
    }
  };

  // Fetch template details when editing
  useEffect(() => {
    if (templateId) {
      fetchTemplateDetails();
    } else {
      setLoading(false);
      // Dla nowego szablonu - oznacz jako zainicjalizowane
      setTimeout(() => {
        initializedRef.current = true;
      }, 100);
    }
  }, [templateId]);

  const fetchTemplateDetails = async () => {
    if (!templateId) return;
    
    setLoading(true);
    try {

      const details = await costEstimateTemplateApi.getTemplateDetails(templateId);
      setTemplate(details);
      
      setTemplateName(details.name);
      setTemplateDescription(details.description ?? "");

      // Użyj structure z nowego API (bez wersjonowania)
      if (details.structure) {
        const struct = details.structure;
        
        // Podstawowe ustawienia (z głównego obiektu details)
        setCanAddGroups(details.canAddGroups);
        setCanBranchGroups(details.canBranchGroups);
        setMaxGroupLevel(details.maxGroupLevel);
        setGroupAutoNumbered(details.autoNumberGroups);
        setGroupNumberFormat(details.groupNumberFormat ?? "");

        // Pola nagłówka grupy - mapuj z GroupHeaderFieldWeb na GroupHeaderFieldDefinition
        setHeaderFields(struct.groupHeaderFields.map(f => {
          // Pobierz fieldType z fieldTypeConfig jeśli f.fieldType nie istnieje
          const fieldType = f.fieldType ?? f.fieldTypeConfig?.fieldType ?? 0;
          return {
          id: f.id,
          name: f.fieldName, // GUID pola
          type: fieldType as GroupHeaderFieldType, // Already in correct range 0-9
          customLabel: f.customLabel,
          required: f.isRequired,
          visible: f.isVisible,
          sortable: f.isSortable ?? true,
          filterable: f.isFilterable ?? true,
          order: f.order,
          readOnly: f.isReadonly,
          defaultValue: f.defaultValue,
          allowedValues: f.allowedValues,
          placeholder: f.placeholder,
          displayFormat: f.displayFormat,
          helpText: f.helpText,
          helpUrl: f.helpUrl,
          icon: f.icon,
          color: f.color,
          fieldTypeConfig: f.fieldTypeConfig, // Zachowaj config z API
        };
        }));

        // Pola systemowe - mapuj z SystemFieldWeb na SystemFieldDefinition
        setSystemFields(struct.systemFields.map(f => {
          // Pobierz fieldType z fieldTypeConfig jeśli f.fieldType nie istnieje
          const fieldType = f.fieldType ?? f.fieldTypeConfig?.fieldType ?? 100;
          return {
          id: f.id,
          type: convertFieldTypeToLegacy(fieldType), // FieldType (100-104) → SystemFieldType (0-4)
          name: f.fieldName,
          label: f.label,
          description: f.description,
          required: f.isRequired,
          visible: f.isVisible,
          order: f.order,
          defaultValue: f.defaultValue,
          helpText: f.helpText,
          helpUrl: f.helpUrl,
          sortable: f.isSortable,
          filterable: f.isFilterable,
          readOnly: f.isReadonly || false,
          fieldTypeConfig: f.fieldTypeConfig, // Zachowaj config z API
          // Mapuj childFields jeśli istnieją (dla pola Options)
          childFields: f.childFields?.map(child => {
            // Pobierz fieldType z fieldTypeConfig jeśli child.fieldType nie istnieje
            const childFieldType = child.fieldType ?? child.fieldTypeConfig?.fieldType ?? 100;
            const childLegacyType = convertFieldTypeToLegacy(childFieldType);
            const childScope = child.fieldTypeConfig?.fieldScope ?? Math.floor(childFieldType / 100);
            
            if (childScope === 1) {
              // System field
              return {
                name: child.fieldName,
                label: child.label,
                type: childLegacyType,
                order: child.order ?? 0,
                required: child.isRequired,
                visible: child.isVisible,
                sortable: child.isSortable,
                filterable: child.isFilterable,
                readOnly: child.isReadonly || false,
                fieldTypeConfig: child.fieldTypeConfig,
              } as SystemFieldDefinition;
            } else if (childScope === 2) {
              // Calculated field
              return {
                name: child.fieldName,
                label: child.label,
                type: childLegacyType,
                order: child.order ?? 0,
                required: child.isRequired,
                visible: child.isVisible,
                sortable: child.isSortable,
                filterable: child.isFilterable,
                summable: false,
                summaryScope: SummaryScope.Both,
                autoCalculated: false,
                readOnly: child.isReadonly || false,
                fieldTypeConfig: child.fieldTypeConfig,
              } as CalculatedFieldDefinition;
            } else {
              // Generic field
              return {
                name: child.fieldName,
                label: child.label,
                type: childLegacyType,
                order: child.order ?? 0,
                required: child.isRequired,
                visible: child.isVisible,
                sortable: child.isSortable,
                filterable: child.isFilterable,
                readOnly: child.isReadonly || false,
                fieldTypeConfig: child.fieldTypeConfig,
              } as GenericFieldDefinition;
            }
          }),
        };
        }));

        // Pola kalkulowane - mapuj z CalculatedFieldWeb na CalculatedFieldDefinition
        setCalculatedFields(struct.calculatedFields.map(f => {
          // Pobierz fieldType z fieldTypeConfig jeśli f.fieldType nie istnieje
          const fieldType = f.fieldType ?? f.fieldTypeConfig?.fieldType ?? 200;
          const legacyType = convertFieldTypeToLegacy(fieldType); // FieldType (200-206) → CalculatedFieldType (0-6)
          // Jeśli backend nie zwraca isSummable, ustaw domyślną wartość bazując na typie pola
          const summable = f.isSummable !== undefined 
            ? f.isSummable 
            : (legacyType === CalculatedFieldType.ValueNet || 
               legacyType === CalculatedFieldType.ValueGross || 
               legacyType === CalculatedFieldType.TotalVat);
          
          return {
            id: f.id,
            type: legacyType,
            name: f.fieldName, // Użyj fieldName z API zamiast generować
            label: f.label,
            description: f.description,
            unit: f.unit,
            displayFormat: f.displayFormat,
            sortable: f.isSortable,
            filterable: f.isFilterable,
            summable: summable,
            summaryScope: f.summaryScope,
            sumInGroup: f.sumInGroup, // Sumowanie w grupie
            sumInTotal: f.sumInTotal, // Sumowanie w podsumowaniu całkowitym
            autoCalculated: f.isAutoCalculated,
            calculationFormula: f.calculationFormula,
            readOnly: f.isReadonly,
            required: f.isRequired,
            visible: f.isVisible,
            order: f.order,
            defaultValue: f.defaultValue,
            helpText: f.helpText,
            helpUrl: f.helpUrl,
            fieldTypeConfig: f.fieldTypeConfig, // Zachowaj config z API
          };
        }));

        // Pola generyczne - mapuj z GenericFieldWeb na GenericFieldDefinition
        setGenericFields(struct.genericFields.map(f => {
          // Pobierz fieldType z fieldTypeConfig jeśli f.fieldType nie istnieje
          const fieldType = f.fieldType ?? f.fieldTypeConfig?.fieldType ?? 300;
          return {
          id: f.id,
          type: convertFieldTypeToLegacy(fieldType), // FieldType (300-305) → GenericFieldType (0-5)
          name: f.fieldName,
          label: f.label,
          description: f.description,
          displayFormat: f.displayFormat,
          sortable: f.isSortable,
          filterable: f.isFilterable,
          readOnly: f.isReadonly || false,
          minValue: f.minValue,
          maxValue: f.maxValue,
          minLength: f.minLength,
          maxLength: f.maxLength,
          pattern: f.pattern,
          required: f.isRequired,
          visible: f.isVisible,
          order: f.order,
          defaultValue: f.defaultValue,
          allowedValues: f.allowedValues,
          placeholder: f.placeholder,
          helpText: f.helpText,
          helpUrl: f.helpUrl,
          fieldTypeConfig: f.fieldTypeConfig, // Zachowaj config z API
        };
        }));

        setValidationRules([]);

        // Konfiguracja podsumowań
        setShowGroupSummary(struct.summaryConfiguration?.showGroupSummary ?? true);
        setShowTotalSummary(struct.summaryConfiguration?.showTotalSummary ?? true);
        setGroupSummaryFields(struct.summaryConfiguration?.groupSummaryFields.map(f => f.fieldName) ?? []);
        setTotalSummaryFields(struct.summaryConfiguration?.totalSummaryFields.map(f => f.fieldName) ?? []);

        // Konfiguracja UI
        setColumns(struct.uiConfiguration?.columns ?? []);
        
        // Załaduj waluty i jednostki — upewnij się, że PLN jest zawsze obecny
        const loadedCurrencies = struct.currencies.map(c => ({
          code: c.code,
          name: c.name,
          symbol: c.symbol,
          isDefault: c.isDefault,
          order: c.order,
        }));
        if (!loadedCurrencies.some(c => c.code === 'PLN')) {
          loadedCurrencies.push({ ...DEFAULT_PLN_CURRENCY, isDefault: loadedCurrencies.length === 0, order: loadedCurrencies.length });
        }
        setCurrencies(loadedCurrencies);
        setUnits(struct.units.map(u => ({
          code: u.code,
          name: u.name,
          symbol: u.symbol,
          category: u.category,
          isDefault: u.isDefault,
          order: u.order,
        })));
        setCategories((struct.categories ?? []).map(c => ({
          name: c.name,
          symbol: c.symbol,
          order: c.order,
        })));
      }
    } catch (error: any) {
      toast({
        title: "Błąd",
        description: "Nie udało się załadować szablonu",
        status: "error",
        duration: 5000,
      });
      navigate("/cost-estimate-templates");
    } finally {
      setLoading(false);
      // Oznacz jako zainicjalizowane dopiero po załadowaniu
      setTimeout(() => {
        initializedRef.current = true;
      }, 100);
    }
  };

  // Funkcja generująca przykładowy kosztorys z szablonu (nowy format CostEstimateDetailsWeb)
  const generateSampleCostEstimate = (): CostEstimateDetailsWeb => {
    const sampleGroups: CostEstimateGroupWeb[] = [];
    const now = new Date().toISOString();

    // Generuj 2 przykładowe grupy
    for (let i = 0; i < 2; i++) {
      const groupId = `sample-group-${i}`;
      const items: CostEstimateItemWeb[] = [];
      
      // Generuj 3 przykładowe pozycje w grupie
      for (let j = 0; j < 3; j++) {
        const itemId = `sample-item-${i}-${j}`;
        const fieldValues: CostEstimateItemFieldValueWeb[] = [];

        // Dodaj wartości dla pól systemowych
        systemFields.forEach((field) => {
          const fieldType = field.fieldTypeConfig?.fieldType ?? (field.type + 100);
          const fieldScope = field.fieldTypeConfig?.fieldScope ?? 1;
          
          // Określ wartość i typ
          let stringValue: string | undefined;
          let decimalValue: number | undefined;
          let boolValue: boolean | undefined;

          if (field.type === SystemFieldType.Name) {
            stringValue = `Pozycja ${i + 1}.${j + 1}`;
          } else if (field.type === SystemFieldType.Quantity) {
            decimalValue = 10 + j * 5;
          } else if (field.type === SystemFieldType.Unit) {
            stringValue = units[0]?.code || 'szt';
          } else if (field.type === SystemFieldType.Selected) {
            boolValue = true;
          }

          if (stringValue !== undefined || decimalValue !== undefined || boolValue !== undefined) {
            fieldValues.push({
              id: `fv-sys-${i}-${j}-${field.name}`,
              fieldDefinitionId: field.id || field.name,
              fieldType,
              fieldScope,
              fieldName: field.name,
              fieldLabel: field.label,
              stringValue,
              decimalValue,
              boolValue,
            });
          }
        });

        // Dodaj wartości dla pól kalkulowanych
        calculatedFields.forEach((field) => {
          const fieldType = field.fieldTypeConfig?.fieldType ?? (field.type + 200);
          const fieldScope = field.fieldTypeConfig?.fieldScope ?? 2;
          
          let decimalValue: number | undefined;
          if (field.type === CalculatedFieldType.UnitPriceNet) {
            decimalValue = 100 + j * 50;
          } else if (field.type === CalculatedFieldType.VatRate) {
            decimalValue = 23;
          } else if (field.type === CalculatedFieldType.UnitPriceGross) {
            const unitPriceNet = 100 + j * 50;
            decimalValue = unitPriceNet * 1.23;
          } else if (field.type === CalculatedFieldType.ValueNet) {
            const unitPriceNet = 100 + j * 50;
            const quantity = 10 + j * 5;
            decimalValue = unitPriceNet * quantity;
          } else if (field.type === CalculatedFieldType.ValueGross) {
            const unitPriceNet = 100 + j * 50;
            const quantity = 10 + j * 5;
            decimalValue = unitPriceNet * quantity * 1.23;
          } else if (field.type === CalculatedFieldType.UnitVat) {
            const unitPriceNet = 100 + j * 50;
            decimalValue = unitPriceNet * 0.23;
          } else if (field.type === CalculatedFieldType.TotalVat) {
            const unitPriceNet = 100 + j * 50;
            const quantity = 10 + j * 5;
            decimalValue = unitPriceNet * quantity * 0.23;
          }

          if (decimalValue !== undefined) {
            fieldValues.push({
              id: `fv-calc-${i}-${j}-${field.name}`,
              fieldDefinitionId: field.id || field.name,
              fieldType,
              fieldScope,
              fieldName: field.name,
              fieldLabel: field.label,
              decimalValue,
            });
          }
        });

        // Dodaj wartości dla pól generycznych
        genericFields.forEach((field) => {
          const fieldType = field.fieldTypeConfig?.fieldType ?? (field.type + 300);
          const fieldScope = field.fieldTypeConfig?.fieldScope ?? 3;
          
          let stringValue: string | undefined;
          let decimalValue: number | undefined;
          let boolValue: boolean | undefined;
          let dateTimeValue: string | undefined;

          if (field.type === GenericFieldType.String) {
            stringValue = `Przykładowy tekst ${j + 1}`;
          } else if (field.type === GenericFieldType.Integer) {
            decimalValue = 10 + j;
          } else if (field.type === GenericFieldType.Decimal) {
            decimalValue = 10.5 + j;
          } else if (field.type === GenericFieldType.Boolean) {
            boolValue = j % 2 === 0;
          } else if (field.type === GenericFieldType.Date) {
            const date = new Date();
            date.setDate(date.getDate() + j);
            dateTimeValue = date.toISOString().split('T')[0];
          } else if (field.type === GenericFieldType.DateTime) {
            const date = new Date();
            date.setDate(date.getDate() + j);
            dateTimeValue = date.toISOString();
          }

          if (stringValue !== undefined || decimalValue !== undefined || boolValue !== undefined || dateTimeValue !== undefined) {
            fieldValues.push({
              id: `fv-gen-${i}-${j}-${field.name}`,
              fieldDefinitionId: field.id || field.name,
              fieldType,
              fieldScope,
              fieldName: field.name,
              fieldLabel: field.label,
              stringValue,
              decimalValue,
              boolValue,
              dateTimeValue,
            });
          }
        });

        items.push({
          id: itemId,
          groupId,
          order: j,
          fieldValues,
          options: [],
          createdAt: now,
        });
      }

      // Wartości nagłówka grupy
      const groupFieldValues: CostEstimateGroupFieldValueWeb[] = [];
      headerFields.forEach((field) => {
        const fieldType = field.fieldTypeConfig?.fieldType ?? field.type;
        
        let stringValue: string | undefined;
        if (field.type === GroupHeaderFieldType.GroupName) {
          stringValue = `Przykładowy etap ${i + 1}`;
        } else if (field.type === GroupHeaderFieldType.GroupNumber) {
          stringValue = `${i + 1}`;
        } else if (field.type === GroupHeaderFieldType.GroupDescription) {
          stringValue = `Opis przykładowego etapu ${i + 1}`;
        }

        if (stringValue !== undefined) {
          groupFieldValues.push({
            id: `gfv-${i}-${field.name}`,
            fieldDefinitionId: field.id || field.name || `header-${field.type}`,
            fieldType,
            fieldScope: 4, // Group scope
            fieldLabel: field.customLabel || groupHeaderFieldTypeLabels[field.type],
            stringValue,
          });
        }
      });

      // Oblicz sumy dla grupy
      let groupTotalNet = 0;
      let groupTotalGross = 0;
      let groupTotalVat = 0;
      
      items.forEach(item => {
        const valueNetField = item.fieldValues.find(fv => fv.fieldName && calculatedFields.some(cf => cf.name === fv.fieldName && cf.type === CalculatedFieldType.ValueNet));
        const valueGrossField = item.fieldValues.find(fv => fv.fieldName && calculatedFields.some(cf => cf.name === fv.fieldName && cf.type === CalculatedFieldType.ValueGross));
        const totalVatField = item.fieldValues.find(fv => fv.fieldName && calculatedFields.some(cf => cf.name === fv.fieldName && cf.type === CalculatedFieldType.TotalVat));
        
        if (valueNetField?.decimalValue) groupTotalNet += valueNetField.decimalValue;
        if (valueGrossField?.decimalValue) groupTotalGross += valueGrossField.decimalValue;
        if (totalVatField?.decimalValue) groupTotalVat += totalVatField.decimalValue;
      });

      sampleGroups.push({
        id: groupId,
        level: 0,
        order: i,
        fieldValues: groupFieldValues,
        totalNet: groupTotalNet,
        totalGross: groupTotalGross,
        totalVat: groupTotalVat,
        lastCalculatedAt: now,
        childGroups: [],
        items,
        createdAt: now,
      });
    }

    // Oblicz sumy całkowite
    const totalNet = sampleGroups.reduce((sum, g) => sum + (g.totalNet || 0), 0);
    const totalGross = sampleGroups.reduce((sum, g) => sum + (g.totalGross || 0), 0);
    const totalVat = sampleGroups.reduce((sum, g) => sum + (g.totalVat || 0), 0);

    // Zbuduj templateStructure na podstawie aktualnej konfiguracji (bez wersjonowania)
    const templateStructure: CostEstimateTemplateStructureWeb = {
      templateId: template?.id || 'preview-template',
      currencies: currencies.map((c, idx) => ({
        id: c.code,
        code: c.code,
        name: c.name,
        symbol: c.symbol,
        isDefault: c.isDefault,
        order: idx,
      })),
      units: units.map((u, idx) => ({
        id: u.code,
        code: u.code,
        name: u.name,
        symbol: u.symbol,
        category: u.category,
        isDefault: u.isDefault,
        order: idx,
      })),
      categories: categories.map((c, idx) => ({
        id: `cat-${idx}`,
        name: c.name,
        symbol: c.symbol,
        order: idx,
      })),
      groupHeaderFields: headerFields.map((f, idx) => ({
        id: f.id || f.name || `header-${f.type}`,
        fieldName: f.name || `header-${f.type}`,
        fieldType: f.type,
        customLabel: f.customLabel,
        isRequired: f.required,
        isVisible: f.visible,
        isReadonly: f.readOnly || false,
        isSortable: f.sortable ?? true,
        isFilterable: f.filterable ?? true,
        order: idx,
        fieldTypeConfig: f.fieldTypeConfig,
      })),
      systemFields: systemFields.map((f, idx) => ({
        id: f.id || f.name,
        fieldName: f.name,
        fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 100),
        label: f.label,
        isRequired: f.required,
        isVisible: f.visible,
        isReadonly: f.readOnly || false,
        isSortable: f.sortable || false,
        isFilterable: f.filterable || false,
        order: idx,
        fieldTypeConfig: f.fieldTypeConfig,
        childFields: f.childFields?.map((cf, cfIdx) => ({
          id: cf.id || cf.name,
          fieldName: cf.name,
          fieldType: cf.fieldTypeConfig?.fieldType ?? ((cf as any).type + 100),
          label: cf.label,
          isRequired: (cf as any).required || false,
          isVisible: (cf as any).visible !== false,
          isReadonly: (cf as any).readOnly || false,
          isSortable: cf.sortable || false,
          isFilterable: cf.filterable || false,
          order: cfIdx,
          fieldTypeConfig: cf.fieldTypeConfig,
        })),
      })),
      calculatedFields: calculatedFields.map((f, idx) => ({
        id: f.id || f.name,
        fieldName: f.name,
        fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 200),
        label: f.label,
        isRequired: f.required || false,
        isVisible: f.visible,
        isSortable: f.sortable || false,
        isFilterable: f.filterable || false,
        isSummable: f.summable || false,
        summaryScope: f.summaryScope,
        isAutoCalculated: f.autoCalculated || false,
        isReadonly: f.readOnly || false,
        order: idx,
        fieldTypeConfig: f.fieldTypeConfig,
      })),
      genericFields: genericFields.map((f, idx) => ({
        id: f.id || f.name,
        fieldName: f.name,
        fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 300),
        label: f.label,
        isRequired: f.required,
        isVisible: f.visible,
        isReadonly: f.readOnly || false,
        isSortable: f.sortable || false,
        isFilterable: f.filterable || false,
        order: idx,
        fieldTypeConfig: f.fieldTypeConfig,
      })),
      summaryConfiguration: {
        showGroupSummary,
        showTotalSummary,
        groupSummaryFields: groupSummaryFields.map((fieldName, idx) => {
          const field = calculatedFields.find(f => f.name === fieldName);
          return {
            fieldId: field?.id || fieldName,
            fieldName,
            fieldType: field?.fieldTypeConfig?.fieldType ?? ((field?.type || 0) + 200),
            fieldLabel: field?.label || fieldName,
            fieldSource: 2, // FieldScope.Calculated
            order: idx,
          };
        }),
        totalSummaryFields: totalSummaryFields.map((fieldName, idx) => {
          const field = calculatedFields.find(f => f.name === fieldName);
          return {
            fieldId: field?.id || fieldName,
            fieldName,
            fieldType: field?.fieldTypeConfig?.fieldType ?? ((field?.type || 0) + 200),
            fieldLabel: field?.label || fieldName,
            fieldSource: 2, // FieldScope.Calculated
            order: idx,
          };
        }),
      },
      uiConfiguration: {
        columns: columns,
      },
    };

    return {
      id: 'preview-cost-estimate',
      tenantId: 'preview-tenant',
      projectId: 'preview-project',
      templateId: template?.id || 'preview-template',
      templateName: templateName || 'Nowy szablon',
      selectedCurrencyId: currencies[0]?.code || 'PLN',
      selectedCurrencyCode: currencies[0]?.code || 'PLN',
      selectedCurrencySymbol: currencies[0]?.symbol || 'zł',
      name: 'Przykładowy kosztorys',
      description: 'Podgląd szablonu z przykładowymi danymi',
      status: CostEstimateStatus.Draft,
      rootGroups: sampleGroups,
      totalNet,
      totalGross,
      totalVat,
      createdAt: now,
      lastCalculatedAt: now,
      ownerId: 'preview-user',
      ownerName: 'Użytkownik podglądu',
      templateStructure,
      accessLevel: CostEstimateAccessLevel.Full,
      sharedWithUsers: [],
    };
  };

  const handlePreview = () => {
    const sampleData = generateSampleCostEstimate();
    setPreviewData(sampleData);
    onPreviewOpen();
  };

  const createDefaultCalculatedField = (type: CalculatedFieldType): CalculatedFieldDefinition => {
    const isAutoCalculated = [
      CalculatedFieldType.UnitPriceGross,
      CalculatedFieldType.ValueNet,
      CalculatedFieldType.ValueGross,
      CalculatedFieldType.UnitVat,
      CalculatedFieldType.TotalVat,
    ].includes(type);

    // Tylko ValueNet (4), ValueGross (5) i TotalVat (7) mogą być sumowalne
    const isSummable = type === CalculatedFieldType.ValueNet ||
                       type === CalculatedFieldType.ValueGross ||
                       type === CalculatedFieldType.TotalVat;

    // Pobierz konfigurację z BE (FieldType = CalculatedFieldType + 200)
    const fieldTypeForConfig = type + 200;
    const config = (fieldTypeConfigs[2] || []).find(c => c.fieldType === fieldTypeForConfig);
    const label = config?.namePl || calculatedFieldTypeLabels[type];

    return {
      name: generateFieldGuid(),
      label: label,
      type: type,
      order: calculatedFields.length + genericFields.length,
      required: false,
      visible: true,
      sortable: true,
      filterable: true,
      summable: isSummable,
      summaryScope: SummaryScope.Both,
      autoCalculated: isAutoCalculated,
      readOnly: isAutoCalculated,
      fieldTypeConfig: config, // Zachowaj config z BE
    };
  };

  const createDefaultSystemField = (type: SystemFieldType): SystemFieldDefinition => {
    const systemFieldTypeLabels: Record<SystemFieldType, string> = {
      [SystemFieldType.Name]: "Nazwa pozycji",
      [SystemFieldType.Quantity]: "Ilość",
      [SystemFieldType.Unit]: "Jednostka miary",
      [SystemFieldType.Options]: "Opcje",
      [SystemFieldType.Selected]: "Zaznaczenie",
    };

    // Pobierz konfigurację z BE (FieldType = SystemFieldType + 100)
    const fieldTypeForConfig = type + 100;
    const config = (fieldTypeConfigs[1] || []).find(c => c.fieldType === fieldTypeForConfig);
    const label = config?.namePl || systemFieldTypeLabels[type];

    return {
      name: generateFieldGuid(),
      label: label,
      type: type,
      order: systemFields.length,
      required: type === SystemFieldType.Name,
      visible: true,
      sortable: true,
      filterable: true,
      readOnly: false,
      fieldTypeConfig: config, // Zachowaj config z BE
    };
  };

  const createDefaultGenericField = (type: GenericFieldType): GenericFieldDefinition => {
    // Pobierz konfigurację z BE (FieldType = GenericFieldType + 300)
    const fieldTypeForConfig = type + 300;
    const config = (fieldTypeConfigs[3] || []).find(c => c.fieldType === fieldTypeForConfig);
    const label = config?.namePl || genericFieldTypeLabels[type];

    return {
      name: generateFieldGuid(),
      label: label,
      type: type,
      order: calculatedFields.length + genericFields.length,
      required: false,
      visible: true,
      sortable: true,
      filterable: true,
      readOnly: false,
      fieldTypeConfig: config, // Zachowaj config z BE
    };
  };

  const handleAddHeaderField = (type: GroupHeaderFieldType) => {
    // Pobierz konfigurację z BE (scope 4 = group header, FieldType = type bezpośrednio)
    const config = (fieldTypeConfigs[4] || []).find(c => c.fieldType === type);
    const label = config?.namePl || groupHeaderFieldTypeLabels[type];

    const newField: GroupHeaderFieldDefinition = {
      name: generateFieldGuid(),
      type: type,
      customLabel: label,
      required: false,
      visible: true,
      order: headerFields.length,
      readOnly: false,
      sortable: true,
      filterable: true,
      fieldTypeConfig: config, // Zachowaj config z BE
    };
    setHeaderFields([...headerFields, newField]);
  };

  const handleRemoveHeaderField = (index: number) => {
    const updatedFields = headerFields.filter((_, i) => i !== index);
    updatedFields.forEach((field, i) => {
      field.order = i;
    });
    setHeaderFields(updatedFields);
  };

  const handleUpdateHeaderField = (
    index: number,
    updates: Partial<GroupHeaderFieldDefinition>
  ) => {
    const updatedFields = [...headerFields];
    updatedFields[index] = { ...updatedFields[index], ...updates };
    setHeaderFields(updatedFields);
  };

  const handleAddSystemField = (type: SystemFieldType) => {
    const newField = createDefaultSystemField(type);
    setSystemFields([...systemFields, newField]);
  };

  const handleRemoveSystemField = (index: number) => {
    const updatedFields = systemFields.filter((_, i) => i !== index);
    updatedFields.forEach((field, i) => {
      field.order = i;
    });
    setSystemFields(updatedFields);
  };

  const handleUpdateSystemField = (
    index: number,
    updates: Partial<SystemFieldDefinition>
  ) => {
    const updatedFields = [...systemFields];
    updatedFields[index] = { ...updatedFields[index], ...updates };
    setSystemFields(updatedFields);
  };

  const handleAddCalculatedField = (type: CalculatedFieldType) => {
    const newField = createDefaultCalculatedField(type);
    setCalculatedFields([...calculatedFields, newField]);
  };

  const handleRemoveCalculatedField = (index: number) => {
    const updatedFields = calculatedFields.filter((_, i) => i !== index);
    const allFields = [...updatedFields, ...genericFields];
    allFields.sort((a, b) => a.order - b.order);
    allFields.forEach((field, i) => {
      field.order = i;
    });
    setCalculatedFields(updatedFields);
  };

  const handleUpdateCalculatedField = (
    index: number,
    updates: Partial<CalculatedFieldDefinition>
  ) => {
    const updatedFields = [...calculatedFields];
    updatedFields[index] = { ...updatedFields[index], ...updates };
    setCalculatedFields(updatedFields);
  };

  const handleAddGenericField = (type: GenericFieldType) => {
    const newField = createDefaultGenericField(type);
    setGenericFields([...genericFields, newField]);
  };

  const handleRemoveGenericField = (index: number) => {
    const updatedFields = genericFields.filter((_, i) => i !== index);
    const allFields = [...calculatedFields, ...updatedFields];
    allFields.sort((a, b) => a.order - b.order);
    allFields.forEach((field, i) => {
      field.order = i;
    });
    setGenericFields(updatedFields);
  };

  const handleUpdateGenericField = (
    index: number,
    updates: Partial<GenericFieldDefinition>
  ) => {
    const updatedFields = [...genericFields];
    updatedFields[index] = { ...updatedFields[index], ...updates };
    setGenericFields(updatedFields);
  };

  const handleAddValidationRule = () => {
    const newRule: CrossFieldValidationRule = {
      ruleName: `rule_${validationRules.length + 1}`,
      expression: "",
      errorMessage: "",
      isActive: true,
    };
    setValidationRules([...validationRules, newRule]);
  };

  const handleRemoveValidationRule = (index: number) => {
    setValidationRules(validationRules.filter((_, i) => i !== index));
  };

  const handleUpdateValidationRule = (
    index: number,
    updates: Partial<CrossFieldValidationRule>
  ) => {
    const updatedRules = [...validationRules];
    updatedRules[index] = { ...updatedRules[index], ...updates };
    setValidationRules(updatedRules);
  };

  const validateTemplate = (): boolean => {
    if (!templateName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa szablonu jest wymagana",
        status: "error",
        duration: 3000,
      });
      return false;
    }

    if (calculatedFields.length === 0 && genericFields.length === 0) {
      toast({
        title: "Błąd walidacji",
        description: "Szablon musi zawierać przynajmniej jedno pole",
        status: "error",
        duration: 3000,
      });
      return false;
    }

    const allFieldNames = [...calculatedFields.map((f) => f.name), ...genericFields.map((f) => f.name)];
    const duplicateNames = allFieldNames.filter(
      (name, index) => allFieldNames.indexOf(name) !== index
    );
    if (duplicateNames.length > 0) {
      toast({
        title: "Błąd walidacji",
        description: `Znaleziono duplikaty nazw pól: ${duplicateNames.join(", ")}`,
        status: "error",
        duration: 3000,
      });
      return false;
    }

    return true;
  };

  // Helper do mapowania pól z childFields na DTO
  const mapFieldToDto = (field: SystemFieldDefinition | CalculatedFieldDefinition | GenericFieldDefinition): any => {
    // Użyj fieldType bezpośrednio z fieldTypeConfig jeśli dostępne
    let fieldType: number | null = field.fieldTypeConfig?.fieldType ?? null;
    
    // Jeśli nie ma fieldTypeConfig, oblicz fieldType na podstawie scope i type
    if (fieldType === null) {
      const fieldScope = field.fieldTypeConfig?.fieldScope;
      
      if (fieldScope === 1) {
        fieldType = (field as SystemFieldDefinition).type + 100;
      } else if (fieldScope === 2) {
        fieldType = (field as CalculatedFieldDefinition).type + 200;
      } else if (fieldScope === 3) {
        fieldType = (field as GenericFieldDefinition).type + 300;
      } else {
        // Fallback: użyj heurystyki bazując na właściwościach
        const hasCalculatedProps = 'summable' in field || 'autoCalculated' in field || 'summaryScope' in field;
        const hasGenericProps = 'minValue' in field || 'maxValue' in field || 'minLength' in field || 'maxLength' in field || 'pattern' in field;
        
        if (hasCalculatedProps) {
          fieldType = (field as CalculatedFieldDefinition).type + 200;
        } else if (hasGenericProps) {
          fieldType = (field as GenericFieldDefinition).type + 300;
        } else {
          // Domyślnie traktuj jako system field (tylko jeśli type <= 4)
          const sysField = field as SystemFieldDefinition;
          if (sysField.type >= 0 && sysField.type <= 4) {
            fieldType = sysField.type + 100;
          }
        }
      }
    }

    return {
      fieldName: field.name,
      fieldType,
      label: field.label,
      isSortable: 'sortable' in field ? field.sortable || false : false,
      isFilterable: 'filterable' in field ? field.filterable || false : false,
      isReadonly: 'readOnly' in field ? field.readOnly || false : false,
    };
  };

  const handleSubmitClick = () => {
    if (!validateTemplate()) return;
    handleSubmit();
  };

  const handleSubmit = async () => {
    setIsSubmitting(true);

    // Upewnij się, że PLN zawsze jest na liście walut
    const hasPLN = currencies.some(c => c.code === 'PLN');
    const finalCurrencies = hasPLN
      ? currencies
      : [...currencies, { code: 'PLN', name: 'Polski Złoty', symbol: 'zł', isDefault: currencies.length === 0, order: currencies.length }];
    if (!hasPLN) {
      setCurrencies(finalCurrencies);
    }

    try {
      if (templateId && template) {
        // Aktualizacja istniejącego szablonu (bez wersjonowania)
        await costEstimateTemplateApi.updateTemplate(templateId, {
          templateId: templateId,
          name: templateName,
          description: templateDescription || undefined,
          category: undefined,  // TODO: dodać pole category w UI
          canAddGroups,
          canBranchGroups,
          maxGroupLevel,
          autoNumberGroups: groupAutoNumbered,
          groupNumberFormat: groupNumberFormat || undefined,
          updateStructure: true, // Aktualizujemy strukturę
          currencies: finalCurrencies,
          units: units,
          categories: categories.map((c, idx) => ({ name: c.name, symbol: c.symbol, order: idx })),
          groupHeaderFields: headerFields.map(f => ({
            fieldName: f.name || generateFieldGuid(),
            fieldType: f.fieldTypeConfig?.fieldType ?? f.type,
            label: f.customLabel || groupHeaderFieldTypeLabels[f.type],
            isSortable: f.sortable || false,
            isFilterable: f.filterable || false,
            isVisible: f.visible,
            isReadonly: f.readOnly || false,
          })),
          systemFields: systemFields.map(f => ({
            fieldName: f.name,
            fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 100),
            label: f.label,
            isSortable: f.sortable || false,
            isFilterable: f.filterable || false,
            isVisible: f.visible,
            isReadonly: f.readOnly || false,
            childFields: f.childFields?.map(mapFieldToDto) || undefined,
          })),
          calculatedFields: calculatedFields.map(f => ({
            fieldName: f.name,
            fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 200),
            label: f.label,
            isSortable: f.sortable || false,
            isFilterable: f.filterable || false,
            isVisible: f.visible,
            isReadonly: f.readOnly || false,
            sumInGroup: f.sumInGroup || false,
            sumInTotal: f.sumInTotal || false,
          })),
          genericFields: genericFields.map(f => ({
            fieldName: f.name,
            fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 300),
            label: f.label,
            isSortable: f.sortable || false,
            isFilterable: f.filterable || false,
            isVisible: f.visible,
            isReadonly: f.readOnly || false,
          })),
          summaryConfiguration: {
            showGroupSummary,
            showTotalSummary,
            groupSummaryFields: groupSummaryFields.length > 0 ? groupSummaryFields : [],
            totalSummaryFields: totalSummaryFields.length > 0 ? totalSummaryFields : [],
          },
          uiConfiguration: {
            columnLayout: columns.length > 0 ? columns.map(c => c.fieldName) : undefined,
            columnWidths: undefined,
          },
        });

        toast({
          title: "Sukces",
          description: "Szablon został zaktualizowany",
          status: "success",
          duration: 3000,
        });
      } else {
        // Krok 1: Utwórz nowy szablon z nazwą i opisem
        const newTemplateId = await costEstimateTemplateApi.createTemplate({
          name: templateName,
          description: templateDescription || undefined,
        });

        // Krok 2: Zaktualizuj szablon pełną strukturą
        await costEstimateTemplateApi.updateTemplate(newTemplateId, {
          templateId: newTemplateId,
          name: templateName,
          description: templateDescription || undefined,
          category: undefined,
          canAddGroups,
          canBranchGroups,
          maxGroupLevel,
          autoNumberGroups: groupAutoNumbered,
          groupNumberFormat: groupNumberFormat || undefined,
          updateStructure: true,
          currencies: finalCurrencies,
          units: units,
          categories: categories.map((c, idx) => ({ name: c.name, symbol: c.symbol, order: idx })),
          groupHeaderFields: headerFields.map(f => ({
            fieldName: f.name || generateFieldGuid(),
            fieldType: f.fieldTypeConfig?.fieldType ?? f.type,
            label: f.customLabel || groupHeaderFieldTypeLabels[f.type],
            isSortable: f.sortable || false,
            isFilterable: f.filterable || false,
            isVisible: f.visible,
            isReadonly: f.readOnly || false,
          })),
          systemFields: systemFields.map(f => ({
            fieldName: f.name,
            fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 100),
            label: f.label,
            isSortable: f.sortable || false,
            isFilterable: f.filterable || false,
            isVisible: f.visible,
            isReadonly: f.readOnly || false,
            childFields: f.childFields?.map(mapFieldToDto) || undefined,
          })),
          calculatedFields: calculatedFields.map(f => ({
            fieldName: f.name,
            fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 200),
            label: f.label,
            isSortable: f.sortable || false,
            isFilterable: f.filterable || false,
            isVisible: f.visible,
            isReadonly: f.readOnly || false,
            sumInGroup: f.sumInGroup || false,
            sumInTotal: f.sumInTotal || false,
          })),
          genericFields: genericFields.map(f => ({
            fieldName: f.name,
            fieldType: f.fieldTypeConfig?.fieldType ?? (f.type + 300),
            label: f.label,
            isSortable: f.sortable || false,
            isFilterable: f.filterable || false,
            isVisible: f.visible,
            isReadonly: f.readOnly || false,
          })),
          summaryConfiguration: {
            showGroupSummary,
            showTotalSummary,
            groupSummaryFields: groupSummaryFields.length > 0 ? groupSummaryFields : [],
            totalSummaryFields: totalSummaryFields.length > 0 ? totalSummaryFields : [],
          },
          uiConfiguration: {
            columnLayout: columns.length > 0 ? columns.map(c => c.fieldName) : undefined,
            columnWidths: undefined,
          },
        });

        toast({
          title: "Sukces",
          description: "Szablon został utworzony",
          status: "success",
          duration: 3000,
        });
      }

      // Reset flagi zmian przed nawigacją
      setHasChanges(false);
      navigate("/cost-estimate-templates");
    } catch (error) {
      toast({
        title: "Błąd",
        description: templateId
          ? "Nie udało się zaktualizować szablonu"
          : "Nie udało się utworzyć szablonu",
        status: "error",
        duration: 5000,
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  // Render funkcja dla TabPanel "Układ pól"
  const renderFieldLayoutTab = () => {
    // Zbierz wszystkie pola z template
    // UWAGA: Zbieramy tylko pola główne (parenty), bez childFields z pól Options
    const allFields: Array<{ name: string; label: string; type: string; colorScheme: string }> = [];

    // Pola nagłówków grup (GroupName, Notes, etc.) — tylko widoczne
    headerFields.filter(f => f.visible).forEach((field) => {
      allFields.push({
        name: field.name || `header_${field.type}_temp`,  // Fallback jeśli brak GUID (nie powinno się zdarzyć)
        label: field.customLabel || getDefaultGroupHeaderLabel(field.type),
        type: 'Nagłówek etapu',
        colorScheme: 'purple',
      });
    });

    // Pola systemowe - tylko parenty, bez childFields — tylko widoczne
    systemFields.filter(f => f.visible).forEach((field) => {
      allFields.push({
        name: field.name,
        label: field.label,
        type: 'Systemowe',
        colorScheme: 'cyan',
      });
    });

    // Pola obliczeniowe — tylko widoczne
    calculatedFields.filter(f => f.visible).forEach((field) => {
      allFields.push({
        name: field.name,
        label: field.label,
        type: 'Obliczeniowe',
        colorScheme: 'blue',
      });
    });

    // Pola generyczne — tylko widoczne
    genericFields.filter(f => f.visible).forEach((field) => {
      allFields.push({
        name: field.name,
        label: field.label,
        type: 'Generyczne',
        colorScheme: 'green',
      });
    });

    // Upewnij się że columns zawiera wszystkie pola
    const existingFieldNames = columns.map(c => c.fieldName);
    allFields.forEach((field, index) => {
      if (!existingFieldNames.includes(field.name)) {
        // Dodaj brakujące pole - musimy określić fieldScope i fieldType
        let fieldScope = FieldScope.ItemGeneric;
        let fieldType = FieldType.ItemGenericString;
        
        if (systemFields.find(f => f.name === field.name)) {
          fieldScope = FieldScope.ItemSystem;
          const sysField = systemFields.find(f => f.name === field.name)!;
          fieldType = sysField.type + 100; // SystemFieldType (0-4) → FieldType (100-104)
        } else if (calculatedFields.find(f => f.name === field.name)) {
          fieldScope = FieldScope.ItemCalculated;
          const calcField = calculatedFields.find(f => f.name === field.name)!;
          fieldType = calcField.type + 200; // CalculatedFieldType (0-6) → FieldType (200-206)
        } else if (genericFields.find(f => f.name === field.name)) {
          fieldScope = FieldScope.ItemGeneric;
          const genField = genericFields.find(f => f.name === field.name)!;
          fieldType = genField.type + 300; // GenericFieldType (0-5) → FieldType (300-305)
        } else if (headerFields.find(f => f.name === field.name)) {
          fieldScope = FieldScope.Group;
          const hdrField = headerFields.find(f => f.name === field.name)!;
          fieldType = hdrField.type as unknown as FieldType; // GroupHeaderFieldType (0-9) = FieldType (0-9)
        }
        
        const newColumn: ColumnConfigurationWeb = {
          fieldId: crypto.randomUUID(),
          fieldName: field.name,
          fieldType,
          fieldLabel: field.label,
          fieldScope: fieldScope,
          order: columns.length,
        };
        
        setColumns([...columns, newColumn]);
      }
    });

    // Usuń pola które już nie istnieją
    const validFieldNames = allFields.map((f) => f.name);
    const validatedColumns = columns.filter((col) => validFieldNames.includes(col.fieldName));

    // Jeśli columns się zmienił, zaktualizuj
    if (validatedColumns.length !== columns.length || !validatedColumns.every((v, i) => v.fieldId === columns[i].fieldId)) {
      setColumns(validatedColumns);
    }

    // Sortuj pola według columns order
    const sortedFields = [...allFields].sort((a, b) => {
      const indexA = validatedColumns.findIndex(c => c.fieldName === a.name);
      const indexB = validatedColumns.findIndex(c => c.fieldName === b.name);
      return indexA - indexB;
    });

    const handleDragStart = (index: number) => {
      setDraggedIndex(index);
    };

    const handleDragOver = (e: React.DragEvent, index: number) => {
      e.preventDefault();
      if (draggedIndex === null || draggedIndex === index) return;

      const newColumns = [...validatedColumns];
      const draggedItem = newColumns[draggedIndex];
      newColumns.splice(draggedIndex, 1);
      newColumns.splice(index, 0, draggedItem);
      
      // Update order property
      const reordered = newColumns.map((col, idx) => ({ ...col, order: idx }));
      setColumns(reordered);
      setDraggedIndex(index);
    };

    const handleDragEnd = () => {
      setDraggedIndex(null);
    };

    return (
      <VStack spacing={4} align="stretch">
        <Box bg="primary.50" p={4} borderRadius="md" borderWidth="1px" borderColor="primary.200">
          <HStack spacing={2} mb={2}>
            <Icon as={Layout} color="primary.600" />
            <Text fontSize="md" fontWeight="bold" color="primary.800">
              Układ kolumn w tabeli
            </Text>
          </HStack>
          <Text fontSize="sm" color="gray.700">
            Przeciągnij i upuść pola, aby zmienić kolejność wyświetlania kolumn w tabeli. Kolejność tutaj określa
            kolejność kolumn w edytorze i podglądzie kosztorysu.
          </Text>
        </Box>

        <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
          <Text fontSize="md" fontWeight="bold" mb={4}>
            Kolejność pól ({sortedFields.length})
          </Text>

          {sortedFields.length === 0 ? (
            <Text fontSize="sm" color="gray.500">
              Brak pól w szablonie
            </Text>
          ) : (
            <VStack spacing={2} align="stretch">
              {sortedFields.map((field, index) => (
                <HStack
                  key={field.name}
                  p={3}
                  bg={draggedIndex === index ? 'primary.100' : 'gray.50'}
                  borderRadius="md"
                  borderWidth="2px"
                  borderColor={draggedIndex === index ? 'primary.400' : 'gray.200'}
                  spacing={3}
                  cursor="grab"
                  _hover={{ bg: draggedIndex === index ? 'primary.100' : 'gray.100', borderColor: 'primary.300' }}
                  _active={{ cursor: 'grabbing' }}
                  draggable
                  onDragStart={() => handleDragStart(index)}
                  onDragOver={(e) => handleDragOver(e, index)}
                  onDragEnd={handleDragEnd}
                  {...createTouchHandlers(index, draggedIndex, setDraggedIndex, (from, to) => {
                    const newCols = [...validatedColumns];
                    const [moved] = newCols.splice(from, 1);
                    newCols.splice(to, 0, moved);
                    setColumns(newCols.map((col, idx) => ({ ...col, order: idx })));
                  })}
                  transition="all 0.2s"
                >
                  <Icon as={GripVertical} color="gray.500" />
                  <Badge colorScheme={field.colorScheme} minW="90px">
                    {field.type}
                  </Badge>
                  <Text fontSize="sm" fontWeight="medium" flex="1">
                    {field.label}
                  </Text>
                </HStack>
              ))}
            </VStack>
          )}
        </Box>
      </VStack>
    );
  };

  if (loading) {
    return (
      <MainLayout>
        <LoadingSpinner />
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box maxW="1400px" mx="auto" p={{ base: 3, sm: 4, md: 6 }} pb={{ base: 24, md: 28 }}>
        {/* Header */}
        <HStack justify="space-between" mb={4} flexWrap="wrap" gap={2}>
          <HStack spacing={2} flexWrap="wrap">
            <FileText size={24} />
            <Heading size={{ base: "md", md: "lg" }}>
              {templateId ? "Edytuj szablon kosztorysu" : "Nowy szablon kosztorysu"}
            </Heading>
            {hasChanges && (
              <Badge colorScheme="orange" fontSize="xs" px={2} py={1} borderRadius="md">
                Niezapisane zmiany
              </Badge>
            )}
          </HStack>
          <HStack spacing={2}>
            <Tooltip label="Podgląd szablonu" hasArrow>
              <IconButton
                aria-label="Podgląd szablonu"
                icon={<Eye size={18} />}
                colorScheme="primary"
                variant="outline"
                size={{ base: "sm", md: "md" }}
                onClick={handlePreview}
              />
            </Tooltip>
          </HStack>
        </HStack>

        {/* Main Content */}
        <VStack spacing={4} align="stretch">
          {/* Informacje podstawowe – zwinięty accordion */}
          <Accordion allowToggle defaultIndex={templateId ? undefined : [0]}>
            <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" overflow="hidden">
              <AccordionButton
                bg={templateName ? "white" : "primary.50"}
                _expanded={{ bg: "white" }}
                px={4}
                py={3}
              >
                <HStack flex={1} spacing={2} minW={0}>
                  <Icon as={FileText} color="primary.600" boxSize={4} flexShrink={0} />
                  <Text fontSize="md" fontWeight="bold" color="primary.800">
                    Informacje podstawowe
                  </Text>
                  {templateName && (
                    <Text
                      fontSize="sm"
                      color="gray.500"
                      fontWeight="normal"
                      isTruncated
                      maxW={{ base: "120px", md: "300px" }}
                    >
                      — {templateName}
                    </Text>
                  )}
                </HStack>
                <AccordionIcon color="primary.600" />
              </AccordionButton>
              <AccordionPanel bg="white" pb={4} px={{ base: 4, md: 6 }}>
                <VStack spacing={4} align="stretch">
                  <FormControl isRequired>
                    <HStack mb={1} spacing={1}>
                      <FormLabel mb={0} fontSize="sm" fontWeight="semibold">
                        Nazwa szablonu
                      </FormLabel>
                      <Tooltip label="To pole jest wymagane" hasArrow>
                        <Box as="span" cursor="help" color="gray.400">
                          <HelpCircle size={14} />
                        </Box>
                      </Tooltip>
                    </HStack>
                    <Input
                      value={templateName}
                      onChange={(e) => setTemplateName(e.target.value)}
                      placeholder="np. Kosztorys robót budowlanych"
                      size="lg"
                    />
                  </FormControl>
                  <FormControl>
                    <FormLabel fontSize="sm" fontWeight="semibold">
                      Opis szablonu
                    </FormLabel>
                    <Textarea
                      value={templateDescription}
                      onChange={(e) => setTemplateDescription(e.target.value)}
                      placeholder="Opcjonalny opis przeznaczenia i zastosowania szablonu"
                      rows={3}
                      maxLength={500}
                    />
                    <FormHelperText textAlign="right" fontSize="xs">
                      {templateDescription.length}/500 znaków
                    </FormHelperText>
                  </FormControl>
                </VStack>
              </AccordionPanel>
            </AccordionItem>
          </Accordion>

            <Divider />

            {/* Tab navigation */}
          {/* Mobile: dropdown Select */}
          <Box display={{ base: "block", md: "none" }}>
            <Select
              value={activeTab}
              onChange={(e) => setActiveTab(parseInt(e.target.value))}
              size="md"
              fontWeight="semibold"
            >
              <option value={0}>Konfiguracja etapów</option>
              <option value={1}>Pola etapów ({headerFields.length})</option>
              <option value={2}>
                Pola pozycji ({systemFields.length + calculatedFields.length + genericFields.length})
              </option>
              <option value={3}>Parametry</option>
              <option value={4}>⠿ Kolejność pól</option>
            </Select>
          </Box>

          <Tabs
            colorScheme="primary"
            variant="enclosed"
            index={activeTab}
            onChange={setActiveTab}
          >
            {/* Desktop: standard tab list */}
            <TabList display={{ base: "none", md: "flex" }}>
              <Tooltip label="Zasady tworzenia etapów: czy można je dodawać, zagnieżdżać i numerować" placement="bottom" hasArrow>
                <Tab _selected={{ color: "primary.700", borderBottomColor: "primary.500", borderBottomWidth: "2px" }}>
                  <HStack spacing={2}>
                    <Settings size={16} />
                    <Text>Konfiguracja etapów</Text>
                  </HStack>
                </Tab>
              </Tooltip>
              <Tooltip label="Pola dostępne na poziomie etapu" placement="bottom" hasArrow>
                <Tab _selected={{ color: "primary.700", borderBottomColor: "primary.500", borderBottomWidth: "2px" }}>
                  <HStack spacing={2}>
                    <Tag size={16} />
                    <Text>Pola etapów</Text>
                    <Badge colorScheme="blue" ml={1}>{headerFields.length}</Badge>
                  </HStack>
                </Tab>
              </Tooltip>
              <Tooltip label="Pola dostępne na poziomie pozycji" placement="bottom" hasArrow>
                <Tab _selected={{ color: "primary.700", borderBottomColor: "primary.500", borderBottomWidth: "2px" }}>
                  <HStack spacing={2}>
                    <List size={16} />
                    <Text>Pola pozycji</Text>
                    <Badge colorScheme="blue" ml={1}>
                      {systemFields.length + calculatedFields.length + genericFields.length}
                    </Badge>
                  </HStack>
                </Tab>
              </Tooltip>
              <Tooltip label="Globalne parametry szablonu" placement="bottom" hasArrow>
                <Tab _selected={{ color: "primary.700", borderBottomColor: "primary.500", borderBottomWidth: "2px" }}>
                  <HStack spacing={2}>
                    <BookOpen size={16} />
                    <Text>Parametry</Text>
                  </HStack>
                </Tab>
              </Tooltip>
              <Tooltip label="Przeciągnij pola aby zmienić kolejność" placement="bottom" hasArrow>
                <Tab _selected={{ color: "primary.700", borderBottomColor: "primary.500", borderBottomWidth: "2px" }}>
                  <HStack spacing={2}>
                    <GripVertical size={16} />
                    <Layout size={16} />
                    <Text>Kolejność pól</Text>
                  </HStack>
                </Tab>
              </Tooltip>
            </TabList>

              <TabPanels>
                <TabPanel>
                  <VStack spacing={6} align="stretch">
                    <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
                      <Text fontSize="md" fontWeight="bold" mb={4}>
                        Ustawienia struktury etapów
                      </Text>
                      <VStack spacing={4} align="stretch">
                        <Checkbox
                          isChecked={canAddGroups}
                          onChange={(e) => setCanAddGroups(e.target.checked)}
                        >
                          <HStack spacing={2}>
                            <Text>Można dodawać nowe etapy podczas wypełniania</Text>
                            <Tooltip label="Użytkownicy będą mogli tworzyć dodatkowe etapy w kosztorysie">
                              <Box as="span">
                                <HelpCircle size={16} />
                              </Box>
                            </Tooltip>
                          </HStack>
                        </Checkbox>

                        <Checkbox
                          isChecked={canBranchGroups}
                          onChange={(e) => setCanBranchGroups(e.target.checked)}
                        >
                          <HStack spacing={2}>
                            <Text>Można tworzyć podetapy (rozgałęzianie)</Text>
                            <Tooltip label="Etapy mogą zawierać zagnieżdżone podetapy">
                              <Box as="span">
                                <HelpCircle size={16} />
                              </Box>
                            </Tooltip>
                          </HStack>
                        </Checkbox>

                        {canBranchGroups && (
                          <FormControl>
                            <FormLabel>Maksymalny poziom zagnieżdżenia</FormLabel>
                            <NumberInput
                              min={1}
                              max={10}
                              value={maxGroupLevel ?? ""}
                              onChange={(_, value) =>
                                setMaxGroupLevel(isNaN(value) ? undefined : value)
                              }
                            >
                              <NumberInputField placeholder="Bez limitu" />
                            </NumberInput>
                            <FormHelperText>
                              Pozostaw puste dla nieograniczonego zagnieżdżenia
                            </FormHelperText>
                          </FormControl>
                        )}
                      </VStack>
                    </Box>
                  </VStack>
                </TabPanel>

                <TabPanel>
                  <HeaderFieldsEditor
                    headerFields={headerFields}
                    onAdd={handleAddHeaderField}
                    onRemove={handleRemoveHeaderField}
                    onUpdate={handleUpdateHeaderField}
                    onReorder={setHeaderFields}
                    fieldTypeConfigs={fieldTypeConfigs}
                  />
                </TabPanel>

                <TabPanel>
                  <Accordion allowMultiple defaultIndex={[]}>
                    {/* Pola systemowe */}
                    <AccordionItem border="1px" borderColor="primary.200" borderRadius="lg" mb={3} overflow="hidden">
                      <AccordionButton
                        bg="primary.50"
                        borderLeft="3px solid"
                        borderLeftColor="primary.400"
                        _expanded={{ bg: "primary.50" }}
                        px={4}
                        py={3}
                      >
                        <HStack flex={1} spacing={2}>
                          <Icon as={FileText} color="primary.700" boxSize={5} />
                          <Text fontSize="md" fontWeight="bold" color="primary.700">
                            Pola systemowe
                          </Text>
                          <Badge colorScheme="blue" ml={1}>{systemFields.length}</Badge>
                        </HStack>
                        <AccordionIcon color="primary.700" />
                      </AccordionButton>
                      <AccordionPanel bg="white" pb={4} px={6}>
                        <SystemFieldsEditor
                          fields={systemFields}
                          onAdd={handleAddSystemField}
                          onRemove={handleRemoveSystemField}
                          onUpdate={handleUpdateSystemField}
                          fieldTypeConfigs={fieldTypeConfigs}
                        />
                      </AccordionPanel>
                    </AccordionItem>

                    {/* Pola obliczeniowe */}
                    <AccordionItem border="1px" borderColor="level1.200" borderRadius="lg" mb={3} overflow="hidden">
                      <AccordionButton
                        bg="level1.50"
                        borderLeft="3px solid"
                        borderLeftColor="level1.500"
                        _expanded={{ bg: "level1.50" }}
                        px={4}
                        py={3}
                      >
                        <HStack flex={1} spacing={2}>
                          <Icon as={Calculator} color="level1.700" boxSize={5} />
                          <Text fontSize="md" fontWeight="bold" color="level1.700">
                            Pola obliczeniowe
                          </Text>
                          <Badge colorScheme="green" ml={1}>{calculatedFields.length}</Badge>
                        </HStack>
                        <AccordionIcon color="level1.700" />
                      </AccordionButton>
                      <AccordionPanel bg="white" pb={4} px={6}>
                        <CalculatedFieldsEditor
                          fields={calculatedFields}
                          onAdd={handleAddCalculatedField}
                          onRemove={handleRemoveCalculatedField}
                          onUpdate={handleUpdateCalculatedField}
                          fieldTypeConfigs={fieldTypeConfigs}
                          units={units}
                        />
                      </AccordionPanel>
                    </AccordionItem>

                    {/* Pola generyczne */}
                    <AccordionItem border="1px" borderColor="level2.200" borderRadius="lg" overflow="hidden">
                      <AccordionButton
                        bg="level2.50"
                        borderLeft="3px solid"
                        borderLeftColor="level2.500"
                        _expanded={{ bg: "level2.50" }}
                        px={4}
                        py={3}
                      >
                        <HStack flex={1} spacing={2}>
                          <Icon as={Tag} color="level2.700" boxSize={5} />
                          <Text fontSize="md" fontWeight="bold" color="level2.700">
                            Pola generyczne
                          </Text>
                          <Badge colorScheme="purple" ml={1}>{genericFields.length}</Badge>
                        </HStack>
                        <AccordionIcon color="level2.700" />
                      </AccordionButton>
                      <AccordionPanel bg="white" pb={4} px={6}>
                        <GenericFieldsEditor
                          fields={genericFields}
                          onAdd={handleAddGenericField}
                          onRemove={handleRemoveGenericField}
                          onUpdate={handleUpdateGenericField}
                          fieldTypeConfigs={fieldTypeConfigs}
                        />
                      </AccordionPanel>
                    </AccordionItem>
                  </Accordion>
                </TabPanel>

                <TabPanel>
                  <Accordion allowMultiple defaultIndex={[]}>
                    {/* Waluty */}
                    <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" mb={3} overflow="hidden">
                      <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                        <HStack flex={1} spacing={2}>
                          <Text fontSize="lg" lineHeight={1}>💰</Text>
                          <Text fontSize="md" fontWeight="bold">Waluty</Text>
                          <Badge colorScheme="primary">{currencies.length}</Badge>
                        </HStack>
                        <AccordionIcon />
                      </AccordionButton>
                      <AccordionPanel bg="white" pb={4} px={6}>
                      <HStack justify="flex-end" mb={4}>
                        <Button
                          size="sm"
                          leftIcon={<Plus size={16} />}
                          onClick={() => {
                            const newOrder = currencies.length;
                            setCurrencies([...currencies, {
                              code: 'PLN',
                              name: 'Polski Złoty',
                              symbol: 'zł',
                              isDefault: currencies.length === 0,
                              order: newOrder,
                            }]);
                          }}
                        >
                          Dodaj walutę
                        </Button>
                      </HStack>

                      {currencies.length === 0 ? (
                        <Box p={4} bg="gray.50" borderRadius="md" textAlign="center">
                          <Text color="gray.600">Brak walut. Dodaj walutę używając przycisku powyżej.</Text>
                        </Box>
                      ) : (
                        <VStack spacing={3} align="stretch">
                          {currencies.map((curr, index) => (
                            <Box key={index} p={4} bg="gray.50" borderRadius="md" borderWidth="1px">
                              <HStack spacing={3} align="start">
                                <VStack flex={1} spacing={3}>
                                  <HStack w="full" spacing={3}>
                                    <FormControl flex={1}>
                                      <FormLabel fontSize="sm">Kod *</FormLabel>
                                      <Input
                                        size="sm"
                                        value={curr.code}
                                        onChange={(e) => {
                                          const updated = [...currencies];
                                          updated[index].code = e.target.value;
                                          setCurrencies(updated);
                                        }}
                                        placeholder="PLN"
                                        maxLength={10}
                                      />
                                    </FormControl>
                                    <FormControl flex={1}>
                                      <FormLabel fontSize="sm">Nazwa *</FormLabel>
                                      <Input
                                        size="sm"
                                        value={curr.name}
                                        onChange={(e) => {
                                          const updated = [...currencies];
                                          updated[index].name = e.target.value;
                                          setCurrencies(updated);
                                        }}
                                        placeholder="Polski złoty"
                                      />
                                    </FormControl>
                                    <FormControl flex={1}>
                                      <FormLabel fontSize="sm">Symbol</FormLabel>
                                      <Input
                                        size="sm"
                                        value={curr.symbol || ''}
                                        onChange={(e) => {
                                          const updated = [...currencies];
                                          updated[index].symbol = e.target.value;
                                          setCurrencies(updated);
                                        }}
                                        placeholder="zł"
                                        maxLength={5}
                                      />
                                    </FormControl>
                                  </HStack>
                                  <Checkbox
                                    isChecked={curr.isDefault}
                                    onChange={(e) => {
                                      const updated = currencies.map((c, i) => ({
                                        ...c,
                                        isDefault: i === index && e.target.checked,
                                      }));
                                      setCurrencies(updated);
                                    }}
                                  >
                                    <Text fontSize="sm">Domyślna waluta</Text>
                                  </Checkbox>
                                </VStack>
                                <IconButton
                                  aria-label="Usuń walutę"
                                  icon={<Trash2 size={16} />}
                                  size="sm"
                                  colorScheme="red"
                                  variant="ghost"
                                  onClick={() => {
                                    setCurrencies(currencies.filter((_, i) => i !== index));
                                  }}
                                />
                              </HStack>
                            </Box>
                          ))}
                        </VStack>
                      )}
                      </AccordionPanel>
                    </AccordionItem>

                    {/* Jednostki */}
                    <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" mb={3} overflow="hidden">
                      <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                        <HStack flex={1} spacing={2}>
                          <Text fontSize="lg" lineHeight={1}>📏</Text>
                          <Text fontSize="md" fontWeight="bold">Jednostki miar</Text>
                          <Badge colorScheme="green">{units.length}</Badge>
                        </HStack>
                        <AccordionIcon />
                      </AccordionButton>
                      <AccordionPanel bg="white" pb={4} px={6}>
                      <HStack justify="flex-end" mb={4}>
                        <Button
                          size="sm"
                          leftIcon={<Plus size={16} />}
                          onClick={() => {
                            const newOrder = units.length;
                            setUnits([...units, {
                              code: '',
                              name: '',
                              symbol: '',
                              category: '',
                              isDefault: units.length === 0,
                              order: newOrder,
                            }]);
                          }}
                        >
                          Dodaj jednostkę
                        </Button>
                      </HStack>

                      {units.length === 0 ? (
                        <Box p={4} bg="gray.50" borderRadius="md" textAlign="center">
                          <Text color="gray.600">Brak jednostek. Dodaj jednostkę używając przycisku powyżej.</Text>
                        </Box>
                      ) : (
                        <VStack spacing={3} align="stretch">
                          {units.map((unit, index) => (
                            <Box key={index} p={4} bg="gray.50" borderRadius="md" borderWidth="1px">
                              <HStack spacing={3} align="start">
                                <VStack flex={1} spacing={3}>
                                  <HStack w="full" spacing={3}>
                                    <FormControl flex={1}>
                                      <FormLabel fontSize="sm">Kod *</FormLabel>
                                      <Input
                                        size="sm"
                                        value={unit.code}
                                        onChange={(e) => {
                                          const updated = [...units];
                                          updated[index].code = e.target.value;
                                          setUnits(updated);
                                        }}
                                        placeholder="m2"
                                        maxLength={10}
                                      />
                                    </FormControl>
                                    <FormControl flex={1}>
                                      <FormLabel fontSize="sm">Nazwa *</FormLabel>
                                      <Input
                                        size="sm"
                                        value={unit.name}
                                        onChange={(e) => {
                                          const updated = [...units];
                                          updated[index].name = e.target.value;
                                          setUnits(updated);
                                        }}
                                        placeholder="Metr kwadratowy"
                                      />
                                    </FormControl>
                                    <FormControl flex={1}>
                                      <FormLabel fontSize="sm">Symbol *</FormLabel>
                                      <Input
                                        size="sm"
                                        value={unit.symbol}
                                        onChange={(e) => {
                                          const updated = [...units];
                                          updated[index].symbol = e.target.value;
                                          setUnits(updated);
                                        }}
                                        placeholder="m²"
                                        maxLength={5}
                                      />
                                    </FormControl>
                                  </HStack>
                                  <HStack w="full" spacing={3}>
                                    <FormControl flex={1}>
                                      <FormLabel fontSize="sm">Kategoria</FormLabel>
                                      <Input
                                        size="sm"
                                        value={unit.category || ''}
                                        onChange={(e) => {
                                          const updated = [...units];
                                          updated[index].category = e.target.value;
                                          setUnits(updated);
                                        }}
                                        placeholder="Powierzchnia, długość, objętość..."
                                      />
                                    </FormControl>
                                    <FormControl>
                                      <FormLabel fontSize="sm" opacity={0}>_</FormLabel>
                                      <Checkbox
                                        isChecked={unit.isDefault}
                                        onChange={(e) => {
                                          const updated = units.map((u, i) => ({
                                            ...u,
                                            isDefault: i === index && e.target.checked,
                                          }));
                                          setUnits(updated);
                                        }}
                                      >
                                        <Text fontSize="sm">Domyślna jednostka</Text>
                                      </Checkbox>
                                    </FormControl>
                                  </HStack>
                                </VStack>
                                <IconButton
                                  aria-label="Usuń jednostkę"
                                  icon={<Trash2 size={16} />}
                                  size="sm"
                                  colorScheme="red"
                                  variant="ghost"
                                  onClick={() => {
                                    setUnits(units.filter((_, i) => i !== index));
                                  }}
                                />
                              </HStack>
                            </Box>
                          ))}
                        </VStack>
                      )}
                      </AccordionPanel>
                    </AccordionItem>

                    {/* Kategorie */}
                    <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" mb={3} overflow="hidden">
                      <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                        <HStack flex={1} spacing={2}>
                          <Text fontSize="lg" lineHeight={1}>🏷️</Text>
                          <Text fontSize="md" fontWeight="bold">Kategorie</Text>
                          <Badge colorScheme="level2">{categories.length}</Badge>
                        </HStack>
                        <AccordionIcon />
                      </AccordionButton>
                      <AccordionPanel bg="white" pb={4} px={6}>
                      <HStack justify="flex-end" mb={4}>
                        <Button
                          size="sm"
                          leftIcon={<Plus size={16} />}
                          onClick={() => {
                            setCategories([...categories, {
                              name: '',
                              symbol: null,
                              order: categories.length,
                            }]);
                          }}
                        >
                          Dodaj kategorię
                        </Button>
                      </HStack>

                      {categories.length === 0 ? (
                        <Box p={4} bg="gray.50" borderRadius="md" textAlign="center">
                          <Text color="gray.600">Brak kategorii. Dodaj kategorię używając przycisku powyżej.</Text>
                        </Box>
                      ) : (
                        <VStack spacing={3} align="stretch">
                          {categories.map((cat, index) => (
                            <Box key={index} p={4} bg="gray.50" borderRadius="md" borderWidth="1px">
                              <HStack spacing={3} align="start">
                                <HStack flex={1} spacing={3}>
                                  <FormControl flex={2}>
                                    <FormLabel fontSize="sm">Nazwa *</FormLabel>
                                    <Input
                                      size="sm"
                                      value={cat.name}
                                      onChange={(e) => {
                                        const updated = [...categories];
                                        updated[index] = { ...updated[index], name: e.target.value };
                                        setCategories(updated);
                                      }}
                                      placeholder="Robocizna"
                                    />
                                  </FormControl>
                                  <FormControl flex={1}>
                                    <FormLabel fontSize="sm">Symbol</FormLabel>
                                    <Input
                                      size="sm"
                                      value={cat.symbol ?? ''}
                                      onChange={(e) => {
                                        const updated = [...categories];
                                        updated[index] = { ...updated[index], symbol: e.target.value || null };
                                        setCategories(updated);
                                      }}
                                      placeholder="R"
                                      maxLength={10}
                                    />
                                  </FormControl>
                                </HStack>
                                <IconButton
                                  aria-label="Usuń kategorię"
                                  icon={<Trash2 size={16} />}
                                  size="sm"
                                  colorScheme="red"
                                  variant="ghost"
                                  mt={6}
                                  onClick={() => {
                                    setCategories(categories.filter((_, i) => i !== index));
                                  }}
                                />
                              </HStack>
                            </Box>
                          ))}
                        </VStack>
                      )}
                      </AccordionPanel>
                    </AccordionItem>
                  </Accordion>
                </TabPanel>

                <TabPanel>
                  {renderFieldLayoutTab()}
                </TabPanel>
            </TabPanels>
          </Tabs>

        {/* Back button – desktop inline, mobile hidden (sticky bar has it) */}
        <Box display={{ base: "none", md: "block" }} pt={2}>
          <Button
            leftIcon={<ArrowLeft size={18} />}
            variant="ghost"
            onClick={() => safeNavigate("/cost-estimate-templates")}
          >
            Powrót
          </Button>
        </Box>
        </VStack>
      </Box>

      {/* Sticky save bar */}
      <Box
        position="sticky"
        bottom={0}
        bg="white"
        borderTop="1px solid"
        borderColor="gray.200"
        px={{ base: 4, md: 8 }}
        py={3}
        zIndex={10}
        shadow="0 -2px 8px rgba(0,0,0,0.06)"
      >
        <HStack justify="space-between" maxW="1400px" mx="auto" spacing={3}>
          <Button
            leftIcon={<ArrowLeft size={18} />}
            variant="ghost"
            onClick={() => safeNavigate("/cost-estimate-templates")}
            display={{ base: "flex", md: "flex" }}
            size={{ base: "sm", md: "md" }}
          >
            Powrót
          </Button>
          <Button
            leftIcon={<Save size={18} />}
            colorScheme="primary"
            onClick={handleSubmitClick}
            isLoading={isSubmitting}
            loadingText="Zapisywanie..."
            w={{ base: "full", md: "auto" }}
            size={{ base: "md", md: "md" }}
          >
            {templateId ? "Zapisz zmiany" : "Utwórz szablon"}
          </Button>
        </HStack>
      </Box>

      {/* Preview Modal */}
      <Modal 
        isOpen={isPreviewOpen} 
        onClose={onPreviewClose} 
        size="full" 
        scrollBehavior="inside"
        closeOnOverlayClick={false}
      >
        <ModalOverlay />
        <ModalContent maxH="100vh" m={0}>
          <ModalHeader borderBottom="1px" borderColor="gray.200">
            <HStack spacing={3}>
              <Eye size={24} />
              <Text>Podgląd szablonu - Przykładowy kosztorys</Text>
            </HStack>
          </ModalHeader>
          <ModalBody p={6} bg="gray.50">
            {previewData && (
              <Box maxW="1600px" mx="auto">
                <VStack spacing={4} align="stretch" mb={4}>
                  <Box bg="primary.50" p={4} borderRadius="md" borderWidth="1px" borderColor="primary.200">
                    <HStack spacing={2}>
                      <AlertCircle size={20} color="primary" />
                      <Text fontSize="sm" color="primary.700">
                        To jest podgląd szablonu z przykładowymi danymi. Dane są generowane automatycznie aby pokazać jak będzie wyglądał kosztorys stworzony na podstawie tego szablonu.
                      </Text>
                    </HStack>
                  </Box>
                  <Box bg="white" p={4} borderRadius="md" borderWidth="1px">
                    <VStack align="start" spacing={2}>
                      <HStack spacing={3}>
                        <Text fontWeight="bold" fontSize="lg">Szablon:</Text>
                        <Text fontSize="lg">{templateName || "Nowy szablon"}</Text>
                      </HStack>
                      {templateDescription && (
                        <Text fontSize="sm" color="gray.600">{templateDescription}</Text>
                      )}
                    </VStack>
                  </Box>
                </VStack>
                <CostEstimateTableView
                  details={previewData}
                  editable={false}
                  onDataChange={() => {}} // Podgląd - brak edycji
                />
              </Box>
            )}
          </ModalBody>
          <ModalFooter borderTop="1px" borderColor="gray.200">
            <Button onClick={onPreviewClose}>Zamknij</Button>
          </ModalFooter>
        </ModalContent>
      </Modal>

      {/* Modal ostrzeżenia o niezapisanych zmianach */}
      <AlertDialog
        isOpen={isUnsavedOpen}
        leastDestructiveRef={unsavedCancelRef}
        onClose={handleCancelLeave}
        isCentered
      >
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader fontSize="lg" fontWeight="bold">
              <HStack spacing={2}>
                <AlertCircle size={24} color="orange" />
                <Text>Niezapisane zmiany</Text>
              </HStack>
            </AlertDialogHeader>

            <AlertDialogBody>
              <Text>
                Masz niezapisane zmiany w szablonie kosztorysu. Czy na pewno chcesz opuścić tę stronę?
              </Text>
              <Text mt={2} color="gray.600" fontSize="sm">
                Wszystkie niezapisane zmiany zostaną utracone.
              </Text>
            </AlertDialogBody>

            <AlertDialogFooter>
              <Button ref={unsavedCancelRef} onClick={handleCancelLeave}>
                Zostań na stronie
              </Button>
              <Button colorScheme="red" onClick={handleConfirmLeave} ml={3}>
                Opuść bez zapisywania
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
  </MainLayout>
  );
}

// ======================== SUB-COMPONENTS ========================

interface HeaderFieldsEditorProps {
  headerFields: GroupHeaderFieldDefinition[];
  onAdd: (type: GroupHeaderFieldType) => void;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<GroupHeaderFieldDefinition>) => void;
  onReorder: (reorderedFields: GroupHeaderFieldDefinition[]) => void;
  fieldTypeConfigs: Record<string, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>;
}

function HeaderFieldsEditor({ headerFields, onAdd, onRemove, onUpdate, onReorder, fieldTypeConfigs }: HeaderFieldsEditorProps) {
  // Pobierz dostępne typy pól nagłówka z BE (scope 4 = group header)
  const availableHeaderFields = fieldTypeConfigs[4] || [];
  
  return (
    <VStack spacing={4} align="stretch">
      <Box
        bg="primary.50"
        p={4}
        borderRadius="md"
        borderLeft="3px solid"
        borderLeftColor="primary.400"
      >
        <HStack spacing={2} mb={3}>
          <Text fontSize="sm" fontWeight="bold" color="primary.700">
            Dodaj pole nagłówka etapu:
          </Text>
        </HStack>
        {availableHeaderFields.length > 0 && availableHeaderFields.every(config => headerFields.some(f => f.type === (config.fieldType as GroupHeaderFieldType))) ? (
          <Text fontSize="sm" color="primary.600" fontStyle="italic">
            Wszystkie pola nagłówka zostały dodane
          </Text>
        ) : (
          <HStack spacing={2} flexWrap="wrap">
            {availableHeaderFields.length > 0 ? (
              availableHeaderFields.map((config) => {
                // FieldType z BE odpowiada GroupHeaderFieldType bezpośrednio (0-9)
                const typeNum = config.fieldType as GroupHeaderFieldType;
                const isAdded = headerFields.some((f) => f.type === typeNum);
                return (
                  <Button
                    key={config.fieldType}
                    size="sm"
                    leftIcon={<Plus size={14} />}
                    colorScheme="blue"
                    variant="outline"
                    onClick={() => onAdd(typeNum)}
                    isDisabled={isAdded}
                    opacity={isAdded ? 0.4 : 1}
                  >
                    {config.namePl}
                  </Button>
                );
              })
            ) : (
              // Fallback jeśli nie załadowano konfiguracji
              Object.entries(groupHeaderFieldTypeLabels).map(([type, label]) => {
                const typeNum = parseInt(type) as GroupHeaderFieldType;
                const isAdded = headerFields.some((f) => f.type === typeNum);
                return (
                  <Button
                    key={type}
                    size="sm"
                    leftIcon={<Plus size={14} />}
                    colorScheme="blue"
                    variant="outline"
                    onClick={() => onAdd(typeNum)}
                    isDisabled={isAdded}
                    opacity={isAdded ? 0.4 : 1}
                  >
                    {label}
                  </Button>
                );
              })
            )}
          </HStack>
        )}
      </Box>

      {headerFields.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak pól w nagłówku</Text>
        </Box>
      ) : (
        <Box overflowX="auto">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>
                  <Tooltip label="Typ pola nagłówka etapu" hasArrow>
                    <span>Typ pola</span>
                  </Tooltip>
                </Th>
                <Th>Etykieta</Th>
                <Th w="80px">
                  <Tooltip label="Czy kolumna jest widoczna w widoku kosztorysu" hasArrow>
                    <span>Widoczne</span>
                  </Tooltip>
                </Th>
                <Th w="80px">
                  <Tooltip label="Czy użytkownik może sortować po tej kolumnie" hasArrow>
                    <span>Sortowalne</span>
                  </Tooltip>
                </Th>
                <Th w="80px">
                  <Tooltip label="Czy użytkownik może filtrować po tej kolumnie" hasArrow>
                    <span>Filtrowalne</span>
                  </Tooltip>
                </Th>
                <Th w="100px">
                  <Tooltip label="Kolumna nie może być edytowana przez użytkownika" hasArrow>
                    <span>Tylko do odczytu</span>
                  </Tooltip>
                </Th>
                <Th w="80px">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {headerFields.map((field, index) => {
                const defaultLabel = field.fieldTypeConfig?.namePl || groupHeaderFieldTypeLabels[field.type];
                const displayLabel = field.customLabel || defaultLabel;
                return (
                  <Tr key={index}>
                    <Td>
                      <Badge colorScheme="blue">
                        {field.fieldTypeConfig?.namePl || groupHeaderFieldTypeLabels[field.type]}
                      </Badge>
                    </Td>
                    <Td>
                      <Input
                        size="sm"
                        value={displayLabel}
                        onChange={(e) => onUpdate(index, { customLabel: e.target.value })}
                        placeholder={defaultLabel}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.visible !== false}
                        onChange={(e) => onUpdate(index, { visible: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.sortable || false}
                        onChange={(e) => onUpdate(index, { sortable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.filterable || false}
                        onChange={(e) => onUpdate(index, { filterable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.readOnly || false}
                        onChange={(e) => onUpdate(index, { readOnly: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Tooltip label="Usuń pole" hasArrow>
                        <IconButton
                          aria-label="Usuń"
                          icon={<Trash2 size={16} />}
                          size="sm"
                          color="red.400"
                          variant="ghost"
                          _hover={{ color: "red.600", bg: "red.50" }}
                          onClick={() => onRemove(index)}
                        />
                      </Tooltip>
                    </Td>
                  </Tr>
                );
              })}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
}


// ===== SYSTEM FIELDS EDITOR =====

interface SystemFieldsEditorProps {
  fields: SystemFieldDefinition[];
  onAdd: (type: SystemFieldType) => void;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<SystemFieldDefinition>) => void;
  fieldTypeConfigs: Record<string, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>;
}

function SystemFieldsEditor({
  fields,
  onAdd,
  onRemove,
  onUpdate,
  fieldTypeConfigs,
}: SystemFieldsEditorProps) {
  // Pobierz dostępne typy pól z BE konfiguracji
  const availableSystemFields = fieldTypeConfigs[1] || [];
  const availableCalculatedFields = fieldTypeConfigs[2] || [];
  const availableGenericFields = fieldTypeConfigs[3] || [];

  // Obsługa przeciągania child fields na urządzeniach dotykowych
  const { createTouchHandlers: createChildTouchHandlers } = useTouchReorder({ itemSelector: '[data-touch-draggable-child]' });
  
  // Fallback labels jeśli nie ma konfiguracji
  const systemFieldTypeLabels: Record<SystemFieldType, string> = {
    [SystemFieldType.Name]: "Nazwa pozycji",
    [SystemFieldType.Quantity]: "Ilość",
    [SystemFieldType.Unit]: "Jednostka miary",
    [SystemFieldType.Options]: "Opcje",
    [SystemFieldType.Selected]: "Zaznaczenie",
  };

  const [expandedField, setExpandedField] = useState<number | null>(null);
  const [draggedChildIndex, setDraggedChildIndex] = useState<number | null>(null);

  // Handler dla zmiany kolejności child fields w opcjach
  const handleChildDragStart = (childIndex: number) => {
    setDraggedChildIndex(childIndex);
  };

  const handleChildDragOver = (e: React.DragEvent, parentIndex: number, targetIndex: number) => {
    e.preventDefault();
    if (draggedChildIndex === null || draggedChildIndex === targetIndex) return;

    const parentField = fields[parentIndex];
    if (!parentField.childFields) return;

    const reordered = [...parentField.childFields];
    const [moved] = reordered.splice(draggedChildIndex, 1);
    reordered.splice(targetIndex, 0, moved);

    // Zaktualizuj order na każdym polu
    const withOrder = reordered.map((f, i) => ({ ...f, order: i }));
    onUpdate(parentIndex, { childFields: withOrder });
    setDraggedChildIndex(targetIndex);
  };

  const handleChildDragEnd = () => {
    setDraggedChildIndex(null);
  };

  // Handler dla dodawania child fields
  const handleAddChildSystemField = (parentIndex: number, type: SystemFieldType) => {
    const parentField = fields[parentIndex];
    if (!parentField.childFields) {
      parentField.childFields = [];
    }
    
    // Pobierz konfigurację z BE (FieldType = SystemFieldType + 100)
    const fieldTypeForConfig = type + 100;
    const config = availableSystemFields.find(c => c.fieldType === fieldTypeForConfig);
    const label = config?.namePl || systemFieldTypeLabels[type];
    
    const newChild: SystemFieldDefinition = {
      name: crypto.randomUUID(),
      label: label,
      type: type,
      order: parentField.childFields.length,
      required: type === SystemFieldType.Name,
      visible: true,
      sortable: true,
      filterable: true,
      readOnly: false,
      fieldTypeConfig: config, // Zachowaj config z BE
    };

    onUpdate(parentIndex, {
      childFields: [...parentField.childFields, newChild],
    });
  };

  const handleAddChildCalculatedField = (parentIndex: number, type: CalculatedFieldType) => {
    const parentField = fields[parentIndex];
    if (!parentField.childFields) {
      parentField.childFields = [];
    }

    // Pobierz konfigurację z BE (FieldType = CalculatedFieldType + 200)
    const fieldTypeForConfig = type + 200;
    const config = availableCalculatedFields.find(c => c.fieldType === fieldTypeForConfig);
    const label = config?.namePl || calculatedFieldTypeLabels[type] || `Pole kalkulowane ${type}`;

    const isSummable = type === CalculatedFieldType.ValueNet ||
                       type === CalculatedFieldType.ValueGross ||
                       type === CalculatedFieldType.TotalVat;
    const isAutoCalculated = [
      CalculatedFieldType.UnitPriceGross,
      CalculatedFieldType.ValueNet,
      CalculatedFieldType.ValueGross,
      CalculatedFieldType.UnitVat,
      CalculatedFieldType.TotalVat,
    ].includes(type);

    const newChild: CalculatedFieldDefinition = {
      name: crypto.randomUUID(),
      label: label,
      type: type,
      order: parentField.childFields.length,
      required: false,
      visible: true,
      sortable: true,
      filterable: true,
      summable: isSummable,
      summaryScope: SummaryScope.Both,
      autoCalculated: isAutoCalculated,
      readOnly: isAutoCalculated,
      fieldTypeConfig: config, // Zachowaj config z BE
    };

    onUpdate(parentIndex, {
      childFields: [...parentField.childFields, newChild],
    });
  };

  const handleAddChildGenericField = (parentIndex: number, type: GenericFieldType) => {
    const parentField = fields[parentIndex];
    if (!parentField.childFields) {
      parentField.childFields = [];
    }

    // Pobierz konfigurację z BE (FieldType = GenericFieldType + 300)
    const fieldTypeForConfig = type + 300;
    const config = availableGenericFields.find(c => c.fieldType === fieldTypeForConfig);
    const label = config?.namePl || genericFieldTypeLabels[type] || `Pole generyczne ${type}`;

    const newChild: GenericFieldDefinition = {
      name: crypto.randomUUID(),
      label: label,
      type: type,
      order: parentField.childFields.length,
      required: false,
      visible: true,
      sortable: true,
      filterable: true,
      readOnly: false,
      fieldTypeConfig: config, // Zachowaj config z BE
    };

    onUpdate(parentIndex, {
      childFields: [...parentField.childFields, newChild],
    });
  };

  const handleRemoveChildField = (parentIndex: number, childIndex: number) => {
    const parentField = fields[parentIndex];
    if (!parentField.childFields) return;

    const updatedChildren = parentField.childFields.filter((_, i) => i !== childIndex);
    onUpdate(parentIndex, { childFields: updatedChildren });
  };

  const handleUpdateChildField = (
    parentIndex: number,
    childIndex: number,
    updates: any
  ) => {
    const parentField = fields[parentIndex];
    if (!parentField.childFields) return;

    const updatedChildren = [...parentField.childFields];
    updatedChildren[childIndex] = { ...updatedChildren[childIndex], ...updates };
    onUpdate(parentIndex, { childFields: updatedChildren });
  };

  const isSystemField = (field: any): field is SystemFieldDefinition => {
    return 'type' in field && typeof field.type === 'number' && field.type >= 0 && field.type <= 4;
  };

  const isCalculatedField = (field: any): field is CalculatedFieldDefinition => {
    return 'type' in field && 'summable' in field;
  };

  const isGenericField = (field: any): field is GenericFieldDefinition => {
    return 'type' in field && !('summable' in field) && !isSystemField(field);
  };

  return (
    <VStack spacing={4} align="stretch">
      <Box
        bg="primary.50"
        p={4}
        borderRadius="md"
        borderLeft="3px solid"
        borderLeftColor="primary.400"
      >
        {availableSystemFields.length > 0 && availableSystemFields.every(config => {
          const t = (config.fieldType - 100) as SystemFieldType;
          return fields.some(f => f.type === t);
        }) ? (
          <Text fontSize="sm" color="primary.600" fontStyle="italic">
            Wszystkie pola systemowe zostały dodane
          </Text>
        ) : (
          <>
            <HStack spacing={2} mb={3}>
              <Text fontSize="sm" fontWeight="bold" color="primary.700">
                Dodaj pole systemowe:
              </Text>
            </HStack>
            <HStack spacing={2} flexWrap="wrap">
              {availableSystemFields.length > 0 ? (
                availableSystemFields.map((config) => {
                  // FieldType 100-199 to pola systemowe, konwertuj na SystemFieldType (0-99)
                  const systemFieldType = (config.fieldType - 100) as SystemFieldType;
                  const isAdded = fields.some((f) => f.type === systemFieldType);
                  // Każde pole systemowe można dodać tylko raz
                  const shouldDisable = isAdded;
                  return (
                    <Button
                      key={config.fieldType}
                      size="sm"
                      leftIcon={<Plus size={14} />}
                      colorScheme="blue"
                      variant="outline"
                      onClick={() => onAdd(systemFieldType)}
                      isDisabled={shouldDisable}
                      opacity={shouldDisable ? 0.4 : 1}
                    >
                      {config.namePl}
                    </Button>
                  );
                })
              ) : (
                // Fallback jeśli nie załadowano konfiguracji z BE
                Object.entries(systemFieldTypeLabels).map(([type, label]) => {
                  const typeNum = parseInt(type) as SystemFieldType;
                  const isAdded = fields.some((f) => f.type === typeNum);
                  const shouldDisable = isAdded;
                  return (
                    <Button
                      key={type}
                      size="sm"
                      leftIcon={<Plus size={14} />}
                      colorScheme="blue"
                      variant="outline"
                      onClick={() => onAdd(typeNum)}
                      isDisabled={shouldDisable}
                      opacity={shouldDisable ? 0.4 : 1}
                    >
                      {label}
                    </Button>
                  );
                })
              )}
            </HStack>
          </>
        )}
      </Box>

      {fields.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak pól systemowych</Text>
        </Box>
      ) : (
        <Box overflowX="auto">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>
                  <Tooltip label="Typ pola systemowego" hasArrow><span>Typ pola</span></Tooltip>
                </Th>
                <Th>Etykieta</Th>
                <Th w="80px">
                  <Tooltip label="Czy kolumna jest widoczna w widoku kosztorysu" hasArrow><span>Widoczne</span></Tooltip>
                </Th>
                <Th w="80px">
                  <Tooltip label="Czy użytkownik może sortować po tej kolumnie" hasArrow><span>Sortowalne</span></Tooltip>
                </Th>
                <Th w="80px">
                  <Tooltip label="Czy użytkownik może filtrować po tej kolumnie" hasArrow><span>Filtrowalne</span></Tooltip>
                </Th>
                <Th w="100px">
                  <Tooltip label="Kolumna nie może być edytowana przez użytkownika" hasArrow><span>Tylko do odczytu</span></Tooltip>
                </Th>
                <Th w="120px">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {fields.map((field, index) => (
                <React.Fragment key={index}>
                  <Tr>
                    <Td>
                      <Badge colorScheme="blue">
                        {field.fieldTypeConfig?.namePl || systemFieldTypeLabels[field.type]}
                      </Badge>
                    </Td>
                    <Td>
                      <Input
                        size="sm"
                        value={field.label}
                        onChange={(e) => onUpdate(index, { label: e.target.value })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.visible !== false}
                        onChange={(e) => onUpdate(index, { visible: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.sortable}
                        onChange={(e) => onUpdate(index, { sortable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.filterable}
                        onChange={(e) => onUpdate(index, { filterable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.readOnly || false}
                        onChange={(e) => onUpdate(index, { readOnly: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <HStack spacing={1}>
                        {field.type === SystemFieldType.Options && (
                          <IconButton
                            aria-label="Pola w opcjach"
                            icon={expandedField === index ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                            size="sm"
                            variant="ghost"
                            onClick={() => setExpandedField(expandedField === index ? null : index)}
                          />
                        )}
                        <Tooltip label="Usuń pole" hasArrow>
                          <IconButton
                            aria-label="Usuń"
                            icon={<Trash2 size={16} />}
                            size="sm"
                            color="red.400"
                            variant="ghost"
                            _hover={{ color: "red.600", bg: "red.50" }}
                            onClick={() => onRemove(index)}
                          />
                        </Tooltip>
                      </HStack>
                    </Td>
                  </Tr>

              {/* Child Fields Editor - tylko dla pola Options */}
              {field.type === SystemFieldType.Options && expandedField === index && (
                <Tr>
                  <Td colSpan={7} p={0}>
                    <Box p={4} bg="gray.50">
                      <VStack spacing={4} align="stretch">
                        <Text fontSize="sm" fontWeight="bold" color="gray.700">
                          Pola w opcjach (systemowe bez Opcji, kalkulowane, generyczne)
                        </Text>

                    {/* Dodawanie pól systemowych bez Options */}
                    <Box bg="primary.50" p={3} borderRadius="md">
                      <Text fontSize="xs" fontWeight="bold" mb={2}>
                        Pola systemowe:
                      </Text>
                      <HStack spacing={2} flexWrap="wrap">
                        {availableSystemFields
                          .filter((config) => {
                            // Dla child fields w Options - wszystkie pola systemowe POZA Options
                            const systemFieldType = (config.fieldType - 100) as SystemFieldType;
                            return systemFieldType !== SystemFieldType.Options;
                          })
                          .map((config) => {
                            const systemFieldType = (config.fieldType - 100) as SystemFieldType;
                            const isAdded = field.childFields?.some(
                              (c) => isSystemField(c) && c.type === systemFieldType
                            );
                            return (
                              <Button
                                key={config.fieldType}
                                size="xs"
                                leftIcon={<Plus size={12} />}
                                colorScheme="primary"
                                variant={isAdded ? "solid" : "outline"}
                                onClick={() => handleAddChildSystemField(index, systemFieldType)}
                                isDisabled={isAdded && systemFieldType !== SystemFieldType.Selected}
                              >
                                {config.namePl}
                              </Button>
                            );
                          })}
                      </HStack>
                    </Box>

                    {/* Dodawanie pól kalkulowanych */}
                    <Box bg="level2.50" p={3} borderRadius="md">
                      <Text fontSize="xs" fontWeight="bold" mb={2}>
                        Pola kalkulowane:
                      </Text>
                      <HStack spacing={2} flexWrap="wrap">
                        {(fieldTypeConfigs[2] || []).map((config) => {
                          const calcFieldType = (config.fieldType - 200) as CalculatedFieldType;
                          const isAdded = field.childFields?.some(
                            (c) => isCalculatedField(c) && c.type === calcFieldType
                          );
                          return (
                            <Button
                              key={config.fieldType}
                              size="xs"
                              leftIcon={<Plus size={12} />}
                              colorScheme="level2"
                              variant={isAdded ? "solid" : "outline"}
                              onClick={() => handleAddChildCalculatedField(index, calcFieldType)}
                              isDisabled={isAdded}
                            >
                              {config.namePl}
                            </Button>
                          );
                        })}
                      </HStack>
                    </Box>

                    {/* Dodawanie pól generycznych */}
                    <Box bg="green.50" p={3} borderRadius="md">
                      <Text fontSize="xs" fontWeight="bold" mb={2}>
                        Pola generyczne:
                      </Text>
                      <HStack spacing={2} flexWrap="wrap">
                        {(fieldTypeConfigs[3] || []).map((config) => {
                          const genFieldType = (config.fieldType - 300) as GenericFieldType;
                          return (
                            <Button
                              key={config.fieldType}
                              size="xs"
                              leftIcon={<Plus size={12} />}
                              colorScheme="green"
                              variant="outline"
                              onClick={() => handleAddChildGenericField(index, genFieldType)}
                            >
                              {config.namePl}
                            </Button>
                          );
                        })}
                      </HStack>
                    </Box>

                    {/* Lista child fields - tabela */}
                    {field.childFields && field.childFields.length > 0 && (
                      <Table size="sm" variant="simple" bg="white" borderRadius="md">
                        <Thead>
                          <Tr>
                            <Th fontSize="xs" w="40px"></Th>
                            <Th fontSize="xs">Typ pola</Th>
                            <Th fontSize="xs">Etykieta</Th>
                            <Th fontSize="xs" w="80px" textAlign="center">Sortowalne</Th>
                            <Th fontSize="xs" w="80px" textAlign="center">Filtrowalne</Th>
                            <Th fontSize="xs" w="100px" textAlign="center">Tylko do odczytu</Th>
                            <Th fontSize="xs" w="60px">Akcje</Th>
                          </Tr>
                        </Thead>
                        <Tbody>
                          {[...field.childFields]
                            .map((cf, origIdx) => ({ cf, origIdx }))
                            .sort((a, b) => (a.cf.order ?? a.origIdx) - (b.cf.order ?? b.origIdx))
                            .map(({ cf: childField, origIdx: childIndex }, sortedIdx) => {
                            let colorScheme = "gray";
                            let typeLabel = "";
                            
                            // Użyj fieldTypeConfig.namePl jeśli dostępne, w przeciwnym razie fallback na słowniki
                            if (childField.fieldTypeConfig?.namePl) {
                              typeLabel = childField.fieldTypeConfig.namePl;
                              // Ustal colorScheme na podstawie fieldScope
                              const scope = childField.fieldTypeConfig.fieldScope;
                              if (scope === 1) colorScheme = "cyan";
                              else if (scope === 2) colorScheme = "purple";
                              else if (scope === 3) colorScheme = "green";
                            } else if (isSystemField(childField)) {
                              colorScheme = "cyan";
                              typeLabel = systemFieldTypeLabels[childField.type];
                            } else if (isCalculatedField(childField)) {
                              colorScheme = "purple";
                              typeLabel = calculatedFieldTypeLabels[childField.type];
                            } else if (isGenericField(childField)) {
                              colorScheme = "green";
                              typeLabel = genericFieldTypeLabels[childField.type];
                            }

                            return (
                              <Tr
                                key={childIndex}
                                draggable
                                onDragStart={() => handleChildDragStart(sortedIdx)}
                                onDragOver={(e) => handleChildDragOver(e, index, sortedIdx)}
                                onDragEnd={handleChildDragEnd}
                                cursor="grab"
                                bg={draggedChildIndex === sortedIdx ? 'primary.50' : undefined}
                                _hover={{ bg: draggedChildIndex === sortedIdx ? 'primary.50' : 'gray.50' }}
                                _active={{ cursor: 'grabbing' }}
                                transition="all 0.15s"
                                {...createChildTouchHandlers(sortedIdx, draggedChildIndex, setDraggedChildIndex, (from, to) => {
                                  const parentField = fields[index];
                                  if (!parentField.childFields) return;
                                  const sorted = [...parentField.childFields]
                                    .map((cf, idx) => ({ cf, idx }))
                                    .sort((a, b) => (a.cf.order ?? a.idx) - (b.cf.order ?? b.idx));
                                  const [moved] = sorted.splice(from, 1);
                                  sorted.splice(to, 0, moved);
                                  const withOrder = sorted.map((s, i) => ({ ...s.cf, order: i }));
                                  onUpdate(index, { childFields: withOrder });
                                })}
                                data-touch-draggable-child
                              >
                                <Td w="40px" px={2}>
                                  <Icon as={GripVertical} color="gray.400" boxSize={4} />
                                </Td>
                                <Td>
                                  <Badge colorScheme={colorScheme} fontSize="xs">
                                    {typeLabel}
                                  </Badge>
                                </Td>
                                <Td>
                                  <Input
                                    size="xs"
                                    value={childField.label}
                                    onChange={(e) =>
                                      handleUpdateChildField(index, childIndex, {
                                        label: e.target.value,
                                      })
                                    }
                                  />
                                </Td>
                                <Td textAlign="center">
                                  <Checkbox
                                    size="sm"
                                    isChecked={childField.sortable ?? false}
                                    onChange={(e) =>
                                      handleUpdateChildField(index, childIndex, {
                                        sortable: e.target.checked,
                                      })
                                    }
                                  />
                                </Td>
                                <Td textAlign="center">
                                  <Checkbox
                                    size="sm"
                                    isChecked={childField.filterable ?? false}
                                    onChange={(e) =>
                                      handleUpdateChildField(index, childIndex, {
                                        filterable: e.target.checked,
                                      })
                                    }
                                  />
                                </Td>
                                <Td textAlign="center">
                                  <Checkbox
                                    size="sm"
                                    isChecked={childField.readOnly ?? false}
                                    onChange={(e) =>
                                      handleUpdateChildField(index, childIndex, {
                                        readOnly: e.target.checked,
                                      })
                                    }
                                  />
                                </Td>
                                <Td>
                                  <IconButton
                                    aria-label="Usuń"
                                    icon={<Trash2 size={14} />}
                                    size="xs"
                                    colorScheme="red"
                                    variant="ghost"
                                    onClick={() => handleRemoveChildField(index, childIndex)}
                                  />
                                </Td>
                              </Tr>
                            );
                          })}
                        </Tbody>
                      </Table>
                    )}
                  </VStack>
                </Box>
              </Td>
            </Tr>
              )}
                </React.Fragment>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
}


// ===== CALCULATED FIELDS EDITOR =====

interface CalculatedFieldsEditorProps {
  fields: CalculatedFieldDefinition[];
  onAdd: (type: CalculatedFieldType) => void;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<CalculatedFieldDefinition>) => void;
  fieldTypeConfigs: Record<string, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>;
  units: Array<{ code: string; name: string; symbol: string; }>;
}

function CalculatedFieldsEditor({
  fields,
  onAdd,
  onRemove,
  onUpdate,
  fieldTypeConfigs,
  units,
}: CalculatedFieldsEditorProps) {
  // Pobierz dostępne typy pól kalkulowanych z BE (scope 2 = calculated)
  const availableCalculatedFields = fieldTypeConfigs[2] || [];

  return (
    <VStack spacing={4} align="stretch">
      <Box
        bg="level1.50"
        p={4}
        borderRadius="md"
        borderLeft="3px solid"
        borderLeftColor="level1.500"
      >
        {availableCalculatedFields.length > 0 && availableCalculatedFields.every(config => {
          const t = (config.fieldType - 200) as CalculatedFieldType;
          return fields.some(f => f.type === t);
        }) ? (
          <Text fontSize="sm" color="level1.700" fontStyle="italic">
            Wszystkie pola obliczeniowe zostały dodane
          </Text>
        ) : (
          <>
            <HStack spacing={2} mb={3}>
              <Text fontSize="sm" fontWeight="bold" color="level1.700">
                Dodaj pole obliczeniowe (każde tylko raz):
              </Text>
            </HStack>
            <HStack spacing={2} flexWrap="wrap">
              {availableCalculatedFields.length > 0 ? (
                availableCalculatedFields.map((config) => {
                  // FieldType 200-299 to pola kalkulowane, konwertuj na CalculatedFieldType (0-99)
                  const calcFieldType = (config.fieldType - 200) as CalculatedFieldType;
                  const isAdded = fields.some((f) => f.type === calcFieldType);
                  return (
                    <Button
                      key={config.fieldType}
                      size="sm"
                      leftIcon={<Plus size={14} />}
                      colorScheme="green"
                      variant="outline"
                      onClick={() => onAdd(calcFieldType)}
                      isDisabled={isAdded}
                      opacity={isAdded ? 0.4 : 1}
                    >
                      {config.namePl}
                    </Button>
                  );
                })
              ) : (
                // Fallback jeśli nie załadowano konfiguracji
                Object.entries(calculatedFieldTypeLabels).map(([type, label]) => {
                  const typeNum = parseInt(type) as CalculatedFieldType;
                  const isAdded = fields.some((f) => f.type === typeNum);
                  return (
                    <Button
                      key={type}
                      size="sm"
                      leftIcon={<Plus size={14} />}
                      colorScheme="green"
                      variant="outline"
                      onClick={() => onAdd(typeNum)}
                      isDisabled={isAdded}
                      opacity={isAdded ? 0.4 : 1}
                    >
                      {label}
                    </Button>
                  );
                })
              )}
            </HStack>
          </>
        )}
      </Box>

      {fields.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak pól obliczeniowych</Text>
        </Box>
      ) : (
        <Box overflowX="auto">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>
                  <Tooltip label="Typ pola obliczeniowego" hasArrow><span>Typ pola</span></Tooltip>
                </Th>
                <Th>Etykieta</Th>
                <Th w="120px">Jednostka</Th>
                <Th w="80px">
                  <Tooltip label="Czy kolumna jest widoczna w widoku kosztorysu" hasArrow><span>Widoczne</span></Tooltip>
                </Th>
                <Th w="80px">
                  <Tooltip label="Czy użytkownik może sortować po tej kolumnie" hasArrow><span>Sortowalne</span></Tooltip>
                </Th>
                <Th w="80px">
                  <Tooltip label="Czy użytkownik może filtrować po tej kolumnie" hasArrow><span>Filtrowalne</span></Tooltip>
                </Th>
                <Th w="100px">
                  <Tooltip label="Kolumna nie może być edytowana przez użytkownika" hasArrow><span>Tylko do odczytu</span></Tooltip>
                </Th>
                <Th w="100px">
                  <Tooltip label="Czy wartości są sumowane na poziomie etapu" hasArrow><span>Suma w etapie</span></Tooltip>
                </Th>
                <Th w="100px">
                  <Tooltip label="Czy wartości są sumowane w podsumowaniu kosztorysu" hasArrow><span>Suma total</span></Tooltip>
                </Th>
                <Th w="80px">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {fields.map((field, index) => {
                const isSummable = field.type === 3 || field.type === 4 || field.type === 6;
                return (
                  <Tr key={index}>
                    <Td>
                      <Badge colorScheme="green">
                        {field.fieldTypeConfig?.namePl || calculatedFieldTypeLabels[field.type]}
                      </Badge>
                    </Td>
                    <Td>
                      <Input
                        size="sm"
                        value={field.label}
                        onChange={(e) => onUpdate(index, { label: e.target.value })}
                      />
                    </Td>
                    <Td>
                      <Input
                        size="sm"
                        list={`units-list-${index}`}
                        value={field.unit || ''}
                        onChange={(e) => onUpdate(index, { unit: e.target.value })}
                        placeholder="Wybierz lub wpisz"
                      />
                      <datalist id={`units-list-${index}`}>
                        {units.map((u) => (
                          <option key={u.code} value={u.symbol || u.code}>
                            {u.name} ({u.symbol || u.code})
                          </option>
                        ))}
                      </datalist>
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.visible !== false}
                        onChange={(e) => onUpdate(index, { visible: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.sortable}
                        onChange={(e) => onUpdate(index, { sortable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.filterable}
                        onChange={(e) => onUpdate(index, { filterable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.readOnly || false}
                        onChange={(e) => onUpdate(index, { readOnly: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Tooltip label={isSummable ? "Sumuj w podsumowaniu etapu" : "Ta opcja nie jest dostępna dla tego typu pola"} hasArrow>
                        <Box>
                          <Checkbox
                            colorScheme="blue"
                            isChecked={field.sumInGroup || false}
                            onChange={(e) => onUpdate(index, { sumInGroup: e.target.checked })}
                            isDisabled={!isSummable}
                            opacity={!isSummable ? 0.4 : 1}
                          />
                        </Box>
                      </Tooltip>
                    </Td>
                    <Td>
                      <Tooltip label={isSummable ? "Sumuj w podsumowaniu całkowitym" : "Ta opcja nie jest dostępna dla tego typu pola"} hasArrow>
                        <Box>
                          <Checkbox
                            colorScheme="blue"
                            isChecked={field.sumInTotal || false}
                            onChange={(e) => onUpdate(index, { sumInTotal: e.target.checked })}
                            isDisabled={!isSummable}
                            opacity={!isSummable ? 0.4 : 1}
                          />
                        </Box>
                      </Tooltip>
                    </Td>
                    <Td>
                      <Tooltip label="Usuń pole" hasArrow>
                        <IconButton
                          aria-label="Usuń"
                          icon={<Trash2 size={16} />}
                          size="sm"
                          color="red.400"
                          variant="ghost"
                          _hover={{ color: "red.600", bg: "red.50" }}
                          onClick={() => onRemove(index)}
                        />
                      </Tooltip>
                    </Td>
                  </Tr>
                );
              })}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
}


interface GenericFieldsEditorProps {
  fields: GenericFieldDefinition[];
  onAdd: (type: GenericFieldType) => void;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<GenericFieldDefinition>) => void;
  fieldTypeConfigs: Record<string, import('../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb[]>;
}

function GenericFieldsEditor({
  fields,
  onAdd,
  onRemove,
  onUpdate,
  fieldTypeConfigs,
}: GenericFieldsEditorProps) {
  // Pobierz dostępne typy pól generycznych z BE (scope 3 = generic)
  const availableGenericFields = fieldTypeConfigs[3] || [];

  return (
    <VStack spacing={4} align="stretch">
      <Box
        bg="level2.50"
        p={4}
        borderRadius="md"
        borderLeft="3px solid"
        borderLeftColor="level2.500"
      >
        <HStack spacing={2} mb={3}>
          <Text fontSize="sm" fontWeight="bold" color="level2.700">
            Dodaj pole generyczne:
          </Text>
        </HStack>
        <HStack spacing={2} flexWrap="wrap">
          {availableGenericFields.length > 0 ? (
            availableGenericFields.map((config) => {
              // FieldType 300-399 to pola generyczne, konwertuj na GenericFieldType (0-99)
              const genFieldType = (config.fieldType - 300) as GenericFieldType;
              return (
                <Button
                  key={config.fieldType}
                  size="sm"
                  leftIcon={<Plus size={14} />}
                  colorScheme="purple"
                  variant="outline"
                  onClick={() => onAdd(genFieldType)}
                >
                  {config.namePl}
                </Button>
              );
            })
          ) : (
            // Fallback
            Object.entries(genericFieldTypeLabels).map(([type, label]) => (
              <Button
                key={type}
                size="sm"
                leftIcon={<Plus size={14} />}
                colorScheme="purple"
                variant="outline"
                onClick={() => onAdd(parseInt(type) as GenericFieldType)}
              >
                {label}
              </Button>
            ))
          )}
        </HStack>
      </Box>

      {fields.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak pól generycznych</Text>
        </Box>
      ) : (
        <Box overflowX="auto">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>
                  <Tooltip label="Typ pola generycznego" hasArrow><span>Typ pola</span></Tooltip>
                </Th>
                <Th>Etykieta</Th>
                <Th w="80px">
                  <Tooltip label="Czy kolumna jest widoczna w widoku kosztorysu" hasArrow><span>Widoczne</span></Tooltip>
                </Th>
                <Th w="80px">
                  <Tooltip label="Czy użytkownik może sortować po tej kolumnie" hasArrow><span>Sortowalne</span></Tooltip>
                </Th>
                <Th w="80px">
                  <Tooltip label="Czy użytkownik może filtrować po tej kolumnie" hasArrow><span>Filtrowalne</span></Tooltip>
                </Th>
                <Th w="100px">
                  <Tooltip label="Kolumna nie może być edytowana przez użytkownika" hasArrow><span>Tylko do odczytu</span></Tooltip>
                </Th>
                <Th w="80px">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {fields.map((field, index) => {
                return (
                  <Tr key={index}>
                    <Td>
                      <Badge colorScheme="purple">
                        {field.fieldTypeConfig?.namePl || genericFieldTypeLabels[field.type]}
                      </Badge>
                    </Td>
                    <Td>
                      <Input
                        size="sm"
                        value={field.label}
                        onChange={(e) => onUpdate(index, { label: e.target.value })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.visible !== false}
                        onChange={(e) => onUpdate(index, { visible: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.sortable}
                        onChange={(e) => onUpdate(index, { sortable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.filterable}
                        onChange={(e) => onUpdate(index, { filterable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        colorScheme="blue"
                        isChecked={field.readOnly || false}
                        onChange={(e) => onUpdate(index, { readOnly: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Tooltip label="Usuń pole" hasArrow>
                        <IconButton
                          aria-label="Usuń"
                          icon={<Trash2 size={16} />}
                          size="sm"
                          color="red.400"
                          variant="ghost"
                          _hover={{ color: "red.600", bg: "red.50" }}
                          onClick={() => onRemove(index)}
                        />
                      </Tooltip>
                    </Td>
                  </Tr>
                );
              })}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
}

interface SummaryConfigurationEditorProps {
  showGroupSummary: boolean;
  showTotalSummary: boolean;
  groupSummaryFields: string[];
  totalSummaryFields: string[];
  calculatedFields: CalculatedFieldDefinition[];
  onToggleGroupSummary: (value: boolean) => void;
  onToggleTotalSummary: (value: boolean) => void;
  onChangeGroupSummaryFields: (fields: string[]) => void;
  onChangeTotalSummaryFields: (fields: string[]) => void;
}

function SummaryConfigurationEditor({
  showGroupSummary,
  showTotalSummary,
  groupSummaryFields,
  totalSummaryFields,
  calculatedFields,
  onToggleGroupSummary,
  onToggleTotalSummary,
  onChangeGroupSummaryFields,
  onChangeTotalSummaryFields,
}: SummaryConfigurationEditorProps) {
  // Pola które mogą być sumowane (ValueNet, ValueGross, TotalVat)
  const summableFields = calculatedFields.filter(
    (f) => f.summable && (
      f.type === CalculatedFieldType.ValueNet || 
      f.type === CalculatedFieldType.ValueGross || 
      f.type === CalculatedFieldType.TotalVat
    )
  );

  const handleToggleGroupField = (fieldName: string) => {
    if (groupSummaryFields.includes(fieldName)) {
      onChangeGroupSummaryFields(groupSummaryFields.filter(f => f !== fieldName));
    } else {
      onChangeGroupSummaryFields([...groupSummaryFields, fieldName]);
    }
  };

  const handleToggleTotalField = (fieldName: string) => {
    if (totalSummaryFields.includes(fieldName)) {
      onChangeTotalSummaryFields(totalSummaryFields.filter(f => f !== fieldName));
    } else {
      onChangeTotalSummaryFields([...totalSummaryFields, fieldName]);
    }
  };

  return (
    <VStack spacing={6} align="stretch">
      <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
        <Text fontSize="md" fontWeight="bold" mb={4}>
          Ustawienia podsumowań
        </Text>
        <VStack spacing={3} align="stretch">
          <Checkbox isChecked={showGroupSummary} onChange={(e) => onToggleGroupSummary(e.target.checked)}>
            Wyświetlaj podsumowanie grup
          </Checkbox>
          <Checkbox isChecked={showTotalSummary} onChange={(e) => onToggleTotalSummary(e.target.checked)}>
            Wyświetlaj podsumowanie całkowite
          </Checkbox>
        </VStack>
      </Box>

      <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
        <Text fontSize="md" fontWeight="bold" mb={4}>
          Pola do sumowania w etapach
        </Text>
        <Text fontSize="sm" color="gray.600" mb={4}>
          Wybierz pola które mają być sumowane w podsumowaniu etapów. Pozostaw puste aby nie sumować żadnych pól.
        </Text>
        {summableFields.length === 0 ? (
          <Text fontSize="sm" color="gray.500">
            Brak pól dostępnych do sumowania (dodaj pola typu ValueNet, ValueGross lub TotalVat z Summable=true)
          </Text>
        ) : (
          <VStack spacing={2} align="stretch">
            {summableFields.map((field) => (
              <Checkbox
                key={field.name}
                isChecked={groupSummaryFields.includes(field.name)}
                onChange={() => handleToggleGroupField(field.name)}
              >
                <Text fontSize="sm" fontWeight="medium">{field.label}</Text>
              </Checkbox>
            ))}
          </VStack>
        )}
        {groupSummaryFields.length > 0 && (
          <Text fontSize="xs" color="primary.600" mt={3}>
            ✓ Wybrano {groupSummaryFields.length} {groupSummaryFields.length === 1 ? 'pole' : 'pól'} do sumowania
          </Text>
        )}
      </Box>

      <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
        <Text fontSize="md" fontWeight="bold" mb={4}>
          Pola do sumowania w całkowitym podsumowaniu
        </Text>
        <Text fontSize="sm" color="gray.600" mb={4}>
          Wybierz pola które mają być sumowane w całkowitym podsumowaniu (grand total). Pozostaw puste aby nie sumować żadnych pól.
        </Text>
        {summableFields.length === 0 ? (
          <Text fontSize="sm" color="gray.500">
            Brak pól dostępnych do sumowania (dodaj pola typu ValueNet, ValueGross lub TotalVat z Summable=true)
          </Text>
        ) : (
          <VStack spacing={2} align="stretch">
            {summableFields.map((field) => (
              <Checkbox
                key={field.name}
                isChecked={totalSummaryFields.includes(field.name)}
                onChange={() => handleToggleTotalField(field.name)}
              >
                <Text fontSize="sm" fontWeight="medium">{field.label}</Text>
              </Checkbox>
            ))}
          </VStack>
        )}
        {totalSummaryFields.length > 0 && (
          <Text fontSize="xs" color="primary.600" mt={3}>
            ✓ Wybrano {totalSummaryFields.length} {totalSummaryFields.length === 1 ? 'pole' : 'pól'} do sumowania
          </Text>
        )}
      </Box>
    </VStack>
  );
}
