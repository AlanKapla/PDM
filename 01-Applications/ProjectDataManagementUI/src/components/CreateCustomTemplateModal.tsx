import { useState, useEffect, useRef } from "react";
import { useTouchReorder } from "../hooks/useTouchReorder";
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
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
} from "@chakra-ui/react";
import {
  Plus,
  Trash2,
  ChevronUp,
  ChevronDown,
  Settings,
  List,
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
  DollarSign,
  Ruler,
} from "lucide-react";
import type {
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  CostEstimateTemplateStructure,
  CostEstimateGroupDefinition,
  CostEstimateWorkScopeFieldsDefinition,
  CostEstimateSummaryConfiguration,
  CostEstimateUiConfiguration,
  GroupHeaderFieldDefinition,
  CrossFieldValidationRule,
} from "../types/costEstimate.types";
import {
  FieldType,
  FieldScope,
  SummaryScope,
  CalculatedFieldType,
  GenericFieldType,
  GroupHeaderFieldType,
} from "../types/costEstimate.types";
import { fieldTypeLabels, convertFieldTypeToLegacy } from "../utils/fieldTypeLabels";
import { getDefaultGroupHeaderLabel } from "./FieldRenderer";
import type { CostEstimateTemplateDetails } from "../api/costEstimateTemplateApi";
import { CostEstimateExcelView } from "./CostEstimateExcelView";
import type { CostEstimateDataModel, CostEstimateGroup, CostEstimateWorkScope, CostEstimateTemplateDto, CostEstimateCollectionItem } from "../types/costEstimate.types";
import { calculateWorkScope } from "../utils/calculationEngine";

interface CreateCustomTemplateModalProps {
  isOpen: boolean;
  onClose: () => void;
  onTemplateCreated: () => void;
  existingTemplate?: CostEstimateTemplateDetails;
}

const calculatedFieldTypeLabels: Record<CalculatedFieldType, string> = {
  [CalculatedFieldType.UnitPriceNet]: fieldTypeLabels[FieldType.ItemCalculatedUnitPriceNet],
  [CalculatedFieldType.VatRate]: fieldTypeLabels[FieldType.ItemCalculatedVatRate],
  [CalculatedFieldType.UnitPriceGross]: fieldTypeLabels[FieldType.ItemCalculatedUnitPriceGross],
  [CalculatedFieldType.ValueNet]: fieldTypeLabels[FieldType.ItemCalculatedValueNet],
  [CalculatedFieldType.ValueGross]: fieldTypeLabels[FieldType.ItemCalculatedValueGross],
  [CalculatedFieldType.UnitVat]: fieldTypeLabels[FieldType.ItemCalculatedUnitVat],
  [CalculatedFieldType.TotalVat]: fieldTypeLabels[FieldType.ItemCalculatedTotalVat],
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
  [SummaryScope.Group]: "W grupie",
  [SummaryScope.Total]: "W całości",
  [SummaryScope.Both]: "W grupie i całości",
};

