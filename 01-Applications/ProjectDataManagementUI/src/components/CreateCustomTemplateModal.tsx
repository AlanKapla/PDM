import { useState } from "react";
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
  CalculatedFieldType,
  GenericFieldType,
  SummaryScope,
  GroupHeaderFieldType,
} from "../types/costEstimate.types";
import { getDefaultGroupHeaderLabel } from "./FieldRenderer";

interface CreateCustomTemplateModalProps {
  isOpen: boolean;
  onClose: () => void;
  onTemplateCreated: () => void;
  existingTemplate?: {
    id: string;
    name: string;
    description?: string;
    templateStructure: CostEstimateTemplateStructure;
  };
}

const calculatedFieldTypeLabels: Record<CalculatedFieldType, string> = {
  [CalculatedFieldType.UnitPriceNet]: "Cena jednostkowa netto",
  [CalculatedFieldType.VatRate]: "Stawka VAT",
  [CalculatedFieldType.UnitPriceGross]: "Cena jednostkowa brutto",
  [CalculatedFieldType.Quantity]: "Ilość",
  [CalculatedFieldType.ValueNet]: "Wartość netto",
  [CalculatedFieldType.ValueGross]: "Wartość brutto",
  [CalculatedFieldType.UnitVat]: "VAT jednostkowy",
  [CalculatedFieldType.TotalVat]: "VAT całkowity",
};

const genericFieldTypeLabels: Record<GenericFieldType, string> = {
  [GenericFieldType.Integer]: "Liczba całkowita",
  [GenericFieldType.Decimal]: "Liczba dziesiętna",
  [GenericFieldType.String]: "Tekst",
  [GenericFieldType.Boolean]: "Tak/Nie",
  [GenericFieldType.Date]: "Data",
  [GenericFieldType.DateTime]: "Data i czas",
  [GenericFieldType.Collection]: "Kolekcja pól",
};