export default function CreateCustomTemplateModal({
  isOpen,
  onClose,
  onTemplateCreated,
  existingTemplate,
}: CreateCustomTemplateModalProps) {
  const toast = useToast();
  const [templateName, setTemplateName] = useState(existingTemplate?.name ?? "");
  const [templateDescription, setTemplateDescription] = useState(existingTemplate?.description ?? "");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { isOpen: isConfirmSaveOpen, onOpen: onConfirmSaveOpen, onClose: onConfirmSaveClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);

  // Preview state
  const { isOpen: isPreviewOpen, onOpen: onPreviewOpen, onClose: onPreviewClose } = useDisclosure();
  const [previewData, setPreviewData] = useState<CostEstimateDataModel | null>(null);

  // Template Structure State
  const [canAddGroups, setCanAddGroups] = useState(existingTemplate?.selectedVersion?.templateStructure?.canAddGroups ?? true);
  const [canBranchGroups, setCanBranchGroups] = useState(existingTemplate?.selectedVersion?.templateStructure?.canBranchGroups ?? true);
  const [maxGroupLevel, setMaxGroupLevel] = useState<number | undefined>(existingTemplate?.selectedVersion?.templateStructure?.maxGroupLevel);

  // Group Definition State
  const [groupAutoNumbered, setGroupAutoNumbered] = useState(existingTemplate?.selectedVersion?.templateStructure?.groupDefinition?.autoNumbered ?? true);
  const [groupNumberFormat, setGroupNumberFormat] = useState(existingTemplate?.selectedVersion?.templateStructure?.groupDefinition?.numberFormat ?? "");
  const [headerFields, setHeaderFields] = useState<GroupHeaderFieldDefinition[]>(
    existingTemplate?.selectedVersion?.templateStructure?.groupDefinition?.headerFields ?? [
      {
        type: GroupHeaderFieldType.GroupName,
        required: true,
        visible: true,
        order: 0,
        readOnly: false,
      },
    ]
  );

  // Work Scope Fields State
  const [calculatedFields, setCalculatedFields] = useState<CalculatedFieldDefinition[]>([]);
  const [genericFields, setGenericFields] = useState<GenericFieldDefinition[]>([]);
  const [validationRules, setValidationRules] = useState<CrossFieldValidationRule[]>([]);

  // Currencies and Units State
  const [currencies, setCurrencies] = useState<Array<{ code: string; name: string; symbol?: string; isDefault: boolean; order: number }>>([]);
  const [units, setUnits] = useState<Array<{ code: string; name: string; symbol: string; category?: string; isDefault: boolean; order: number }>>([]);

  // Summary Configuration State
  const [showGroupSummary, setShowGroupSummary] = useState(true);
  const [showTotalSummary, setShowTotalSummary] = useState(true);
  const [groupSummaryFields, setGroupSummaryFields] = useState<string[]>([]);
  const [totalSummaryFields, setTotalSummaryFields] = useState<string[]>([]);

  // UI Configuration State
  const [columnLayout, setColumnLayout] = useState<string[]>([]);

  // Drag and drop state dla układu pól
  const [draggedIndexLayout, setDraggedIndexLayout] = useState<number | null>(null);

  // Obsługa przeciągania na urządzeniach dotykowych (smartfony, tablety)
  const { createTouchHandlers } = useTouchReorder();

  // Update state when existingTemplate changes (e.g., after async load)
  useEffect(() => {
    if (existingTemplate) {
      setTemplateName(existingTemplate.name);
      setTemplateDescription(existingTemplate.description ?? "");

      // Użyj structure z nowego API (bez wersjonowania)
      if (existingTemplate.structure) {
        const struct = existingTemplate.structure;
        
        // Update structure settings
        setCanAddGroups(existingTemplate.canAddGroups ?? true);
        setCanBranchGroups(existingTemplate.canBranchGroups ?? true);
        setMaxGroupLevel(existingTemplate.maxGroupLevel);
        setGroupAutoNumbered(existingTemplate.autoNumberGroups ?? true);
        setGroupNumberFormat(existingTemplate.groupNumberFormat ?? "");

        // Update header fields from structure
        if (struct.groupHeaderFields && struct.groupHeaderFields.length > 0) {
          setHeaderFields(struct.groupHeaderFields.map(f => ({
            id: f.id,
            name: f.fieldName,
            type: f.fieldType as GroupHeaderFieldType,
            customLabel: f.customLabel,
            required: f.isRequired,
            visible: f.isVisible,
            sortable: f.isSortable ?? true,
            filterable: f.isFilterable ?? true,
            order: f.order,
            readOnly: f.isReadOnly,
            fieldTypeConfig: f.fieldTypeConfig,
          })));
        } else {
          setHeaderFields([
            {
              type: GroupHeaderFieldType.GroupName,
              required: true,
              visible: true,
              order: 0,
              readOnly: false,
            },
          ]);
        }

        // Update calculated fields
        if (struct.calculatedFields) {
          setCalculatedFields(struct.calculatedFields.map(f => ({
            id: f.id,
            name: f.fieldName,
            label: f.label,
            type: convertFieldTypeToLegacy(f.fieldType),
            order: f.order,
            required: f.isRequired || false,
            visible: f.isVisible,
            sortable: f.isSortable,
            filterable: f.isFilterable,
            summable: f.isSummable || false,
            summaryScope: f.summaryScope,
            sumInGroup: f.sumInGroup,
            sumInTotal: f.sumInTotal,
            autoCalculated: f.isAutoCalculated,
            readOnly: f.isReadOnly,
            fieldTypeConfig: f.fieldTypeConfig,
          })));
        }

        // Update generic fields
        if (struct.genericFields) {
          setGenericFields(struct.genericFields.map(f => ({
            id: f.id,
            name: f.fieldName,
            label: f.label,
            type: convertFieldTypeToLegacy(f.fieldType),
            order: f.order,
            required: f.isRequired,
            visible: f.isVisible,
            sortable: f.isSortable,
            filterable: f.isFilterable,
            fieldTypeConfig: f.fieldTypeConfig,
          })));
        }

        setValidationRules([]);

        // Update summary configuration - mapuj SummaryFieldWeb[] na string[]
        setShowGroupSummary(struct.summaryConfiguration?.showGroupSummary ?? true);
        setShowTotalSummary(struct.summaryConfiguration?.showTotalSummary ?? true);
        setGroupSummaryFields(struct.summaryConfiguration?.groupSummaryFields.map(f => f.fieldName) ?? []);
        setTotalSummaryFields(struct.summaryConfiguration?.totalSummaryFields.map(f => f.fieldName) ?? []);

        // Update UI configuration - mapuj ColumnConfigurationWeb[] na string[]
        setColumnLayout(struct.uiConfiguration?.columns?.map(col => col.fieldName) ?? []);
        
        // Load currencies and units from structure
        setCurrencies(struct.currencies.map(c => ({
          code: c.code,
          name: c.name,
          symbol: c.symbol,
          isDefault: c.isDefault,
          order: c.order,
        })));
        
        setUnits(struct.units.map(u => ({
          code: u.code,
          name: u.name,
          symbol: u.symbol,
          category: u.category,
          isDefault: u.isDefault,
          order: u.order,
        })));
      }
    }
  }, [existingTemplate]);

  // Funkcja generująca przykładowy kosztorys z szablonu
  const generateSampleCostEstimate = (): CostEstimateDataModel => {
    const sampleGroups: CostEstimateGroup[] = [];

    // Generuj 2-3 przykładowe grupy
    for (let i = 0; i < 2; i++) {
      const workScopes: CostEstimateWorkScope[] = [];
      
      // Generuj 2-3 przykładowe pozycje w grupie
      for (let j = 0; j < 3; j++) {
        const calculatedFieldValues: Record<string, number> = {};
        const genericFieldValues: Record<string, any> = {};
        const collectionFieldValues: Record<string, CostEstimateCollectionItem[]> = {};

        // Wypełnij pola kalkulowane przykładowymi wartościami
        calculatedFields.forEach((field) => {
          if (field.type === CalculatedFieldType.UnitPriceNet) {
            calculatedFieldValues[field.name] = 100 + j * 50;
          } else if (field.type === CalculatedFieldType.VatRate) {
            calculatedFieldValues[field.name] = 23;
          }
          // Inne pola będą auto-kalkulowane
        });

        // Wypełnij pola generyczne
        genericFields.forEach((field) => {
          if (field.type === GenericFieldType.String) {
            genericFieldValues[field.name] = `Przykładowa pozycja ${i + 1}.${j + 1}`;
          } else if (field.type === GenericFieldType.Integer) {
            genericFieldValues[field.name] = 10 + j;
          } else if (field.type === GenericFieldType.Decimal) {
            genericFieldValues[field.name] = 10.5 + j;
          } else if (field.type === GenericFieldType.Boolean) {
            genericFieldValues[field.name] = j % 2 === 0;
          } else if (field.type === GenericFieldType.Date) {
            const date = new Date();
            date.setDate(date.getDate() + j);
            genericFieldValues[field.name] = date.toISOString().split('T')[0];
          } else if (field.type === GenericFieldType.DateTime) {
            const date = new Date();
            date.setDate(date.getDate() + j);
            genericFieldValues[field.name] = date.toISOString();
          }
        });

        // Utwórz workScope z bazowymi wartościami
        let workScope: CostEstimateWorkScope = {
          id: `ws-${i}-${j}`,
          order: j,
          calculatedFieldValues,
          genericFieldValues,
          collectionFieldValues: Object.keys(collectionFieldValues).length > 0 ? collectionFieldValues : undefined,
        };
        
        // Przelicz auto-kalkulowane pola
        workScope = calculateWorkScope(workScope, {
          calculatedFields,
          genericFields,
        });
        
        workScopes.push(workScope);
      }

      const headerValues: Record<string, any> = {};
      headerFields.forEach((field) => {
        const fieldKey = GroupHeaderFieldType[field.type];
        if (field.type === GroupHeaderFieldType.GroupName) {
          headerValues[fieldKey] = `Grupa ${i + 1}`;
        } else if (field.type === GroupHeaderFieldType.GroupNumber) {
          headerValues[fieldKey] = `${i + 1}`;
        } else if (field.type === GroupHeaderFieldType.GroupDescription) {
          headerValues[fieldKey] = `Opis grupy ${i + 1}`;
        }
      });

      sampleGroups.push({
        id: `group-${i}`,
        level: 0,
        order: i,
        headerValues,
        workScopes,
      });
    }

    return {
      groups: sampleGroups,
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

    return {
      name: crypto.randomUUID(),
      label: calculatedFieldTypeLabels[type],
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
    };
  };

  const createDefaultGenericField = (type: GenericFieldType): GenericFieldDefinition => {
    return {
      name: crypto.randomUUID(),
      label: genericFieldTypeLabels[type],
      type: type,
      order: calculatedFields.length + genericFields.length,
      required: false,
      visible: true,
      sortable: true,
      filterable: true,
    };
  };

  const handleAddHeaderField = (type: GroupHeaderFieldType) => {
    const newField: GroupHeaderFieldDefinition = {
      name: crypto.randomUUID(),
      type: type,
      required: type === GroupHeaderFieldType.GroupName,
      visible: true,
      order: headerFields.length,
      readOnly: false,
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

    const hasGroupName = headerFields.some((f) => f.type === GroupHeaderFieldType.GroupName);
    if (!hasGroupName) {
      toast({
        title: "Błąd walidacji",
        description: "Pole 'Nazwa grupy' jest wymagane w nagłówku",
        status: "error",
        duration: 3000,
      });
      return false;
    }

    return true;
  };

  const handleSubmitClick = () => {
    if (!validateTemplate()) return;
    handleSubmit();
  };

  // Potwierdzenie zapisu zatwierdzonej wersji — zamyka dialog potwierdzenia i uruchamia zapis
  const confirmSaveApprovedVersion = () => {
    onConfirmSaveClose();
    handleSubmit();
  };

  const handleSubmit = async () => {
    setIsSubmitting(true);

    try {
      const { costEstimateTemplateApi } = await import("../api/costEstimateTemplateApi");

      if (existingTemplate) {
        // Update existing template (bez wersjonowania)
        await costEstimateTemplateApi.updateTemplate(existingTemplate.id, {
          templateId: existingTemplate.id,
          name: templateName,
          description: templateDescription || undefined,
          category: undefined,  // TODO: dodać pole category w UI
          canAddGroups,
          canBranchGroups,
          maxGroupLevel,
          autoNumberGroups: groupAutoNumbered,
          groupNumberFormat: groupNumberFormat || undefined,
          updateStructure: true,
          currencies,
          units,
          groupHeaderFields: headerFields.map(f => ({
            fieldName: f.name || crypto.randomUUID(),
            fieldType: f.type,
            label: f.customLabel || `Pole grupy`,
            isSortable: false,
            isFilterable: false,
            isVisible: f.visible,
          })),
          systemFields: [],  // TODO: obsługa system fields
          calculatedFields: calculatedFields.map(f => ({
            fieldName: f.name,
            fieldType: f.type + 200,
            label: f.label,
            isSortable: f.sortable,
            isFilterable: f.filterable,
            isVisible: f.visible,
          })),
          genericFields: genericFields.map(f => ({
            fieldName: f.name,
            fieldType: f.type + 300,
            label: f.label,
            isSortable: f.sortable,
            isFilterable: f.filterable,
            isVisible: f.visible,
          })),
          summaryConfiguration: {
            showGroupSummary,
            showTotalSummary,
            groupSummaryFields: groupSummaryFields.length > 0 ? groupSummaryFields : [],
            totalSummaryFields: totalSummaryFields.length > 0 ? totalSummaryFields : [],
          },
          uiConfiguration: {
            columnLayout: columnLayout.length > 0 ? columnLayout : undefined,
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
          currencies,
          units,
          groupHeaderFields: headerFields.map(f => ({
            fieldName: f.name || crypto.randomUUID(),
            fieldType: f.type,
            label: f.customLabel || `Pole grupy`,
            isSortable: f.sortable ?? true,
            isFilterable: f.filterable ?? true,
            isVisible: f.visible,
          })),
          systemFields: [],  // TODO: obsługa system fields
          calculatedFields: calculatedFields.map(f => ({
            fieldName: f.name,
            fieldType: f.type + 200,
            label: f.label,
            isSortable: f.sortable,
            isFilterable: f.filterable,
            isVisible: f.visible,
          })),
          genericFields: genericFields.map(f => ({
            fieldName: f.name,
            fieldType: f.type + 300,
            label: f.label,
            isSortable: f.sortable,
            isFilterable: f.filterable,
            isVisible: f.visible,
          })),
          summaryConfiguration: {
            showGroupSummary,
            showTotalSummary,
            groupSummaryFields: groupSummaryFields.length > 0 ? groupSummaryFields : [],
            totalSummaryFields: totalSummaryFields.length > 0 ? totalSummaryFields : [],
          },
          uiConfiguration: {
            columnLayout: columnLayout.length > 0 ? columnLayout : undefined,
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

      onTemplateCreated();
      handleClose();
    } catch (error) {
      console.error("Błąd podczas zapisywania szablonu:", error);
      toast({
        title: "Błąd",
        description: existingTemplate
          ? "Nie udało się zaktualizować szablonu"
          : "Nie udało się utworzyć szablonu",
        status: "error",
        duration: 5000,
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    setTemplateName("");
    setTemplateDescription("");
    setCanAddGroups(true);
    setCanBranchGroups(true);
    setMaxGroupLevel(undefined);
    setGroupAutoNumbered(true);
    setGroupNumberFormat("");
    setHeaderFields([
      {
        type: GroupHeaderFieldType.GroupName,
        required: true,
        visible: true,
        order: 0,
        readOnly: false,
      },
    ]);
    setCalculatedFields([]);
    setGenericFields([]);
    setValidationRules([]);
    setShowGroupSummary(true);
    setShowTotalSummary(true);
    setGroupSummaryFields([]);
    setTotalSummaryFields([]);
    setColumnLayout([]);
    setCurrencies([]);
    setUnits([]);
    onClose();
  };

  // Funkcja zatwierdzania wersji szablonu
  const handleApproveVersion = async () => {
    if (!existingTemplate?.selectedVersion) {
      toast({
        title: "Błąd",
        description: "Brak wybranej wersji do zatwierdzenia",
        status: "error",
        duration: 3000,
      });
      return;
    }

    try {
      const { costEstimateTemplateApi } = await import("../api/costEstimateTemplateApi");
      await costEstimateTemplateApi.approveVersion(
        existingTemplate.id,
        existingTemplate.selectedVersion.id
      );
      
      toast({
        title: "Sukces",
        description: `Wersja v${existingTemplate.selectedVersion.versionNumber} została zatwierdzona`,
        status: "success",
        duration: 3000,
      });

      onTemplateCreated();
      handleClose();
    } catch (error) {
      console.error("Błąd podczas zatwierdzania wersji:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się zatwierdzić wersji szablonu",
        status: "error",
        duration: 5000,
      });
    }
  };

  // Render funkcja dla TabPanel "Układ pól"
  const renderFieldLayoutTab = () => {
    // Zbierz wszystkie pola z template
    const allFields: Array<{ name: string; label: string; type: string; colorScheme: string }> = [];

    // Pola nagłówków grup (GroupName, Notes, etc.)
    headerFields.forEach((field) => {
      allFields.push({
        name: field.name || crypto.randomUUID(), // GUID pola, nie nazwa typu
        label: field.customLabel || getDefaultGroupHeaderLabel(field.type),
        type: 'Nagłówek grupy',
        colorScheme: 'purple',
      });
    });

    // Pola obliczeniowe
    calculatedFields.forEach((field) => {
      allFields.push({
        name: field.name,
        label: field.label,
        type: 'Obliczeniowe',
        colorScheme: 'blue',
      });
    });

    // Pola generyczne
    genericFields.forEach((field) => {
      allFields.push({
        name: field.name,
        label: field.label,
        type: 'Generyczne',
        colorScheme: 'green',
      });
    });

    // Upewnij się że columnLayout zawiera wszystkie pola
    const currentLayout = [...columnLayout];
    allFields.forEach((field) => {
      if (!currentLayout.includes(field.name)) {
        currentLayout.push(field.name);
      }
    });

    // Usuń pola które już nie istnieją
    const validFieldNames = allFields.map((f) => f.name);
    const validatedLayout = currentLayout.filter((name) => validFieldNames.includes(name));

    // Sortuj pola według columnLayout
    const sortedFields = [...allFields].sort((a, b) => {
      const indexA = validatedLayout.indexOf(a.name);
      const indexB = validatedLayout.indexOf(b.name);
      return indexA - indexB;
    });

    const handleDragStart = (index: number) => {
      setDraggedIndexLayout(index);
    };

    const handleDragOver = (e: React.DragEvent, index: number) => {
      e.preventDefault();
      if (draggedIndexLayout === null || draggedIndexLayout === index) return;

      const newLayout = [...validatedLayout];
      const draggedItem = newLayout[draggedIndexLayout];
      newLayout.splice(draggedIndexLayout, 1);
      newLayout.splice(index, 0, draggedItem);
      
      setColumnLayout(newLayout);
      setDraggedIndexLayout(index);
    };

    const handleDragEnd = () => {
      setDraggedIndexLayout(null);
    };

    return (
      <VStack spacing={4} align="stretch">
        <Box bg="blue.50" p={4} borderRadius="md" borderWidth="1px" borderColor="blue.200">
          <HStack spacing={2} mb={2}>
            <Icon as={Layout} color="blue.600" />
            <Text fontSize="md" fontWeight="bold" color="blue.800">
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
                  bg={draggedIndexLayout === index ? 'blue.100' : 'gray.50'}
                  borderRadius="md"
                  borderWidth="2px"
                  borderColor={draggedIndexLayout === index ? 'blue.400' : 'gray.200'}
                  spacing={3}
                  cursor="grab"
                  _hover={{ bg: draggedIndexLayout === index ? 'blue.100' : 'gray.100', borderColor: 'blue.300' }}
                  _active={{ cursor: 'grabbing' }}
                  draggable
                  onDragStart={() => handleDragStart(index)}
                  onDragOver={(e) => handleDragOver(e, index)}
                  onDragEnd={handleDragEnd}
                  {...createTouchHandlers(index, draggedIndexLayout, setDraggedIndexLayout, (from, to) => {
                    const newLayout = [...validatedLayout];
                    const [moved] = newLayout.splice(from, 1);
                    newLayout.splice(to, 0, moved);
                    setColumnLayout(newLayout);
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

  return (
    <>
      <Modal isOpen={isOpen} onClose={handleClose} size="full" scrollBehavior="inside">
        <ModalOverlay />
      <ModalContent maxH="100vh" m={0}>
        <ModalHeader borderBottom="1px" borderColor="gray.200">
          <HStack spacing={3} justify="space-between" w="full" pr={10}>
            <HStack spacing={3}>
              <FileText size={24} />
              <Text>{existingTemplate ? "Edycja szablonu kosztorysu" : "Kreator szablonu kosztorysu"}</Text>
            </HStack>
            <HStack spacing={2}>
              <Button
                leftIcon={<Eye size={18} />}
                size="sm"
                variant="outline"
                colorScheme="blue"
                onClick={handlePreview}
              >
                Podgląd
              </Button>
              {existingTemplate && (
                <Button
                  leftIcon={<History size={18} />}
                  size="sm"
                  variant="outline"
                  colorScheme="purple"
                  onClick={() => {
                    window.open(`/cost-estimate-templates/${existingTemplate.id}/versions`, '_blank');
                  }}
                >
                  Historia wersji
                </Button>
              )}
            </HStack>
          </HStack>
        </ModalHeader>
        <ModalCloseButton />

        <ModalBody p={6}>
          <VStack spacing={6} align="stretch" maxW="1400px" mx="auto">
            <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
              <Text fontSize="lg" fontWeight="bold" mb={4}>
                Informacje podstawowe
              </Text>
              <VStack spacing={4} align="stretch">
                {existingTemplate?.selectedVersion && (
                  <Box p={3} bg="blue.50" borderRadius="md" borderWidth="1px" borderColor="blue.200">
                    <HStack justify="space-between" align="center">
                      <VStack align="start" spacing={0}>
                        <Text fontSize="sm" fontWeight="bold" color="blue.800">
                          Status wersji: v{existingTemplate.selectedVersion.versionNumber}
                        </Text>
                        <Text fontSize="xs" color="blue.600">
                          Utworzona: {new Date(existingTemplate.selectedVersion.createdAt).toLocaleDateString('pl-PL')}
                        </Text>
                      </VStack>
                      <Badge
                        colorScheme={
                          existingTemplate.selectedVersion.status === 1 ? "green" : "gray"
                        }
                        fontSize="md"
                        px={3}
                        py={1}
                      >
                        {existingTemplate.selectedVersion.status === 1 ? "Zatwierdzona" : "Szkic"}
                      </Badge>
                    </HStack>
                    {existingTemplate.selectedVersion.status === 1 && (
                      <Text fontSize="xs" color="orange.600" mt={2}>
                        ⚠️ Edycja struktury zatwierdzonej wersji utworzy nową wersję szkicu (v{existingTemplate.selectedVersion.versionNumber + 1})
                      </Text>
                    )}
                  </Box>
                )}

                <FormControl isRequired>
                  <FormLabel>Nazwa szablonu</FormLabel>
                  <Input
                    value={templateName}
                    onChange={(e) => setTemplateName(e.target.value)}
                    placeholder="np. Kosztorys robót budowlanych"
                    size="lg"
                  />
                </FormControl>

                <FormControl>
                  <FormLabel>Opis szablonu</FormLabel>
                  <Textarea
                    value={templateDescription}
                    onChange={(e) => setTemplateDescription(e.target.value)}
                    placeholder="Opcjonalny opis przeznaczenia i zastosowania szablonu"
                    rows={3}
                  />
                </FormControl>
              </VStack>
            </Box>

            <Divider />

            <Tabs colorScheme="blue" variant="enclosed">
              <TabList>
                <Tab>
                  <HStack spacing={2}>
                    <Settings size={18} />
                    <Text>Konfiguracja grup</Text>
                  </HStack>
                </Tab>
                <Tab>
                  <HStack spacing={2}>
                    <Tag size={18} />
                    <Text>Pola grup ({headerFields.length})</Text>
                  </HStack>
                </Tab>
                <Tab>
                  <HStack spacing={2}>
                    <List size={18} />
                    <Text>Pola pozycji ({calculatedFields.length + genericFields.length})</Text>
                  </HStack>
                </Tab>
                <Tab>
                  <HStack spacing={2}>
                    <DollarSign size={18} />
                    <Text>Waluty i jednostki ({currencies.length + units.length})</Text>
                  </HStack>
                </Tab>
                <Tab>
                  <HStack spacing={2}>
                    <Layout size={18} />
                    <Text>Kolejność pól</Text>
                  </HStack>
                </Tab>
                <Tab>
                  <HStack spacing={2}>
                    <Layers size={18} />
                    <Text>Podsumowania</Text>
                  </HStack>
                </Tab>
              </TabList>

              <TabPanels>
                <TabPanel>
                  <VStack spacing={6} align="stretch">
                    <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
                      <Text fontSize="md" fontWeight="bold" mb={4}>
                        Ustawienia struktury grup
                      </Text>
                      <VStack spacing={4} align="stretch">
                        <Checkbox
                          isChecked={canAddGroups}
                          onChange={(e) => setCanAddGroups(e.target.checked)}
                        >
                          <HStack spacing={2}>
                            <Text>Można dodawać nowe grupy podczas wypełniania</Text>
                            <Tooltip label="Użytkownicy będą mogli tworzyć dodatkowe grupy w kosztorysie">
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
                            <Text>Można tworzyć podgrupy (rozgałęzianie)</Text>
                            <Tooltip label="Grupy mogą zawierać zagnieżdżone podgrupy">
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
                  />
                </TabPanel>

                <TabPanel>
                  <VStack spacing={6} align="stretch">
                    <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
                      <HStack spacing={2} mb={4}>
                        <Calculator size={20} />
                        <Text fontSize="lg" fontWeight="bold">Pola obliczeniowe</Text>
                      </HStack>
                      <CalculatedFieldsEditor
                        fields={calculatedFields}
                        onAdd={handleAddCalculatedField}
                        onRemove={handleRemoveCalculatedField}
                        onUpdate={handleUpdateCalculatedField}
                      />
                    </Box>

                    <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
                      <HStack spacing={2} mb={4}>
                        <Tag size={20} />
                        <Text fontSize="lg" fontWeight="bold">Pola generyczne</Text>
                      </HStack>
                      <GenericFieldsEditor
                        fields={genericFields}
                        onAdd={handleAddGenericField}
                        onRemove={handleRemoveGenericField}
                        onUpdate={handleUpdateGenericField}
                      />
                    </Box>
                  </VStack>
                </TabPanel>

                <TabPanel>
                  <VStack spacing={6} align="stretch">
                    <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
                      <HStack spacing={2} mb={4}>
                        <DollarSign size={20} />
                        <Text fontSize="lg" fontWeight="bold">Waluty</Text>
                      </HStack>
                      <CurrenciesEditor
                        currencies={currencies}
                        onChange={setCurrencies}
                      />
                    </Box>

                    <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
                      <HStack spacing={2} mb={4}>
                        <Ruler size={20} />
                        <Text fontSize="lg" fontWeight="bold">Jednostki miary</Text>
                      </HStack>
                      <UnitsEditor
                        units={units}
                        onChange={setUnits}
                      />
                    </Box>
                  </VStack>
                </TabPanel>

                <TabPanel>
                  {renderFieldLayoutTab()}
                </TabPanel>

                <TabPanel>
                  <SummaryConfigurationEditor
                    showGroupSummary={showGroupSummary}
                    showTotalSummary={showTotalSummary}
                    groupSummaryFields={groupSummaryFields}
                    totalSummaryFields={totalSummaryFields}
                    calculatedFields={calculatedFields}
                    onToggleGroupSummary={setShowGroupSummary}
                    onToggleTotalSummary={setShowTotalSummary}
                    onChangeGroupSummaryFields={setGroupSummaryFields}
                    onChangeTotalSummaryFields={setTotalSummaryFields}
                  />
                </TabPanel>
              </TabPanels>
            </Tabs>
          </VStack>
        </ModalBody>

        <ModalFooter borderTop="1px" borderColor="gray.200">
          <HStack spacing={3} width="100%" justify="space-between">
            <HStack spacing={3}>
              {existingTemplate?.selectedVersion?.status === 0 && (
                <Button
                  colorScheme="green"
                  size="lg"
                  onClick={handleApproveVersion}
                  leftIcon={<Check size={20} />}
                >
                  Zatwierdź wersję
                </Button>
              )}
            </HStack>
            <HStack spacing={3}>
              <Button variant="ghost" size="lg" onClick={handleClose}>
                Anuluj
              </Button>
              <Button
                colorScheme="blue"
                size="lg"
                onClick={handleSubmitClick}
                isLoading={isSubmitting}
                loadingText={existingTemplate ? "Zapisywanie..." : "Tworzenie..."}
                leftIcon={<Plus size={20} />}
              >
                {existingTemplate ? "Zapisz zmiany" : "Utwórz szablon"}
              </Button>
            </HStack>
          </HStack>
        </ModalFooter>
      </ModalContent>
    </Modal>

      {/* ALERT DIALOG: Confirm saving approved version (creates new draft) */}
      <AlertDialog
        isOpen={isConfirmSaveOpen}
        leastDestructiveRef={cancelRef}
        onClose={onConfirmSaveClose}
      >
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader fontSize="lg" fontWeight="bold">
              Edycja zatwierdzonej wersji
            </AlertDialogHeader>

            <AlertDialogBody>
              <VStack align="flex-start" spacing={3}>
                <Text>
                  Edytujesz zatwierdzoną wersję <Badge colorScheme="blue">v{existingTemplate?.selectedVersion?.versionNumber}</Badge>. 
                  Zapisanie zmian spowoduje utworzenie nowej wersji szkicu.
                </Text>
                <Box p={3} bg="orange.50" borderRadius="md" borderWidth="1px" borderColor="orange.200" w="full">
                  <HStack spacing={2}>
                    <Text fontSize="2xl">⚠️</Text>
                    <VStack align="flex-start" spacing={1}>
                      <Text fontSize="sm" fontWeight="bold" color="orange.800">
                        Co się stanie?
                      </Text>
                      <Text fontSize="sm" color="orange.700">
                        • Zostanie utworzona nowa wersja szkicu z wprowadzonymi zmianami<br />
                        • Zatwierdzona wersja pozostanie bez zmian<br />
                        • Będziesz mógł kontynuować edycję nowej wersji szkicu
                      </Text>
                    </VStack>
                  </HStack>
                </Box>
              </VStack>
            </AlertDialogBody>

            <AlertDialogFooter>
              <Button ref={cancelRef} onClick={onConfirmSaveClose}>
                Anuluj
              </Button>
              <Button 
                colorScheme="blue" 
                onClick={confirmSaveApprovedVersion} 
                ml={3}
                isLoading={isSubmitting}
              >
                Kontynuuj zapis
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>

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
                  <Box bg="blue.50" p={4} borderRadius="md" borderWidth="1px" borderColor="blue.200">
                    <HStack spacing={2}>
                      <AlertCircle size={20} color="blue" />
                      <Text fontSize="sm" color="blue.700">
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
                <CostEstimateExcelView
                  dataModel={previewData!}
                  template={{
                    id: existingTemplate?.id || "preview",
                    name: templateName || "Nowy szablon",
                    description: templateDescription || undefined,
                    ownerId: existingTemplate?.ownerId || "",
                    ownerName: existingTemplate?.ownerName || "",
                    templateVersionNumber: existingTemplate?.selectedVersion?.versionNumber || 1,
                    templateStructure: {
                      canAddGroups,
                      canBranchGroups,
                      maxGroupLevel,
                      groupDefinition: {
                        autoNumbered: groupAutoNumbered,
                        numberFormat: groupNumberFormat,
                        headerFields,
                      },
                      workScopeFieldsDefinition: {
                        calculatedFields,
                        genericFields,
                        crossFieldValidationRules: validationRules,
                      },
                      summaryConfiguration: {
                        showGroupSummary,
                        showTotalSummary,
                        groupSummaryFields,
                        totalSummaryFields,
                      },
                      uiConfiguration: {
                        columnLayout,
                      },
                    },
                    createdAt: existingTemplate?.createdAt || new Date().toISOString(),
                    updatedAt: existingTemplate?.updatedAt || new Date().toISOString(),
                  } as unknown as CostEstimateTemplateDto}
                  readOnly={true}
                  editable={false}
                />
              </Box>
            )}
          </ModalBody>
          <ModalFooter borderTop="1px" borderColor="gray.200">
            <Button onClick={onPreviewClose}>Zamknij</Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </>
  );
}

// ======================== SUB-COMPONENTS ========================

interface HeaderFieldsEditorProps {
  headerFields: GroupHeaderFieldDefinition[];
  onAdd: (type: GroupHeaderFieldType) => void;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<GroupHeaderFieldDefinition>) => void;
  onReorder: (reorderedFields: GroupHeaderFieldDefinition[]) => void;
}

function HeaderFieldsEditor({ headerFields, onAdd, onRemove, onUpdate, onReorder }: HeaderFieldsEditorProps) {
  return (
    <VStack spacing={4} align="stretch">
      <Box bg="blue.50" p={4} borderRadius="md">
        <Text fontSize="sm" fontWeight="bold" mb={3}>
          Dodaj pole nagłówka:
        </Text>
        <HStack spacing={2} flexWrap="wrap">
          {Object.entries(groupHeaderFieldTypeLabels).map(([type, label]) => {
            const typeNum = parseInt(type) as GroupHeaderFieldType;
            const isAdded = headerFields.some((f) => f.type === typeNum);
            return (
              <Button
                key={type}
                size="sm"
                leftIcon={<Plus size={14} />}
                colorScheme="purple"
                variant={isAdded ? "solid" : "outline"}
                onClick={() => onAdd(typeNum)}
                isDisabled={isAdded}
              >
                {label}
              </Button>
            );
          })}
        </HStack>
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
                <Th>Typ pola</Th>
                <Th>Etykieta własna</Th>
                <Th w="100px">Wymagane</Th>
                <Th w="100px">Widoczne</Th>
                <Th w="100px">Tylko odczyt</Th>
                <Th w="80px">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {headerFields.map((field, index) => {
                const isGroupName = field.type === GroupHeaderFieldType.GroupName;
                return (
                  <Tr key={index}>
                    <Td>
                      <Badge colorScheme="purple">
                        {groupHeaderFieldTypeLabels[field.type]}
                      </Badge>
                    </Td>
                    <Td>
                      <Input
                        size="sm"
                        value={field.customLabel || ''}
                        onChange={(e) => onUpdate(index, { customLabel: e.target.value })}
                        placeholder={groupHeaderFieldTypeLabels[field.type]}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        isChecked={field.required}
                        onChange={(e) => onUpdate(index, { required: e.target.checked })}
                        isDisabled={isGroupName}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        isChecked={field.visible}
                        onChange={(e) => onUpdate(index, { visible: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        isChecked={field.readOnly}
                        onChange={(e) => onUpdate(index, { readOnly: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <IconButton
                        aria-label="Usuń"
                        icon={<Trash2 size={16} />}
                        size="sm"
                        colorScheme="red"
                        variant="ghost"
                        onClick={() => onRemove(index)}
                        isDisabled={isGroupName}
                      />
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

interface CalculatedFieldsEditorProps{
  fields: CalculatedFieldDefinition[];
  onAdd: (type: CalculatedFieldType) => void;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<CalculatedFieldDefinition>) => void;
}

function CalculatedFieldsEditor({
  fields,
  onAdd,
  onRemove,
  onUpdate,
}: CalculatedFieldsEditorProps) {
  return (
    <VStack spacing={4} align="stretch">
      <Box bg="purple.50" p={4} borderRadius="md">
        <Text fontSize="sm" fontWeight="bold" mb={3}>
          Dodaj pole obliczeniowe (każde tylko raz):
        </Text>
        <HStack spacing={2} flexWrap="wrap">
          {Object.entries(calculatedFieldTypeLabels).map(([type, label]) => {
            const typeNum = parseInt(type) as CalculatedFieldType;
            const isAdded = fields.some((f) => f.type === typeNum);
            return (
              <Button
                key={type}
                size="sm"
                leftIcon={<Plus size={14} />}
                colorScheme="purple"
                variant={isAdded ? "solid" : "outline"}
                onClick={() => onAdd(typeNum)}
                isDisabled={isAdded}
              >
                {label}
              </Button>
            );
          })}
        </HStack>
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
                <Th>Typ pola</Th>
                <Th>Etykieta</Th>
                <Th w="80px">Sortowalne</Th>
                <Th w="80px">Filtrowalne</Th>
                <Th w="80px">Sumowalne</Th>
                <Th w="100px">Widoczne</Th>
                <Th w="80px">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {fields.map((field, index) => {
                const isSummable = field.type === 3 || field.type === 4 || field.type === 6;
                return (
                  <Tr key={index}>
                    <Td>
                      <Badge colorScheme="blue">
                        {calculatedFieldTypeLabels[field.type]}
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
                        isChecked={field.sortable}
                        onChange={(e) => onUpdate(index, { sortable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        isChecked={field.filterable}
                        onChange={(e) => onUpdate(index, { filterable: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        isChecked={field.summable}
                        onChange={(e) => onUpdate(index, { summable: e.target.checked })}
                        isDisabled={!isSummable}
                      />
                    </Td>
                    <Td>
                      <Checkbox
                        isChecked={field.visible}
                        onChange={(e) => onUpdate(index, { visible: e.target.checked })}
                      />
                    </Td>
                    <Td>
                      <IconButton
                        aria-label="Usuń"
                        icon={<Trash2 size={16} />}
                        size="sm"
                        colorScheme="red"
                        variant="ghost"
                        onClick={() => onRemove(index)}
                      />
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

interface CalculatedFieldEditorProps {
  field: CalculatedFieldDefinition;
  index: number;
  totalFields: number;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<CalculatedFieldDefinition>) => void;
}

function CalculatedFieldEditor({
  field,
  index,
  totalFields,
  onRemove,
  onUpdate,
}: CalculatedFieldEditorProps) {
  const { isOpen, onToggle } = useDisclosure({ defaultIsOpen: false });

  return (
    <Box borderWidth="1px" borderRadius="md" bg="white" shadow="sm">
      <HStack p={4} justify="space-between">
        <HStack spacing={3} flex={1}>
          <Badge colorScheme="blue">#{field.order + 1}</Badge>
          <Text fontWeight="medium">{field.label}</Text>
          <Badge colorScheme="purple">{calculatedFieldTypeLabels[field.type]}</Badge>
          {field.autoCalculated && <Badge colorScheme="green">Auto</Badge>}
        </HStack>
        <HStack spacing={2}>
          <IconButton
            aria-label="Rozwiń"
            icon={isOpen ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
            size="sm"
            variant="ghost"
            onClick={onToggle}
          />
          <IconButton
            aria-label="Usuń"
            icon={<Trash2 size={16} />}
            size="sm"
            colorScheme="red"
            variant="ghost"
            onClick={() => onRemove(index)}
          />
        </HStack>
      </HStack>

      <Collapse in={isOpen}>
        <Box p={4} pt={0} borderTop="1px" borderColor="gray.100">
          <VStack spacing={4} align="stretch">
            <FormControl isRequired>
              <FormLabel fontSize="sm">Etykieta</FormLabel>
              <Input
                size="sm"
                value={field.label}
                onChange={(e) => onUpdate(index, { label: e.target.value })}
              />
            </FormControl>

            <FormControl>
              <FormLabel fontSize="sm">Opis</FormLabel>
              <Textarea
                size="sm"
                rows={2}
                value={field.description || ""}
                onChange={(e) => onUpdate(index, { description: e.target.value })}
              />
            </FormControl>

            <HStack spacing={4} flexWrap="wrap">
              <Checkbox
                isChecked={field.required}
                onChange={(e) => onUpdate(index, { required: e.target.checked })}
              >
                Wymagane
              </Checkbox>
              <Checkbox
                isChecked={field.visible}
                onChange={(e) => onUpdate(index, { visible: e.target.checked })}
              >
                Widoczne
              </Checkbox>
              <Checkbox
                isChecked={field.sortable}
                onChange={(e) => onUpdate(index, { sortable: e.target.checked })}
              >
                Sortowalne
              </Checkbox>
              <Checkbox
                isChecked={field.filterable}
                onChange={(e) => onUpdate(index, { filterable: e.target.checked })}
              >
                Filtrowalne
              </Checkbox>
              <Checkbox
                isChecked={field.summable}
                onChange={(e) => onUpdate(index, { summable: e.target.checked })}
                isDisabled={field.type !== 3 && field.type !== 4 && field.type !== 6}
              >
                Sumowalne {(field.type !== 3 && field.type !== 4 && field.type !== 6) && <Text as="span" fontSize="xs" color="gray.500">(tylko ValueNet/ValueGross/TotalVat)</Text>}
              </Checkbox>
            </HStack>

            {field.summable && (
              <FormControl>
                <FormLabel fontSize="sm">Zakres sumowania</FormLabel>
                <Select
                  size="sm"
                  value={field.summaryScope ?? SummaryScope.Both}
                  onChange={(e) =>
                    onUpdate(index, { summaryScope: parseInt(e.target.value) as SummaryScope })
                  }
                >
                  {Object.entries(summaryScopeLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </Select>
              </FormControl>
            )}

            <HStack spacing={3}>
              <FormControl>
                <FormLabel fontSize="sm">Jednostka</FormLabel>
                <Input
                  size="sm"
                  value={field.unit || ""}
                  onChange={(e) => onUpdate(index, { unit: e.target.value })}
                  placeholder="np. m², szt, mb"
                />
              </FormControl>
              <FormControl>
                <FormLabel fontSize="sm">Format</FormLabel>
                <Input
                  size="sm"
                  value={field.displayFormat || ""}
                  onChange={(e) => onUpdate(index, { displayFormat: e.target.value })}
                  placeholder="N2, C"
                />
              </FormControl>
            </HStack>

            <Checkbox
              isChecked={field.autoCalculated}
              onChange={(e) => onUpdate(index, { autoCalculated: e.target.checked })}
            >
              Obliczane automatycznie
            </Checkbox>

            {field.autoCalculated && (
              <FormControl>
                <FormLabel fontSize="sm">Formuła</FormLabel>
                <Input
                  size="sm"
                  value={field.calculationFormula || ""}
                  onChange={(e) => onUpdate(index, { calculationFormula: e.target.value })}
                  placeholder="unitPriceNet * quantity"
                />
              </FormControl>
            )}
          </VStack>
        </Box>
      </Collapse>
    </Box>
  );
}

interface GenericFieldsEditorProps {
  fields: GenericFieldDefinition[];
  onAdd: (type: GenericFieldType) => void;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<GenericFieldDefinition>) => void;
}

function GenericFieldsEditor({
  fields,
  onAdd,
  onRemove,
  onUpdate,
}: GenericFieldsEditorProps) {
  return (
    <VStack spacing={4} align="stretch">
      <Box bg="green.50" p={4} borderRadius="md">
        <Text fontSize="sm" fontWeight="bold" mb={3}>
          Dodaj pole generyczne:
        </Text>
        <HStack spacing={2} flexWrap="wrap">
          {Object.entries(genericFieldTypeLabels).map(([type, label]) => (
            <Button
              key={type}
              size="sm"
              leftIcon={<Plus size={14} />}
              colorScheme="green"
              variant="outline"
              onClick={() => onAdd(parseInt(type) as GenericFieldType)}
            >
              {label}
            </Button>
          ))}
        </HStack>
      </Box>

      {fields.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak pól generycznych</Text>
        </Box>
      ) : (
        <VStack spacing={3} align="stretch">
          {fields.map((field, index) => (
            <GenericFieldEditor
              key={index}
              field={field}
              index={index}
              totalFields={fields.length}
              onRemove={onRemove}
              onUpdate={onUpdate}
            />
          ))}
        </VStack>
      )}
    </VStack>
  );
}

interface GenericFieldEditorProps {
  field: GenericFieldDefinition;
  index: number;
  totalFields: number;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<GenericFieldDefinition>) => void;
}

function GenericFieldEditor({
  field,
  index,
  totalFields,
  onRemove,
  onUpdate,
}: GenericFieldEditorProps) {
  const { isOpen, onToggle } = useDisclosure({ defaultIsOpen: false });

  return (
    <Box borderWidth="1px" borderRadius="md" bg="white" shadow="sm">
      <HStack p={4} justify="space-between">
        <HStack spacing={3} flex={1}>
          <Badge colorScheme="blue">#{field.order + 1}</Badge>
          <Text fontWeight="medium">{field.label}</Text>
          <Badge colorScheme="green">{genericFieldTypeLabels[field.type]}</Badge>
        </HStack>
        <HStack spacing={2}>
          <IconButton
            aria-label="Rozwiń"
            icon={isOpen ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
            size="sm"
            variant="ghost"
            onClick={onToggle}
          />
          <IconButton
            aria-label="Usuń"
            icon={<Trash2 size={16} />}
            size="sm"
            colorScheme="red"
            variant="ghost"
            onClick={() => onRemove(index)}
          />
        </HStack>
      </HStack>

      <Collapse in={isOpen}>
        <Box p={4} pt={0} borderTop="1px" borderColor="gray.100">
          <VStack spacing={4} align="stretch">
            <FormControl isRequired>
              <FormLabel fontSize="sm">Etykieta</FormLabel>
              <Input
                size="sm"
                value={field.label}
                onChange={(e) => onUpdate(index, { label: e.target.value })}
              />
            </FormControl>

            <FormControl>
              <FormLabel fontSize="sm">Opis</FormLabel>
              <Textarea
                size="sm"
                rows={2}
                value={field.description || ""}
                onChange={(e) => onUpdate(index, { description: e.target.value })}
              />
            </FormControl>

            <HStack spacing={4} flexWrap="wrap">
              <Checkbox
                isChecked={field.required}
                onChange={(e) => onUpdate(index, { required: e.target.checked })}
              >
                Wymagane
              </Checkbox>
              <Checkbox
                isChecked={field.visible}
                onChange={(e) => onUpdate(index, { visible: e.target.checked })}
              >
                Widoczne
              </Checkbox>
              <Checkbox
                isChecked={field.sortable}
                onChange={(e) => onUpdate(index, { sortable: e.target.checked })}
              >
                Sortowalne
              </Checkbox>
              <Checkbox
                isChecked={field.filterable}
                onChange={(e) => onUpdate(index, { filterable: e.target.checked })}
              >
                Filtrowalne
              </Checkbox>
            </HStack>

            {(field.type === GenericFieldType.Integer || field.type === GenericFieldType.Decimal) && (
              <HStack spacing={3}>
                <FormControl>
                  <FormLabel fontSize="sm">Min</FormLabel>
                  <NumberInput
                    size="sm"
                    value={field.minValue ?? ""}
                    onChange={(_, value) =>
                      onUpdate(index, { minValue: isNaN(value) ? undefined : value })
                    }
                  >
                    <NumberInputField />
                  </NumberInput>
                </FormControl>
                <FormControl>
                  <FormLabel fontSize="sm">Max</FormLabel>
                  <NumberInput
                    size="sm"
                    value={field.maxValue ?? ""}
                    onChange={(_, value) =>
                      onUpdate(index, { maxValue: isNaN(value) ? undefined : value })
                    }
                  >
                    <NumberInputField />
                  </NumberInput>
                </FormControl>
              </HStack>
            )}

            {field.type === GenericFieldType.String && (
              <>
                <HStack spacing={3}>
                  <FormControl>
                    <FormLabel fontSize="sm">Min długość</FormLabel>
                    <NumberInput
                      size="sm"
                      min={0}
                      value={field.minLength ?? ""}
                      onChange={(_, value) =>
                        onUpdate(index, { minLength: isNaN(value) ? undefined : value })
                      }
                    >
                      <NumberInputField />
                    </NumberInput>
                  </FormControl>
                  <FormControl>
                    <FormLabel fontSize="sm">Max długość</FormLabel>
                    <NumberInput
                      size="sm"
                      min={0}
                      value={field.maxLength ?? ""}
                      onChange={(_, value) =>
                        onUpdate(index, { maxLength: isNaN(value) ? undefined : value })
                      }
                    >
                      <NumberInputField />
                    </NumberInput>
                  </FormControl>
                </HStack>

                <FormControl>
                  <FormLabel fontSize="sm">Dozwolone wartości (oddzielone przecinkami)</FormLabel>
                  <Input
                    size="sm"
                    value={field.allowedValues?.join(", ") || ""}
                    onChange={(e) => {
                      const values = e.target.value
                        .split(",")
                        .map((v) => v.trim())
                        .filter((v) => v);
                      onUpdate(index, {
                        allowedValues: values.length > 0 ? values : undefined,
                      });
                    }}
                  />
                </FormControl>
              </>
            )}

            <FormControl>
              <FormLabel fontSize="sm">Placeholder</FormLabel>
              <Input
                size="sm"
                value={field.placeholder || ""}
                onChange={(e) => onUpdate(index, { placeholder: e.target.value })}
              />
            </FormControl>
          </VStack>
        </Box>
      </Collapse>
    </Box>
  );
}

interface ValidationRulesEditorProps {
  rules: CrossFieldValidationRule[];
  onAdd: () => void;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<CrossFieldValidationRule>) => void;
}

function ValidationRulesEditor({ rules, onAdd, onRemove, onUpdate }: ValidationRulesEditorProps) {
  return (
    <VStack spacing={4} align="stretch">
      <Box bg="orange.50" p={4} borderRadius="md">
        <HStack justify="space-between">
          <Text fontSize="sm" fontWeight="bold">
            Reguły walidacji krzyżowej między polami
          </Text>
          <Button size="sm" leftIcon={<Plus size={14} />} colorScheme="orange" onClick={onAdd}>
            Dodaj regułę
          </Button>
        </HStack>
      </Box>

      {rules.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak reguł walidacji</Text>
        </Box>
      ) : (
        <VStack spacing={3} align="stretch">
          {rules.map((rule, index) => (
            <Box key={index} p={4} borderWidth="1px" borderRadius="md" bg="white" shadow="sm">
              <VStack spacing={3} align="stretch">
                <HStack justify="space-between">
                  <Badge colorScheme="orange">Reguła #{index + 1}</Badge>
                  <HStack spacing={2}>
                    <Checkbox
                      isChecked={rule.isActive}
                      onChange={(e) => onUpdate(index, { isActive: e.target.checked })}
                    >
                      Aktywna
                    </Checkbox>
                    <IconButton
                      aria-label="Usuń"
                      icon={<Trash2 size={16} />}
                      size="sm"
                      colorScheme="red"
                      variant="ghost"
                      onClick={() => onRemove(index)}
                    />
                  </HStack>
                </HStack>

                <FormControl isRequired>
                  <FormLabel fontSize="sm">Nazwa reguły</FormLabel>
                  <Input
                    size="sm"
                    value={rule.ruleName}
                    onChange={(e) => onUpdate(index, { ruleName: e.target.value })}
                    placeholder="np. date_range_validation"
                  />
                </FormControl>

                <FormControl isRequired>
                  <FormLabel fontSize="sm">Wyrażenie walidacji</FormLabel>
                  <Input
                    size="sm"
                    value={rule.expression}
                    onChange={(e) => onUpdate(index, { expression: e.target.value })}
                    placeholder="np. endDate >= startDate"
                  />
                  <FormHelperText fontSize="xs">
                    Użyj nazw pól i operatorów logicznych
                  </FormHelperText>
                </FormControl>

                <FormControl isRequired>
                  <FormLabel fontSize="sm">Komunikat błędu</FormLabel>
                  <Textarea
                    size="sm"
                    rows={2}
                    value={rule.errorMessage}
                    onChange={(e) => onUpdate(index, { errorMessage: e.target.value })}
                    placeholder="Komunikat wyświetlany gdy walidacja nie powiedzie się"
                  />
                </FormControl>
              </VStack>
            </Box>
          ))}
        </VStack>
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

interface NestedFieldsEditorProps {
  field: GenericFieldDefinition;
  onUpdate: (updates: Partial<GenericFieldDefinition>) => void;
}

function NestedFieldsEditor({ field, onUpdate }: NestedFieldsEditorProps) {
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);
  const [draggedType, setDraggedType] = useState<'calculated' | 'generic' | null>(null);

  // Obsługa przeciągania na urządzeniach dotykowych
  const { createTouchHandlers: createNestedTouchHandlers } = useTouchReorder({ itemSelector: '[data-touch-draggable-nested]' });
  
  const nestedCalculatedFields = field.nestedFields?.calculatedFields ?? [];
  const nestedGenericFields = field.nestedFields?.genericFields ?? [];

  // Połącz wszystkie pola dla drag & drop
  const allNestedFields = [
    ...nestedCalculatedFields.map(f => ({ ...f, fieldType: 'calculated' as const })),
    ...nestedGenericFields.map(f => ({ ...f, fieldType: 'generic' as const }))
  ].sort((a, b) => a.order - b.order);

  const handleDragStart = (index: number, type: 'calculated' | 'generic') => {
    setDraggedIndex(index);
    setDraggedType(type);
  };

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault();
    if (draggedIndex === null || draggedIndex === index || !draggedType) return;

    const reorderedFields = [...allNestedFields];
    const draggedField = reorderedFields[draggedIndex];
    reorderedFields.splice(draggedIndex, 1);
    reorderedFields.splice(index, 0, draggedField);
    
    // Zaktualizuj order
    reorderedFields.forEach((f, idx) => {
      f.order = idx;
    });

    // Podziel z powrotem na calculated i generic
    const updatedCalculated = reorderedFields
      .filter(f => f.fieldType === 'calculated')
      .map(({ fieldType, ...rest }) => rest as CalculatedFieldDefinition);
    const updatedGeneric = reorderedFields
      .filter(f => f.fieldType === 'generic')
      .map(({ fieldType, ...rest }) => rest as GenericFieldDefinition);

    onUpdate({
      nestedFields: {
        isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
        enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
        ...field.nestedFields,
        calculatedFields: updatedCalculated,
        genericFields: updatedGeneric,
      },
    });
    
    setDraggedIndex(index);
  };

  const handleDragEnd = () => {
    setDraggedIndex(null);
    setDraggedType(null);
  };

  const handleAddNestedCalculatedField = (type: CalculatedFieldType) => {
    const isSummable = type === CalculatedFieldType.ValueNet ||
                       type === CalculatedFieldType.ValueGross ||
                       type === CalculatedFieldType.TotalVat;
    
    const newField: CalculatedFieldDefinition = {
      name: `nested_calc_${nestedCalculatedFields.length + 1}`,
      label: calculatedFieldTypeLabels[type],
      type: type,
      order: nestedCalculatedFields.length + nestedGenericFields.length,
      required: false,
      visible: true,
      sortable: true,
      filterable: true,
      summable: isSummable,
      summaryScope: SummaryScope.Both,
      autoCalculated: [
        CalculatedFieldType.UnitPriceGross,
        CalculatedFieldType.ValueNet,
        CalculatedFieldType.ValueGross,
        CalculatedFieldType.UnitVat,
        CalculatedFieldType.TotalVat,
      ].includes(type),
      readOnly: [
        CalculatedFieldType.UnitPriceGross,
        CalculatedFieldType.ValueNet,
        CalculatedFieldType.ValueGross,
        CalculatedFieldType.UnitVat,
        CalculatedFieldType.TotalVat,
      ].includes(type),
    };

    onUpdate({
      nestedFields: {
        isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
        enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
        ...field.nestedFields,
        calculatedFields: [...nestedCalculatedFields, newField],
      },
    });
  };

  const handleRemoveNestedCalculatedField = (fieldIndex: number) => {
    const updatedFields = nestedCalculatedFields.filter((_, i) => i !== fieldIndex);
    onUpdate({
      nestedFields: {
        isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
        enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
        ...field.nestedFields,
        calculatedFields: updatedFields,
      },
    });
  };

  const handleUpdateNestedCalculatedField = (
    fieldIndex: number,
    updates: Partial<CalculatedFieldDefinition>
  ) => {
    const updatedFields = [...nestedCalculatedFields];
    updatedFields[fieldIndex] = { ...updatedFields[fieldIndex], ...updates };
    onUpdate({
      nestedFields: {
        isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
        enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
        ...field.nestedFields,
        calculatedFields: updatedFields,
      },
    });
  };

  const handleAddNestedGenericField = (type: GenericFieldType) => {
    const newField: GenericFieldDefinition = {
      name: `nested_gen_${nestedGenericFields.length + 1}`,
      label: genericFieldTypeLabels[type],
      type: type,
      order: nestedCalculatedFields.length + nestedGenericFields.length,
      required: false,
      visible: true,
      sortable: true,
      filterable: true,
    };

    onUpdate({
      nestedFields: {
        isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
        enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
        ...field.nestedFields,
        genericFields: [...nestedGenericFields, newField],
      },
    });
  };

  const handleRemoveNestedGenericField = (fieldIndex: number) => {
    const updatedFields = nestedGenericFields.filter((_, i) => i !== fieldIndex);
    onUpdate({
      nestedFields: {
        isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
        enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
        ...field.nestedFields,
        genericFields: updatedFields,
      },
    });
  };

  const handleUpdateNestedGenericField = (
    fieldIndex: number,
    updates: Partial<GenericFieldDefinition>
  ) => {
    const updatedFields = [...nestedGenericFields];
    updatedFields[fieldIndex] = { ...updatedFields[fieldIndex], ...updates };
    onUpdate({
      nestedFields: {
        isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
        enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
        ...field.nestedFields,
        genericFields: updatedFields,
      },
    });
  };

  return (
    <VStack spacing={4} align="stretch">
      <Text fontSize="sm" fontWeight="bold" color="gray.700">
        Zagnieżdżone pola w kolekcji
      </Text>

      {/* Nested Calculated Fields */}
      <Box>
        <HStack justify="space-between" mb={2}>
          <Text fontSize="sm" fontWeight="bold" color="purple.600">
            Pola obliczeniowe ({nestedCalculatedFields.length})
          </Text>
        </HStack>

        <HStack spacing={2} flexWrap="wrap" mb={3}>
          {Object.entries(calculatedFieldTypeLabels).map(([type, label]) => {
            const typeNum = parseInt(type) as CalculatedFieldType;
            const isAdded = nestedCalculatedFields.some((f) => f.type === typeNum);
            return (
              <Button
                key={type}
                size="xs"
                leftIcon={<Plus size={12} />}
                colorScheme="purple"
                variant={isAdded ? "solid" : "outline"}
                onClick={() => handleAddNestedCalculatedField(typeNum)}
                isDisabled={isAdded}
              >
                {label}
              </Button>
            );
          })}
        </HStack>

        {nestedCalculatedFields.length > 0 && (
          <VStack spacing={2} align="stretch">
            {nestedCalculatedFields.map((nestedField, nestedIndex) => (
              <Box
                key={nestedIndex}
                p={3}
                borderWidth="1px"
                borderRadius="md"
                bg="purple.50"
                borderColor="purple.200"
              >
                <HStack justify="space-between" mb={2}>
                  <HStack spacing={2}>
                    <Badge colorScheme="purple" fontSize="xs">
                      {calculatedFieldTypeLabels[nestedField.type]}
                    </Badge>
                    <Text fontSize="xs" fontWeight="medium">
                      {nestedField.label}
                    </Text>
                  </HStack>
                  <IconButton
                    aria-label="Usuń"
                    icon={<Trash2 size={12} />}
                    size="xs"
                    colorScheme="red"
                    variant="ghost"
                    onClick={() => handleRemoveNestedCalculatedField(nestedIndex)}
                  />
                </HStack>

                <VStack spacing={2} align="stretch">
                  <HStack spacing={2}>
                    <FormControl size="sm">
                      <FormLabel fontSize="xs">Nazwa</FormLabel>
                      <Input
                        size="xs"
                        value={nestedField.name}
                        onChange={(e) =>
                          handleUpdateNestedCalculatedField(nestedIndex, { name: e.target.value })
                        }
                      />
                    </FormControl>
                    <FormControl size="sm">
                      <FormLabel fontSize="xs">Etykieta</FormLabel>
                      <Input
                        size="xs"
                        value={nestedField.label}
                        onChange={(e) =>
                          handleUpdateNestedCalculatedField(nestedIndex, { label: e.target.value })
                        }
                      />
                    </FormControl>
                  </HStack>

                  <HStack spacing={2} flexWrap="wrap">
                    <Checkbox
                      size="sm"
                      isChecked={nestedField.required}
                      onChange={(e) =>
                        handleUpdateNestedCalculatedField(nestedIndex, { required: e.target.checked })
                      }
                    >
                      <Text fontSize="xs">Wymagane</Text>
                    </Checkbox>
                    <Checkbox
                      size="sm"
                      isChecked={nestedField.summable}
                      onChange={(e) =>
                        handleUpdateNestedCalculatedField(nestedIndex, { summable: e.target.checked })
                      }
                      isDisabled={nestedField.type !== 3 && nestedField.type !== 4 && nestedField.type !== 6}
                    >
                      <Text fontSize="xs">Sumowalne {(nestedField.type !== 3 && nestedField.type !== 4 && nestedField.type !== 6) && <Text as="span" fontSize="2xs" color="gray.500">(tylko ValueNet/ValueGross/TotalVat)</Text>}</Text>
                    </Checkbox>
                    <Checkbox
                      size="sm"
                      isChecked={nestedField.autoCalculated}
                      onChange={(e) =>
                        handleUpdateNestedCalculatedField(nestedIndex, {
                          autoCalculated: e.target.checked,
                        })
                      }
                    >
                      <Text fontSize="xs">Auto-kalkulacja</Text>
                    </Checkbox>
                  </HStack>

                  {nestedField.autoCalculated && (
                    <FormControl size="sm">
                      <FormLabel fontSize="xs">Formuła</FormLabel>
                      <Input
                        size="xs"
                        value={nestedField.calculationFormula || ""}
                        onChange={(e) =>
                          handleUpdateNestedCalculatedField(nestedIndex, {
                            calculationFormula: e.target.value,
                          })
                        }
                        placeholder="np. unitPriceNet * quantity"
                      />
                    </FormControl>
                  )}
                </VStack>
              </Box>
            ))}
          </VStack>
        )}
      </Box>

      <Divider />

      {/* Nested Generic Fields */}
      <Box>
        <HStack justify="space-between" mb={2}>
          <Text fontSize="sm" fontWeight="bold" color="green.600">
            Pola generyczne ({nestedGenericFields.length})
          </Text>
        </HStack>

        <HStack spacing={2} flexWrap="wrap" mb={3}>
          {Object.entries(genericFieldTypeLabels)
            .map(([type, label]) => (
              <Button
                key={type}
                size="xs"
                leftIcon={<Plus size={12} />}
                colorScheme="green"
                variant="outline"
                onClick={() => handleAddNestedGenericField(parseInt(type) as GenericFieldType)}
              >
                {label}
              </Button>
            ))}
        </HStack>

        {nestedGenericFields.length > 0 && (
          <VStack spacing={2} align="stretch">
            {nestedGenericFields.map((nestedField, nestedIndex) => (
              <Box
                key={nestedIndex}
                p={3}
                borderWidth="1px"
                borderRadius="md"
                bg="green.50"
                borderColor="green.200"
              >
                <HStack justify="space-between" mb={2}>
                  <HStack spacing={2}>
                    <Badge colorScheme="green" fontSize="xs">
                      {genericFieldTypeLabels[nestedField.type]}
                    </Badge>
                    <Text fontSize="xs" fontWeight="medium">
                      {nestedField.label}
                    </Text>
                  </HStack>
                  <IconButton
                    aria-label="Usuń"
                    icon={<Trash2 size={12} />}
                    size="xs"
                    colorScheme="red"
                    variant="ghost"
                    onClick={() => handleRemoveNestedGenericField(nestedIndex)}
                  />
                </HStack>

                <VStack spacing={2} align="stretch">
                  <HStack spacing={2}>
                    <FormControl size="sm">
                      <FormLabel fontSize="xs">Nazwa</FormLabel>
                      <Input
                        size="xs"
                        value={nestedField.name}
                        onChange={(e) =>
                          handleUpdateNestedGenericField(nestedIndex, { name: e.target.value })
                        }
                      />
                    </FormControl>
                    <FormControl size="sm">
                      <FormLabel fontSize="xs">Etykieta</FormLabel>
                      <Input
                        size="xs"
                        value={nestedField.label}
                        onChange={(e) =>
                          handleUpdateNestedGenericField(nestedIndex, { label: e.target.value })
                        }
                      />
                    </FormControl>
                  </HStack>

                  <HStack spacing={2} flexWrap="wrap">
                    <Checkbox
                      size="sm"
                      isChecked={nestedField.required}
                      onChange={(e) =>
                        handleUpdateNestedGenericField(nestedIndex, { required: e.target.checked })
                      }
                    >
                      <Text fontSize="xs">Wymagane</Text>
                    </Checkbox>
                    <Checkbox
                      size="sm"
                      isChecked={nestedField.sortable}
                      onChange={(e) =>
                        handleUpdateNestedGenericField(nestedIndex, { sortable: e.target.checked })
                      }
                    >
                      <Text fontSize="xs">Sortowalne</Text>
                    </Checkbox>
                  </HStack>

                  {(nestedField.type === GenericFieldType.Integer ||
                    nestedField.type === GenericFieldType.Decimal) && (
                    <HStack spacing={2}>
                      <FormControl size="sm">
                        <FormLabel fontSize="xs">Min</FormLabel>
                        <NumberInput
                          size="xs"
                          value={nestedField.minValue ?? ""}
                          onChange={(_, value) =>
                            handleUpdateNestedGenericField(nestedIndex, {
                              minValue: isNaN(value) ? undefined : value,
                            })
                          }
                        >
                          <NumberInputField />
                        </NumberInput>
                      </FormControl>
                      <FormControl size="sm">
                        <FormLabel fontSize="xs">Max</FormLabel>
                        <NumberInput
                          size="xs"
                          value={nestedField.maxValue ?? ""}
                          onChange={(_, value) =>
                            handleUpdateNestedGenericField(nestedIndex, {
                              maxValue: isNaN(value) ? undefined : value,
                            })
                          }
                        >
                          <NumberInputField />
                        </NumberInput>
                      </FormControl>
                    </HStack>
                  )}

                  {nestedField.type === GenericFieldType.String && (
                    <>
                      <HStack spacing={2}>
                        <FormControl size="sm">
                          <FormLabel fontSize="xs">Min długość</FormLabel>
                          <NumberInput
                            size="xs"
                            min={0}
                            value={nestedField.minLength ?? ""}
                            onChange={(_, value) =>
                              handleUpdateNestedGenericField(nestedIndex, {
                                minLength: isNaN(value) ? undefined : value,
                              })
                            }
                          >
                            <NumberInputField />
                          </NumberInput>
                        </FormControl>
                        <FormControl size="sm">
                          <FormLabel fontSize="xs">Max długość</FormLabel>
                          <NumberInput
                            size="xs"
                            min={0}
                            value={nestedField.maxLength ?? ""}
                            onChange={(_, value) =>
                              handleUpdateNestedGenericField(nestedIndex, {
                                maxLength: isNaN(value) ? undefined : value,
                              })
                            }
                          >
                            <NumberInputField />
                          </NumberInput>
                        </FormControl>
                      </HStack>

                      <FormControl size="sm">
                        <FormLabel fontSize="xs">Dozwolone wartości (przecinki)</FormLabel>
                        <Input
                          size="xs"
                          value={nestedField.allowedValues?.join(", ") || ""}
                          onChange={(e) => {
                            const values = e.target.value
                              .split(",")
                              .map((v) => v.trim())
                              .filter((v) => v);
                            handleUpdateNestedGenericField(nestedIndex, {
                              allowedValues: values.length > 0 ? values : undefined,
                            });
                          }}
                        />
                      </FormControl>
                    </>
                  )}

                  <FormControl size="sm">
                    <FormLabel fontSize="xs">Placeholder</FormLabel>
                    <Input
                      size="xs"
                      value={nestedField.placeholder || ""}
                      onChange={(e) =>
                        handleUpdateNestedGenericField(nestedIndex, { placeholder: e.target.value })
                      }
                    />
                  </FormControl>
                </VStack>
              </Box>
            ))}
          </VStack>
        )}
      </Box>

      {/* Kolejność pól zagnieżdżonych */}
      {allNestedFields.length > 1 && (
        <>
          <Divider />
          <Box bg="orange.50" p={4} borderRadius="md" borderWidth="1px" borderColor="orange.200">
            <HStack spacing={2} mb={2}>
              <Icon as={Layout} color="orange.600" />
              <Text fontSize="sm" fontWeight="bold" color="orange.800">
                Kolejność pół w kolekcji
              </Text>
            </HStack>
            <Text fontSize="xs" color="gray.700" mb={3}>
              Przeciągnij i upuść pola, aby zmienić kolejność wyświetlania w kolekcji.
            </Text>

            <VStack spacing={2} align="stretch">
              {allNestedFields.map((nestedField, index) => (
                <HStack
                  key={`${nestedField.fieldType}-${nestedField.name}`}
                  p={2}
                  bg={draggedIndex === index ? 'orange.100' : 'white'}
                  borderRadius="md"
                  borderWidth="2px"
                  borderColor={draggedIndex === index ? 'orange.400' : 'gray.200'}
                  cursor="grab"
                  _hover={{ bg: draggedIndex === index ? 'orange.100' : 'gray.50', borderColor: 'orange.300' }}
                  _active={{ cursor: 'grabbing' }}
                  draggable
                  onDragStart={() => handleDragStart(index, nestedField.fieldType)}
                  onDragOver={(e) => handleDragOver(e, index)}
                  onDragEnd={handleDragEnd}
                  {...createNestedTouchHandlers(index, draggedIndex, setDraggedIndex, (from, to) => {
                    const reorderedFields = [...allNestedFields];
                    const [moved] = reorderedFields.splice(from, 1);
                    reorderedFields.splice(to, 0, moved);
                    reorderedFields.forEach((f, idx) => { f.order = idx; });
                    const updatedCalc = reorderedFields
                      .filter(f => f.fieldType === 'calculated')
                      .map(({ fieldType, ...rest }) => rest as CalculatedFieldDefinition);
                    const updatedGen = reorderedFields
                      .filter(f => f.fieldType === 'generic')
                      .map(({ fieldType, ...rest }) => rest as GenericFieldDefinition);
                    onUpdate({
                      nestedFields: {
                        isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
                        enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
                        ...field.nestedFields,
                        calculatedFields: updatedCalc,
                        genericFields: updatedGen,
                      },
                    });
                  })}
                  data-touch-draggable-nested
                  transition="all 0.2s"
                  spacing={3}
                >
                  <Icon as={GripVertical} color="gray.400" size={14} />
                  <Badge 
                    colorScheme={nestedField.fieldType === 'calculated' ? 'purple' : 'green'} 
                    fontSize="xs"
                    minW="70px"
                  >
                    {nestedField.fieldType === 'calculated' ? 'Oblic.' : 'Gen.'}
                  </Badge>
                  <Text fontSize="xs" fontWeight="medium" flex="1">
                    {nestedField.label}
                  </Text>
                </HStack>
              ))}
            </VStack>
          </Box>
        </>
      )}
    </VStack>
  );
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
          Pola do sumowania w grupach
        </Text>
        <Text fontSize="sm" color="gray.600" mb={4}>
          Wybierz pola które mają być sumowane w podsumowaniu grup. Pozostaw puste aby nie sumować żadnych pól.
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
          <Text fontSize="xs" color="blue.600" mt={3}>
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
          <Text fontSize="xs" color="blue.600" mt={3}>
            ✓ Wybrano {totalSummaryFields.length} {totalSummaryFields.length === 1 ? 'pole' : 'pól'} do sumowania
          </Text>
        )}
      </Box>
    </VStack>
  );
};

// ======================== CURRENCIES EDITOR ========================

interface CurrenciesEditorProps {
  currencies: Array<{ code: string; name: string; symbol?: string; isDefault: boolean; order: number }>;
  onChange: (currencies: Array<{ code: string; name: string; symbol?: string; isDefault: boolean; order: number }>) => void;
}

function CurrenciesEditor({ currencies, onChange }: CurrenciesEditorProps) {
  const handleAdd = () => {
    const newCurrency = {
      code: '',
      name: '',
      symbol: '',
      isDefault: currencies.length === 0,
      order: currencies.length,
    };
    onChange([...currencies, newCurrency]);
  };

  const handleRemove = (index: number) => {
    const updated = currencies.filter((_, i) => i !== index);
    // Jeśli usunięto domyślną walutę i są inne, ustaw pierwszą jako domyślną
    if (currencies[index].isDefault && updated.length > 0) {
      updated[0].isDefault = true;
    }
    // Zaktualizuj order
    updated.forEach((c, i) => { c.order = i; });
    onChange(updated);
  };

  const handleUpdate = (index: number, updates: Partial<typeof currencies[0]>) => {
    const updated = [...currencies];
    updated[index] = { ...updated[index], ...updates };
    
    // Jeśli ustawiono isDefault na true, wyłącz dla innych
    if (updates.isDefault === true) {
      updated.forEach((c, i) => {
        if (i !== index) c.isDefault = false;
      });
    }
    
    onChange(updated);
  };

  const handleMoveUp = (index: number) => {
    if (index === 0) return;
    const updated = [...currencies];
    [updated[index - 1], updated[index]] = [updated[index], updated[index - 1]];
    updated.forEach((c, i) => { c.order = i; });
    onChange(updated);
  };

  const handleMoveDown = (index: number) => {
    if (index === currencies.length - 1) return;
    const updated = [...currencies];
    [updated[index], updated[index + 1]] = [updated[index + 1], updated[index]];
    updated.forEach((c, i) => { c.order = i; });
    onChange(updated);
  };

  return (
    <VStack spacing={4} align="stretch">
      <Box bg="blue.50" p={4} borderRadius="md" borderWidth="1px" borderColor="blue.200">
        <HStack spacing={2} mb={2}>
          <Icon as={DollarSign} color="blue.600" />
          <Text fontSize="sm" fontWeight="bold" color="blue.800">
            Waluty dostępne w szablonie
          </Text>
        </HStack>
        <Text fontSize="xs" color="gray.700">
          Zdefiniuj waluty, które będą dostępne przy tworzeniu kosztorysów z tego szablonu. Możesz dodać wiele walut (PLN, EUR, USD, etc.).
        </Text>
      </Box>

      <Button
        leftIcon={<Plus size={16} />}
        size="sm"
        colorScheme="blue"
        variant="outline"
        onClick={handleAdd}
      >
        Dodaj walutę
      </Button>

      {currencies.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak walut. Dodaj przynajmniej jedną walutę.</Text>
        </Box>
      ) : (
        <VStack spacing={2} align="stretch">
          {currencies.map((currency, index) => (
            <Box
              key={index}
              p={4}
              bg="white"
              borderRadius="md"
              borderWidth="1px"
              borderColor="gray.200"
              shadow="sm"
            >
              <VStack spacing={3} align="stretch">
                <HStack justify="space-between">
                  <HStack spacing={2}>
                    <Badge colorScheme="blue">#{currency.order + 1}</Badge>
                    {currency.isDefault && <Badge colorScheme="green">Domyślna</Badge>}
                  </HStack>
                  <HStack spacing={1}>
                    <IconButton
                      aria-label="Przenieś w górę"
                      icon={<ChevronUp size={16} />}
                      size="xs"
                      variant="ghost"
                      onClick={() => handleMoveUp(index)}
                      isDisabled={index === 0}
                    />
                    <IconButton
                      aria-label="Przenieś w dół"
                      icon={<ChevronDown size={16} />}
                      size="xs"
                      variant="ghost"
                      onClick={() => handleMoveDown(index)}
                      isDisabled={index === currencies.length - 1}
                    />
                    <IconButton
                      aria-label="Usuń"
                      icon={<Trash2 size={16} />}
                      size="xs"
                      colorScheme="red"
                      variant="ghost"
                      onClick={() => handleRemove(index)}
                    />
                  </HStack>
                </HStack>

                <HStack spacing={3}>
                  <FormControl isRequired flex={1}>
                    <FormLabel fontSize="xs">Kod (np. PLN, EUR, USD)</FormLabel>
                    <Input
                      size="sm"
                      value={currency.code}
                      onChange={(e) => handleUpdate(index, { code: e.target.value.toUpperCase() })}
                      placeholder="PLN"
                      maxLength={3}
                    />
                  </FormControl>

                  <FormControl isRequired flex={2}>
                    <FormLabel fontSize="xs">Nazwa waluty</FormLabel>
                    <Input
                      size="sm"
                      value={currency.name}
                      onChange={(e) => handleUpdate(index, { name: e.target.value })}
                      placeholder="Polski złoty"
                    />
                  </FormControl>

                  <FormControl flex={1}>
                    <FormLabel fontSize="xs">Symbol</FormLabel>
                    <Input
                      size="sm"
                      value={currency.symbol || ''}
                      onChange={(e) => handleUpdate(index, { symbol: e.target.value })}
                      placeholder="zł"
                    />
                  </FormControl>
                </HStack>

                <Checkbox
                  isChecked={currency.isDefault}
                  onChange={(e) => handleUpdate(index, { isDefault: e.target.checked })}
                  size="sm"
                >
                  Ustaw jako domyślną walutę
                </Checkbox>
              </VStack>
            </Box>
          ))}
        </VStack>
      )}
    </VStack>
  );
}

// ======================== UNITS EDITOR ========================

interface UnitsEditorProps {
  units: Array<{ code: string; name: string; symbol: string; category?: string; isDefault: boolean; order: number }>;
  onChange: (units: Array<{ code: string; name: string; symbol: string; category?: string; isDefault: boolean; order: number }>) => void;
}

function UnitsEditor({ units, onChange }: UnitsEditorProps) {
  const handleAdd = () => {
    const newUnit = {
      code: '',
      name: '',
      symbol: '',
      category: '',
      isDefault: units.length === 0,
      order: units.length,
    };
    onChange([...units, newUnit]);
  };

  const handleRemove = (index: number) => {
    const updated = units.filter((_, i) => i !== index);
    // Jeśli usunięto domyślną jednostkę i są inne, ustaw pierwszą jako domyślną
    if (units[index].isDefault && updated.length > 0) {
      updated[0].isDefault = true;
    }
    // Zaktualizuj order
    updated.forEach((u, i) => { u.order = i; });
    onChange(updated);
  };

  const handleUpdate = (index: number, updates: Partial<typeof units[0]>) => {
    const updated = [...units];
    updated[index] = { ...updated[index], ...updates };
    
    // Jeśli ustawiono isDefault na true, wyłącz dla innych
    if (updates.isDefault === true) {
      updated.forEach((u, i) => {
        if (i !== index) u.isDefault = false;
      });
    }
    
    onChange(updated);
  };

  const handleMoveUp = (index: number) => {
    if (index === 0) return;
    const updated = [...units];
    [updated[index - 1], updated[index]] = [updated[index], updated[index - 1]];
    updated.forEach((u, i) => { u.order = i; });
    onChange(updated);
  };

  const handleMoveDown = (index: number) => {
    if (index === units.length - 1) return;
    const updated = [...units];
    [updated[index], updated[index + 1]] = [updated[index + 1], updated[index]];
    updated.forEach((u, i) => { u.order = i; });
    onChange(updated);
  };

  const unitCategories = ['Długość', 'Powierzchnia', 'Objętość', 'Masa', 'Czas', 'Ilość', 'Inne'];

  return (
    <VStack spacing={4} align="stretch">
      <Box bg="green.50" p={4} borderRadius="md" borderWidth="1px" borderColor="green.200">
        <HStack spacing={2} mb={2}>
          <Icon as={Ruler} color="green.600" />
          <Text fontSize="sm" fontWeight="bold" color="green.800">
            Jednostki miary dostępne w szablonie
          </Text>
        </HStack>
        <Text fontSize="xs" color="gray.700">
          Zdefiniuj jednostki miary (szt, m², mb, kg, etc.), które będą dostępne w kosztorysach tworzonych z tego szablonu.
        </Text>
      </Box>

      <Button
        leftIcon={<Plus size={16} />}
        size="sm"
        colorScheme="green"
        variant="outline"
        onClick={handleAdd}
      >
        Dodaj jednostkę
      </Button>

      {units.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak jednostek miar. Dodaj przynajmniej jedną jednostkę.</Text>
        </Box>
      ) : (
        <VStack spacing={2} align="stretch">
          {units.map((unit, index) => (
            <Box
              key={index}
              p={4}
              bg="white"
              borderRadius="md"
              borderWidth="1px"
              borderColor="gray.200"
              shadow="sm"
            >
              <VStack spacing={3} align="stretch">
                <HStack justify="space-between">
                  <HStack spacing={2}>
                    <Badge colorScheme="green">#{unit.order + 1}</Badge>
                    {unit.isDefault && <Badge colorScheme="orange">Domyślna</Badge>}
                    {unit.category && <Badge variant="outline">{unit.category}</Badge>}
                  </HStack>
                  <HStack spacing={1}>
                    <IconButton
                      aria-label="Przenieś w górę"
                      icon={<ChevronUp size={16} />}
                      size="xs"
                      variant="ghost"
                      onClick={() => handleMoveUp(index)}
                      isDisabled={index === 0}
                    />
                    <IconButton
                      aria-label="Przenieś w dół"
                      icon={<ChevronDown size={16} />}
                      size="xs"
                      variant="ghost"
                      onClick={() => handleMoveDown(index)}
                      isDisabled={index === units.length - 1}
                    />
                    <IconButton
                      aria-label="Usuń"
                      icon={<Trash2 size={16} />}
                      size="xs"
                      colorScheme="red"
                      variant="ghost"
                      onClick={() => handleRemove(index)}
                    />
                  </HStack>
                </HStack>

                <HStack spacing={3}>
                  <FormControl isRequired flex={1}>
                    <FormLabel fontSize="xs">Kod (np. szt, m2, kg)</FormLabel>
                    <Input
                      size="sm"
                      value={unit.code}
                      onChange={(e) => handleUpdate(index, { code: e.target.value })}
                      placeholder="szt"
                    />
                  </FormControl>

                  <FormControl isRequired flex={2}>
                    <FormLabel fontSize="xs">Nazwa jednostki</FormLabel>
                    <Input
                      size="sm"
                      value={unit.name}
                      onChange={(e) => handleUpdate(index, { name: e.target.value })}
                      placeholder="sztuka"
                    />
                  </FormControl>

                  <FormControl isRequired flex={1}>
                    <FormLabel fontSize="xs">Symbol wyświetlania</FormLabel>
                    <Input
                      size="sm"
                      value={unit.symbol}
                      onChange={(e) => handleUpdate(index, { symbol: e.target.value })}
                      placeholder="szt"
                    />
                  </FormControl>
                </HStack>

                <HStack spacing={3}>
                  <FormControl flex={1}>
                    <FormLabel fontSize="xs">Kategoria</FormLabel>
                    <Select
                      size="sm"
                      value={unit.category || ''}
                      onChange={(e) => handleUpdate(index, { category: e.target.value || undefined })}
                      placeholder="Wybierz kategorię"
                    >
                      {unitCategories.map((cat) => (
                        <option key={cat} value={cat}>{cat}</option>
                      ))}
                    </Select>
                  </FormControl>

                  <FormControl flex={1} display="flex" alignItems="flex-end">
                    <Checkbox
                      isChecked={unit.isDefault}
                      onChange={(e) => handleUpdate(index, { isDefault: e.target.checked })}
                      size="sm"
                    >
                      Ustaw jako domyślną jednostkę
                    </Checkbox>
                  </FormControl>
                </HStack>
              </VStack>
            </Box>
          ))}
        </VStack>
      )}
    </VStack>
  );
}