const groupHeaderFieldTypeLabels: Record<GroupHeaderFieldType, string> = {
  [GroupHeaderFieldType.GroupName]: "Nazwa grupy",
  [GroupHeaderFieldType.GroupDescription]: "Opis grupy",
  [GroupHeaderFieldType.GroupNumber]: "Numer grupy",
  [GroupHeaderFieldType.StartDate]: "Data rozpoczęcia",
  [GroupHeaderFieldType.EndDate]: "Data zakończenia",
  [GroupHeaderFieldType.Status]: "Status",
  [GroupHeaderFieldType.Notes]: "Uwagi",
  [GroupHeaderFieldType.Responsible]: "Odpowiedzialny",
  [GroupHeaderFieldType.Budget]: "Budżet",
  [GroupHeaderFieldType.Priority]: "Priorytet",
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

  // Template Structure State
  const [canAddGroups, setCanAddGroups] = useState(existingTemplate?.templateStructure.canAddGroups ?? true);
  const [canBranchGroups, setCanBranchGroups] = useState(existingTemplate?.templateStructure.canBranchGroups ?? true);
  const [maxGroupLevel, setMaxGroupLevel] = useState<number | undefined>(existingTemplate?.templateStructure.maxGroupLevel);

  // Group Definition State
  const [groupAutoNumbered, setGroupAutoNumbered] = useState(existingTemplate?.templateStructure.groupDefinition.autoNumbered ?? true);
  const [groupNumberFormat, setGroupNumberFormat] = useState(existingTemplate?.templateStructure.groupDefinition.numberFormat ?? "");
  const [headerFields, setHeaderFields] = useState<GroupHeaderFieldDefinition[]>(
    existingTemplate?.templateStructure.groupDefinition.headerFields ?? [
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
  const [calculatedFields, setCalculatedFields] = useState<CalculatedFieldDefinition[]>(
    existingTemplate?.templateStructure.workScopeFieldsDefinition.calculatedFields ?? []
  );
  const [genericFields, setGenericFields] = useState<GenericFieldDefinition[]>(
    existingTemplate?.templateStructure.workScopeFieldsDefinition.genericFields ?? []
  );
  const [validationRules, setValidationRules] = useState<CrossFieldValidationRule[]>(
    existingTemplate?.templateStructure.workScopeFieldsDefinition.crossFieldValidationRules ?? []
  );

  // Summary Configuration State
  const [showGroupSummary, setShowGroupSummary] = useState(existingTemplate?.templateStructure.summaryConfiguration?.showGroupSummary ?? true);
  const [showTotalSummary, setShowTotalSummary] = useState(existingTemplate?.templateStructure.summaryConfiguration?.showTotalSummary ?? true);
  const [groupSummaryFields, setGroupSummaryFields] = useState<string[]>(
    existingTemplate?.templateStructure.summaryConfiguration?.groupSummaryFields ?? []
  );
  const [totalSummaryFields, setTotalSummaryFields] = useState<string[]>(
    existingTemplate?.templateStructure.summaryConfiguration?.totalSummaryFields ?? []
  );

  // UI Configuration State
  const [columnLayout, setColumnLayout] = useState<string[]>(
    existingTemplate?.templateStructure.uiConfiguration?.columnLayout ?? []
  );

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
      name: `field_calc_${calculatedFields.length + 1}`,
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
      name: `field_gen_${genericFields.length + 1}`,
      label: genericFieldTypeLabels[type],
      type: type,
      order: calculatedFields.length + genericFields.length,
      required: false,
      visible: true,
      sortable: type !== GenericFieldType.Collection,
      filterable: type !== GenericFieldType.Collection,
    };
  };

  const handleAddHeaderField = (type: GroupHeaderFieldType) => {
    const newField: GroupHeaderFieldDefinition = {
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

  const handleSubmit = async () => {
    if (!validateTemplate()) return;

    setIsSubmitting(true);

    try {
      const groupDefinition: CostEstimateGroupDefinition = {
        autoNumbered: groupAutoNumbered,
        numberFormat: groupNumberFormat || undefined,
        headerFields: headerFields,
      };

      const workScopeFieldsDefinition: CostEstimateWorkScopeFieldsDefinition = {
        calculatedFields: calculatedFields,
        genericFields: genericFields,
        crossFieldValidationRules: validationRules.length > 0 ? validationRules : undefined,
      };

      const summaryConfiguration: CostEstimateSummaryConfiguration = {
        groupSummaryFields: groupSummaryFields,
        totalSummaryFields: totalSummaryFields,
        showGroupSummary,
        showTotalSummary,
      };

      const uiConfiguration: CostEstimateUiConfiguration = {
        columnLayout: columnLayout.length > 0 ? columnLayout : undefined,
        columnWidths: undefined, // TODO: możliwość ustawiania szerokości kolumn
      };

      const templateStructure: CostEstimateTemplateStructure = {
        canAddGroups,
        canBranchGroups,
        maxGroupLevel,
        groupDefinition,
        workScopeFieldsDefinition,
        summaryConfiguration,
        uiConfiguration,
      };

      // Import API at the top of the file
      const { costEstimateTemplateApi } = await import("../api/costEstimateTemplateApi");

      if (existingTemplate) {
        // Update existing template
        await costEstimateTemplateApi.updateTemplate(existingTemplate.id, {
          templateId: existingTemplate.id,
          name: templateName,
          description: templateDescription || undefined,
          templateStructure,
        });

        toast({
          title: "Sukces",
          description: "Szablon został zaktualizowany",
          status: "success",
          duration: 3000,
        });
      } else {
        // Create new template
        await costEstimateTemplateApi.createTemplate({
          name: templateName,
          description: templateDescription || undefined,
          templateStructure,
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
    onClose();
  };

  // Render funkcja dla TabPanel "Układ pól"
  const renderFieldLayoutTab = () => {
    const [draggedIndex, setDraggedIndex] = useState<number | null>(null);

    // Zbierz wszystkie pola z template
    const allFields: Array<{ name: string; label: string; type: string; colorScheme: string }> = [];

    // Pola nagłówków grup (GroupName, Notes, etc.)
    headerFields.forEach((field) => {
      allFields.push({
        name: GroupHeaderFieldType[field.type],
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

    // Jeśli layout się zmienił, zaktualizuj
    if (validatedLayout.length !== columnLayout.length || !validatedLayout.every((v, i) => v === columnLayout[i])) {
      setColumnLayout(validatedLayout);
    }

    // Sortuj pola według columnLayout
    const sortedFields = [...allFields].sort((a, b) => {
      const indexA = validatedLayout.indexOf(a.name);
      const indexB = validatedLayout.indexOf(b.name);
      return indexA - indexB;
    });

    const handleDragStart = (index: number) => {
      setDraggedIndex(index);
    };

    const handleDragOver = (e: React.DragEvent, index: number) => {
      e.preventDefault();
      if (draggedIndex === null || draggedIndex === index) return;

      const newLayout = [...validatedLayout];
      const draggedItem = newLayout[draggedIndex];
      newLayout.splice(draggedIndex, 1);
      newLayout.splice(index, 0, draggedItem);
      
      setColumnLayout(newLayout);
      setDraggedIndex(index);
    };

    const handleDragEnd = () => {
      setDraggedIndex(null);
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
                  bg={draggedIndex === index ? 'blue.100' : 'gray.50'}
                  borderRadius="md"
                  borderWidth="2px"
                  borderColor={draggedIndex === index ? 'blue.400' : 'gray.200'}
                  spacing={3}
                  cursor="grab"
                  _hover={{ bg: draggedIndex === index ? 'blue.100' : 'gray.100', borderColor: 'blue.300' }}
                  _active={{ cursor: 'grabbing' }}
                  draggable
                  onDragStart={() => handleDragStart(index)}
                  onDragOver={(e) => handleDragOver(e, index)}
                  onDragEnd={handleDragEnd}
                  transition="all 0.2s"
                >
                  <Icon as={GripVertical} color="gray.500" />
                  <Badge colorScheme={field.colorScheme} minW="90px">
                    {field.type}
                  </Badge>
                  <Text fontSize="sm" fontWeight="medium" flex="1">
                    {field.label}
                  </Text>
                  <Text fontSize="xs" color="gray.500" minW="120px">
                    {field.name}
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
    <Modal isOpen={isOpen} onClose={handleClose} size="full" scrollBehavior="inside">
      <ModalOverlay />
      <ModalContent maxH="100vh" m={0}>
        <ModalHeader borderBottom="1px" borderColor="gray.200">
          <HStack spacing={3}>
            <FileText size={24} />
            <Text>{existingTemplate ? "Edycja szablonu kosztorysu" : "Kreator szablonu kosztorysu"}</Text>
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
                    <Text>Nagłówek grupy ({headerFields.length})</Text>
                  </HStack>
                </Tab>
                <Tab>
                  <HStack spacing={2}>
                    <List size={18} />
                    <Text>Pola zakresów prac ({calculatedFields.length + genericFields.length})</Text>
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
                    <AlertCircle size={18} />
                    <Text>Walidacja ({validationRules.length})</Text>
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

                    <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
                      <Text fontSize="md" fontWeight="bold" mb={4}>
                        Numerowanie grup
                      </Text>
                      <VStack spacing={4} align="stretch">
                        <Checkbox
                          isChecked={groupAutoNumbered}
                          onChange={(e) => setGroupAutoNumbered(e.target.checked)}
                        >
                          Automatyczne numerowanie grup
                        </Checkbox>

                        {groupAutoNumbered && (
                          <FormControl>
                            <FormLabel>Format numeracji</FormLabel>
                            <Input
                              value={groupNumberFormat}
                              onChange={(e) => setGroupNumberFormat(e.target.value)}
                              placeholder='"{0}" lub "Etap {0}" lub "{0:00}"'
                            />
                            <FormHelperText>
                              {"{0}"} = numer, {"{0:00}"} = numer z zerami wiodącymi
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
                  {renderFieldLayoutTab()}
                </TabPanel>

                <TabPanel>
                  <ValidationRulesEditor
                    rules={validationRules}
                    onAdd={handleAddValidationRule}
                    onRemove={handleRemoveValidationRule}
                    onUpdate={handleUpdateValidationRule}
                  />
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
          <HStack spacing={3}>
            <Button variant="ghost" size="lg" onClick={handleClose}>
              Anuluj
            </Button>
            <Button
              colorScheme="blue"
              size="lg"
              onClick={handleSubmit}
              isLoading={isSubmitting}
              loadingText={existingTemplate ? "Zapisywanie..." : "Tworzenie..."}
              leftIcon={<Plus size={20} />}
            >
              {existingTemplate ? "Zapisz zmiany" : "Utwórz szablon"}
            </Button>
          </HStack>
        </ModalFooter>
      </ModalContent>
    </Modal>
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
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);

  const handleDragStart = (index: number) => {
    setDraggedIndex(index);
  };

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault();
    if (draggedIndex === null || draggedIndex === index) return;

    // Pracujemy na sortedFields (posortowanych według order)
    const sortedFields = [...headerFields].sort((a, b) => a.order - b.order);
    const reorderedFields = [...sortedFields];
    const draggedField = reorderedFields[draggedIndex];
    reorderedFields.splice(draggedIndex, 1);
    reorderedFields.splice(index, 0, draggedField);
    
    // Zaktualizuj order dla wszystkich pól
    reorderedFields.forEach((field, idx) => {
      field.order = idx;
    });
    
    onReorder(reorderedFields);
    setDraggedIndex(index);
  };

  const handleDragEnd = () => {
    setDraggedIndex(null);
  };

  // Sortuj według order
  const sortedFields = [...headerFields].sort((a, b) => a.order - b.order);

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

      <Box bg="purple.50" p={4} borderRadius="md" borderWidth="1px" borderColor="purple.200">
        <HStack spacing={2} mb={2}>
          <Icon as={List} color="purple.600" />
          <Text fontSize="sm" fontWeight="bold" color="purple.800">
            Pola nagłówka grupy
          </Text>
        </HStack>
        <Text fontSize="xs" color="gray.700">
          Kolejność nagłówków grup jest określona w zakładce "Układ kolumn".
        </Text>
      </Box>

      {sortedFields.length === 0 ? (
        <Box p={8} textAlign="center" borderWidth="2px" borderRadius="md" borderStyle="dashed">
          <Text color="gray.500">Brak pól w nagłówku</Text>
        </Box>
      ) : (
        <VStack spacing={2} align="stretch">
          {sortedFields.map((field, index) => {
            const originalIndex = headerFields.findIndex(f => f.type === field.type);
            return (
              <Box
                key={field.type}
                p={3}
                bg="gray.50"
                borderRadius="md"
                borderWidth="1px"
                borderColor="gray.200"
              >
                <HeaderFieldEditor
                  field={field}
                  index={originalIndex}
                  onRemove={onRemove}
                  onUpdate={onUpdate}
                />
              </Box>
            );
          })}
        </VStack>
      )}
    </VStack>
  );
}

interface HeaderFieldEditorProps {
  field: GroupHeaderFieldDefinition;
  index: number;
  onRemove: (index: number) => void;
  onUpdate: (index: number, updates: Partial<GroupHeaderFieldDefinition>) => void;
}

function HeaderFieldEditor({ field, index, onRemove, onUpdate }: HeaderFieldEditorProps) {
  const { isOpen, onToggle } = useDisclosure({ defaultIsOpen: false });
  const isGroupName = field.type === GroupHeaderFieldType.GroupName;

  return (
    <Box>
      <HStack justify="space-between">
        <HStack spacing={3} flex={1}>
          <Badge colorScheme="purple" fontSize="sm" px={2} py={1}>
            {groupHeaderFieldTypeLabels[field.type]}
          </Badge>
          {isGroupName && <Badge colorScheme="orange">Wymagane</Badge>}
          {!field.visible && (
            <HStack spacing={1} color="gray.500">
              <EyeOff size={14} />
              <Text fontSize="xs">Ukryte</Text>
            </HStack>
          )}
          {field.readOnly && (
            <Badge colorScheme="gray" variant="outline">
              Tylko odczyt
            </Badge>
          )}
        </HStack>
        <HStack spacing={2}>
          <IconButton
            aria-label="Rozwiń/Zwiń"
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
            isDisabled={isGroupName}
          />
        </HStack>
      </HStack>

      <Collapse in={isOpen}>
        <Box pt={3} mt={3} borderTop="1px" borderColor="gray.200">
          <VStack spacing={4} align="stretch">
            <FormControl>
              <FormLabel fontSize="sm">Niestandardowa etykieta</FormLabel>
              <Input
                size="sm"
                value={field.customLabel || ""}
                onChange={(e) => onUpdate(index, { customLabel: e.target.value })}
                placeholder={groupHeaderFieldTypeLabels[field.type]}
              />
            </FormControl>

            <HStack spacing={4} flexWrap="wrap">
              <Checkbox
                isChecked={field.required}
                onChange={(e) => onUpdate(index, { required: e.target.checked })}
                isDisabled={isGroupName}
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
                isChecked={field.readOnly}
                onChange={(e) => onUpdate(index, { readOnly: e.target.checked })}
              >
                Tylko odczyt
              </Checkbox>
            </HStack>

            <FormControl>
              <FormLabel fontSize="sm">Placeholder</FormLabel>
              <Input
                size="sm"
                value={field.placeholder || ""}
                onChange={(e) => onUpdate(index, { placeholder: e.target.value })}
                placeholder="Podpowiedź dla użytkownika"
              />
            </FormControl>

            {(field.type === GroupHeaderFieldType.Status ||
              field.type === GroupHeaderFieldType.Priority) && (
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
                  placeholder="np. Nowy, W trakcie, Zakończony"
                />
              </FormControl>
            )}

            <FormControl>
              <FormLabel fontSize="sm">Tekst pomocy</FormLabel>
              <Textarea
                size="sm"
                rows={2}
                value={field.helpText || ""}
                onChange={(e) => onUpdate(index, { helpText: e.target.value })}
                placeholder="Rozszerzony opis dla użytkownika"
              />
            </FormControl>

            <HStack spacing={3}>
              <FormControl>
                <FormLabel fontSize="sm">Ikona</FormLabel>
                <Input
                  size="sm"
                  value={field.icon || ""}
                  onChange={(e) => onUpdate(index, { icon: e.target.value })}
                  placeholder="np. calendar, user"
                />
              </FormControl>

              <FormControl>
                <FormLabel fontSize="sm">Kolor</FormLabel>
                <Input
                  size="sm"
                  value={field.color || ""}
                  onChange={(e) => onUpdate(index, { color: e.target.value })}
                  placeholder="np. #FF5733, blue"
                />
              </FormControl>
            </HStack>
          </VStack>
        </Box>
      </Collapse>
    </Box>
  );
}

interface CalculatedFieldsEditorProps {
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
        <VStack spacing={3} align="stretch">
          {fields.map((field, index) => (
            <CalculatedFieldEditor
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
            <HStack spacing={3}>
              <FormControl isRequired>
                <FormLabel fontSize="sm">Nazwa (ID)</FormLabel>
                <Input
                  size="sm"
                  value={field.name}
                  onChange={(e) => onUpdate(index, { name: e.target.value })}
                />
              </FormControl>
              <FormControl isRequired>
                <FormLabel fontSize="sm">Etykieta</FormLabel>
                <Input
                  size="sm"
                  value={field.label}
                  onChange={(e) => onUpdate(index, { label: e.target.value })}
                />
              </FormControl>
            </HStack>

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
                isDisabled={field.type !== 4 && field.type !== 5 && field.type !== 7}
              >
                Sumowalne {(field.type !== 4 && field.type !== 5 && field.type !== 7) && <Text as="span" fontSize="xs" color="gray.500">(tylko ValueNet/ValueGross/TotalVat)</Text>}
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
            <HStack spacing={3}>
              <FormControl isRequired>
                <FormLabel fontSize="sm">Nazwa (ID)</FormLabel>
                <Input
                  size="sm"
                  value={field.name}
                  onChange={(e) => onUpdate(index, { name: e.target.value })}
                />
              </FormControl>
              <FormControl isRequired>
                <FormLabel fontSize="sm">Etykieta</FormLabel>
                <Input
                  size="sm"
                  value={field.label}
                  onChange={(e) => onUpdate(index, { label: e.target.value })}
                />
              </FormControl>
            </HStack>

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

            {field.type === GenericFieldType.Collection && (
              <Box mt={4} p={4} borderWidth="1px" borderRadius="md" bg="blue.50">
                <Text fontSize="sm" fontWeight="bold" mb={3}>
                  Konfiguracja kolekcji zagnieżdżonych pól
                </Text>
                <VStack spacing={4} align="stretch">
                  <HStack spacing={3}>
                    <FormControl>
                      <FormLabel fontSize="sm">Min elementów</FormLabel>
                      <NumberInput
                        size="sm"
                        min={0}
                        value={field.nestedFields?.minItems ?? ""}
                        onChange={(_, value) =>
                          onUpdate(index, {
                            nestedFields: {
                              isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
                              enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
                              ...field.nestedFields,
                              minItems: isNaN(value) ? undefined : value,
                            },
                          })
                        }
                      >
                        <NumberInputField />
                      </NumberInput>
                    </FormControl>
                    <FormControl>
                      <FormLabel fontSize="sm">Max elementów</FormLabel>
                      <NumberInput
                        size="sm"
                        min={0}
                        value={field.nestedFields?.maxItems ?? ""}
                        onChange={(_, value) =>
                          onUpdate(index, {
                            nestedFields: {
                              isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
                              enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
                              ...field.nestedFields,
                              maxItems: isNaN(value) ? undefined : value,
                            },
                          })
                        }
                      >
                        <NumberInputField />
                      </NumberInput>
                    </FormControl>
                  </HStack>

                  <Checkbox
                    isChecked={field.nestedFields?.isSelectableCollection ?? false}
                    onChange={(e) =>
                      onUpdate(index, {
                        nestedFields: {
                          ...field.nestedFields,
                          isSelectableCollection: e.target.checked,
                          enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
                        },
                      })
                    }
                  >
                    <HStack spacing={2}>
                      <Text fontSize="sm">Kolekcja z możliwością zaznaczania opcji</Text>
                      <Tooltip label="Użytkownik będzie mógł zaznaczyć jedną opcję z kolekcji (np. wybór wariantu wykończenia). Po zaznaczeniu cena jednostkowa z wybranej opcji zostanie skopiowana do głównej pozycji.">
                        <Box as="span">
                          <HelpCircle size={14} />
                        </Box>
                      </Tooltip>
                    </HStack>
                  </Checkbox>

                  <Checkbox
                    isChecked={field.nestedFields?.enableCalculatedFieldsSummation ?? false}
                    onChange={(e) =>
                      onUpdate(index, {
                        nestedFields: {
                          ...field.nestedFields,
                          isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
                          enableCalculatedFieldsSummation: e.target.checked,
                        },
                      })
                    }
                  >
                    <HStack spacing={2}>
                      <Text fontSize="sm">Włącz sumowanie pól obliczeniowych w kolekcji</Text>
                      <Tooltip label="Wartości z pól obliczeniowych będą sumowane (np. suma cen wszystkich opcji)">
                        <Box as="span">
                          <HelpCircle size={14} />
                        </Box>
                      </Tooltip>
                    </HStack>
                  </Checkbox>

                  {field.nestedFields?.enableCalculatedFieldsSummation && (
                    <FormControl>
                      <FormLabel fontSize="sm">
                        Lista pól do sumowania (nazwy oddzielone przecinkami)
                      </FormLabel>
                      <Input
                        size="sm"
                        value={field.nestedFields?.summableCalculatedFields?.join(", ") || ""}
                        onChange={(e) => {
                          const values = e.target.value
                            .split(",")
                            .map((v) => v.trim())
                            .filter((v) => v);
                          onUpdate(index, {
                            nestedFields: {
                              isSelectableCollection: field.nestedFields?.isSelectableCollection ?? false,
                              enableCalculatedFieldsSummation: field.nestedFields?.enableCalculatedFieldsSummation ?? false,
                              ...field.nestedFields,
                              summableCalculatedFields: values.length > 0 ? values : undefined,
                            },
                          });
                        }}
                        placeholder="Zostaw puste aby sumować wszystkie pola z Summable=true"
                      />
                      <FormHelperText fontSize="xs">
                        Jeśli puste, sumowane będą wszystkie pola obliczeniowe z Summable = true
                      </FormHelperText>
                    </FormControl>
                  )}

                  <Divider />

                  <NestedFieldsEditor
                    field={field}
                    onUpdate={(updates) => onUpdate(index, updates)}
                  />
                </VStack>
              </Box>
            )}
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
      sortable: type !== GenericFieldType.Collection,
      filterable: type !== GenericFieldType.Collection,
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
                      isDisabled={nestedField.type !== 4 && nestedField.type !== 5 && nestedField.type !== 7}
                    >
                      <Text fontSize="xs">Sumowalne {(nestedField.type !== 4 && nestedField.type !== 5 && nestedField.type !== 7) && <Text as="span" fontSize="2xs" color="gray.500">(tylko ValueNet/ValueGross/TotalVat)</Text>}</Text>
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
            .filter(([type]) => parseInt(type) !== GenericFieldType.Collection)
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
                  <Text fontSize="xs" color="gray.500">
                    {nestedField.name}
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
    (f) => f.summable && (f.type === 4 || f.type === 5 || f.type === 7) // CalculatedFieldType: ValueNet = 4, ValueGross = 5, TotalVat = 7
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
                <HStack spacing={2}>
                  <Text fontSize="sm" fontWeight="medium">{field.label}</Text>
                  <Badge colorScheme="green" fontSize="xs">{field.name}</Badge>
                </HStack>
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
                <HStack spacing={2}>
                  <Text fontSize="sm" fontWeight="medium">{field.label}</Text>
                  <Badge colorScheme="blue" fontSize="xs">{field.name}</Badge>
                </HStack>
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
