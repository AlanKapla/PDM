import { useState, useEffect, useMemo } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Button,
  SimpleGrid,
  Card,
  CardBody,
  Badge,
  useColorModeValue,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  useDisclosure,
  Input,
  Textarea,
  FormControl,
  FormLabel,
  FormHelperText,
  Divider,
  Alert,
  AlertIcon,
  Tooltip,
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionPanel,
  AccordionIcon,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Checkbox,
} from "@chakra-ui/react";
import {
  FileText,
  ArrowLeft,
  Eye,
  Plus,
  Layers,
  CheckCircle,
  AlertCircle,
  Tag,
  List,
  Calculator,
  Layout,
  Settings,
  Play,
  BookOpen,
} from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import {
  costEstimateTemplateApi,
  type CostEstimateTemplateStructureWeb,
} from "../api/costEstimateTemplateApi";
import type { 
  DefaultCostEstimateTemplateListItemWeb,
  CostEstimateTemplateStructureWeb as TemplateStructureWeb,
  GroupHeaderFieldWeb,
  SystemFieldWeb,
  CalculatedFieldWeb,
  GenericFieldWeb,
} from "../types/costEstimate.types";
import { FieldScope, FieldType } from "../types/costEstimate.types";
import { fieldTypeLabels, fieldScopeLabels } from "../utils/fieldTypeLabels";
import { CostEstimateTableView } from "../components/CostEstimate/CostEstimateTableView";
import type { 
  CostEstimateDetailsWeb, 
  CostEstimateGroupWeb, 
  CostEstimateItemWeb,
  CostEstimateFieldValueWeb,
} from "../types/costEstimate.types.new";
import { CostEstimateStatus, CostEstimateAccessLevel } from "../types/costEstimate.types.new";

export default function CostEstimateTemplateSelector() {
  const { showSuccess, showError } = useToastNotification();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [defaultTemplates, setDefaultTemplates] = useState<DefaultCostEstimateTemplateListItemWeb[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<DefaultCostEstimateTemplateListItemWeb | null>(null);
  const [templateStructure, setTemplateStructure] = useState<CostEstimateTemplateStructureWeb | null>(null);
  const [loadingStructure, setLoadingStructure] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  
  // Modal states
  const { isOpen: isPreviewOpen, onOpen: onPreviewOpen, onClose: onPreviewClose } = useDisclosure();
  const { isOpen: isCreateOpen, onOpen: onCreateOpen, onClose: onCreateClose } = useDisclosure();
  const { isOpen: isLivePreviewOpen, onOpen: onLivePreviewOpen, onClose: onLivePreviewClose } = useDisclosure();
  
  // Form state
  const [newTemplateName, setNewTemplateName] = useState("");
  const [newTemplateDescription, setNewTemplateDescription] = useState("");

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("blue.50", "blue.900");

  useEffect(() => {
    fetchDefaultTemplates();
  }, []);

  const fetchDefaultTemplates = async () => {
    setLoading(true);
    try {
      const data = await costEstimateTemplateApi.getDefaultTemplates();
      setDefaultTemplates(Array.isArray(data) ? data : []);
    } catch (error: any) {
      showError('Nie udało się załadować szablonów', error?.message || 'Wystąpił nieoczekiwany błąd');
      setDefaultTemplates([]);
    } finally {
      setLoading(false);
    }
  };

  const handlePreviewTemplate = async (template: DefaultCostEstimateTemplateListItemWeb) => {
    setSelectedTemplate(template);
    setLoadingStructure(true);
    onPreviewOpen();
    
    try {
      const structure = await costEstimateTemplateApi.getDefaultTemplate(template.slug);
      setTemplateStructure(structure);
    } catch (error: any) {
      showError('Nie udało się załadować struktury szablonu', error?.message);
      setTemplateStructure(null);
    } finally {
      setLoadingStructure(false);
    }
  };

  const handleOpenCreateModal = (template: DefaultCostEstimateTemplateListItemWeb) => {
    setSelectedTemplate(template);
    setNewTemplateName(`${template.name} - Moja kopia`);
    setNewTemplateDescription(`Na podstawie: ${template.name}`);
    onCreateOpen();
  };

  const handleCreateFromDefault = async () => {
    if (!selectedTemplate || !newTemplateName.trim()) {
      showError('Błąd walidacji', 'Nazwa szablonu jest wymagana');
      return;
    }

    setIsCreating(true);
    try {
      const newTemplateId = await costEstimateTemplateApi.createFromDefault(
        selectedTemplate.slug,
        {
          name: newTemplateName.trim(),
          description: newTemplateDescription.trim() || undefined,
        }
      );

      showSuccess('Szablon został utworzony', 'Możesz teraz edytować strukturę szablonu');
      onCreateClose();
      
      // Przekieruj do edycji nowo utworzonego szablonu
      navigate(`/cost-estimate-templates/${newTemplateId}/edit`);
    } catch (error: any) {
      showError('Nie udało się utworzyć szablonu', error?.message || 'Wystąpił nieoczekiwany błąd');
    } finally {
      setIsCreating(false);
    }
  };

  const handleClosePreview = () => {
    onPreviewClose();
    setTemplateStructure(null);
  };

  const handleShowLivePreview = () => {
    onLivePreviewOpen();
  };

  // Podgląd kosztorysu — ładuje strukturę i od razu otwiera live preview (bez modalu struktury)
  const handleLivePreview = async (template: DefaultCostEstimateTemplateListItemWeb) => {
    setSelectedTemplate(template);
    setLoadingStructure(true);
    onLivePreviewOpen();

    try {
      const structure = await costEstimateTemplateApi.getDefaultTemplate(template.slug);
      setTemplateStructure(structure);
    } catch (error: any) {
      showError('Nie udało się załadować struktury szablonu', error?.message);
      setTemplateStructure(null);
    } finally {
      setLoadingStructure(false);
    }
  };

  // Generowanie przykładowych danych kosztorysu na podstawie struktury szablonu
  const previewData = useMemo((): CostEstimateDetailsWeb | null => {
    if (!templateStructure || !selectedTemplate) return null;
    return generateSampleCostEstimate(templateStructure, selectedTemplate.name);
  }, [templateStructure, selectedTemplate]);

  // Helper do wyświetlania etykiety typu pola
  const getFieldTypeLabel = (fieldType: number): string => {
    const ft = fieldType as FieldType;
    return (ft in FieldType && fieldTypeLabels[ft]) || `Typ ${fieldType}`;
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
      <Box p={{ base: 3, sm: 4, md: 8 }}>
        <VStack spacing={6} align="stretch">
          {/* Header */}
          <HStack justify="space-between" flexWrap="wrap" gap={4}>
            <VStack align="start" spacing={1}>
              <HStack spacing={3}>
                <Layers size={28} />
                <Heading size="lg">Wybierz szablon kosztorysu</Heading>
              </HStack>
              <Text fontSize="sm" color="gray.600">
                Wybierz jeden z gotowych szablonów jako podstawę lub utwórz pusty szablon od zera
              </Text>
            </VStack>
            <Button
              leftIcon={<ArrowLeft size={18} />}
              variant="ghost"
              onClick={() => navigate('/cost-estimate-templates')}
            >
              Powrót do listy
            </Button>
          </HStack>

          {/* Sekcja: Pusty szablon */}
          <Card bg={cardBg} borderWidth="1px" borderColor={borderColor}>
            <CardBody>
              <HStack justify="space-between" align="center">
                <VStack align="start" spacing={1}>
                  <HStack spacing={2}>
                    <FileText size={20} />
                    <Text fontWeight="bold">Pusty szablon</Text>
                  </HStack>
                  <Text fontSize="sm" color="gray.600">
                    Zacznij od zera — sam zdefiniujesz wszystkie pola, waluty i jednostki
                  </Text>
                </VStack>
                <Button
                  leftIcon={<Plus size={18} />}
                  colorScheme="gray"
                  variant="outline"
                  onClick={() => navigate('/cost-estimate-templates/new')}
                >
                  Utwórz pusty
                </Button>
              </HStack>
            </CardBody>
          </Card>

          <Divider />

          {/* Sekcja: Szablony domyślne */}
          <VStack align="stretch" spacing={4}>
            <HStack spacing={2}>
              <CheckCircle size={20} />
              <Heading size="md">Gotowe szablony</Heading>
              <Badge colorScheme="blue">{defaultTemplates.length}</Badge>
            </HStack>

            {defaultTemplates.length === 0 ? (
              <EmptyState
                icon={FileText}
                title="Brak dostępnych szablonów"
                description="Nie znaleziono żadnych szablonów domyślnych"
              />
            ) : (
              <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={4}>
                {defaultTemplates.map((template) => (
                  <Card
                    key={template.slug}
                    bg={cardBg}
                    borderWidth="2px"
                    borderColor={borderColor}
                    _hover={{ 
                      borderColor: "blue.400",
                      bg: hoverBg,
                      transform: "translateY(-2px)",
                      shadow: "md"
                    }}
                    transition="all 0.2s"
                  >
                    <CardBody>
                      <VStack align="stretch" spacing={4}>
                        <VStack align="start" spacing={2}>
                          <HStack justify="space-between" width="100%">
                            <HStack spacing={2}>
                              <FileText size={20} />
                              <Text fontWeight="bold" fontSize="md">
                                {template.name}
                              </Text>
                            </HStack>
                            {template.category && (
                              <Badge colorScheme="purple" size="sm">
                                {template.category}
                              </Badge>
                            )}
                          </HStack>

                          {template.description && (
                            <Text fontSize="sm" color="gray.600" noOfLines={3}>
                              {template.description}
                            </Text>
                          )}
                        </VStack>

                        <HStack spacing={2} pt={2}>
                          <Tooltip label="Podgląd przykładowego kosztorysu">
                            <Button
                              size="sm"
                              leftIcon={<Eye size={16} />}
                              variant="ghost"
                              onClick={() => handleLivePreview(template)}
                              flex={1}
                            >
                              Podgląd
                            </Button>
                          </Tooltip>
                          <Tooltip label="Szczegóły struktury szablonu">
                            <Button
                              size="sm"
                              leftIcon={<Settings size={16} />}
                              variant="ghost"
                              onClick={() => handlePreviewTemplate(template)}
                            >
                              Szczegóły
                            </Button>
                          </Tooltip>
                          <Tooltip label="Utwórz szablon na podstawie tego szablonu">
                            <Button
                              size="sm"
                              leftIcon={<Plus size={16} />}
                              colorScheme="blue"
                              onClick={() => handleOpenCreateModal(template)}
                              flex={1}
                            >
                              Użyj
                            </Button>
                          </Tooltip>
                        </HStack>
                      </VStack>
                    </CardBody>
                  </Card>
                ))}
              </SimpleGrid>
            )}
          </VStack>
        </VStack>

        {/* Modal: Podgląd struktury szablonu */}
        <Modal 
          isOpen={isPreviewOpen} 
          onClose={handleClosePreview} 
          size="6xl" 
          scrollBehavior="inside"
        >
          <ModalOverlay />
          <ModalContent maxH="90vh">
            <ModalHeader borderBottom="1px" borderColor="gray.200">
              <HStack spacing={3} justify="space-between" pr={8}>
                <HStack spacing={3}>
                  <Eye size={24} />
                  <Text>Struktura szablonu: {selectedTemplate?.name}</Text>
                  {selectedTemplate?.category && (
                    <Badge colorScheme="purple">{selectedTemplate.category}</Badge>
                  )}
                </HStack>
              </HStack>
            </ModalHeader>
            <ModalCloseButton />
            <ModalBody py={4}>
              {loadingStructure ? (
                <Box py={8}>
                  <LoadingSpinner />
                </Box>
              ) : templateStructure ? (
                <VStack spacing={4} align="stretch">
                  {/* Opis szablonu */}
                  {selectedTemplate?.description && (
                    <Alert status="info" borderRadius="md">
                      <AlertIcon />
                      <Text fontSize="sm">{selectedTemplate.description}</Text>
                    </Alert>
                  )}

                  {/* Zakładki - jak w edytorze szablonu */}
                  <Tabs colorScheme="blue" variant="enclosed">
                    <TabList>
                      <Tooltip label="Pola nagłówka etapu: nazwa, opis, daty, status, odpowiedzialny i inne" placement="bottom" hasArrow>
                        <Tab>
                          <HStack spacing={2}>
                            <Tag size={16} />
                            <Text>Pola etapów ({templateStructure.groupHeaderFields?.length || 0})</Text>
                          </HStack>
                        </Tab>
                      </Tooltip>
                      <Tooltip label="Kolumny tabeli kosztorysu: pola systemowe (nazwa, ilość, jednostka), obliczeniowe (ceny, wartości) i własne" placement="bottom" hasArrow>
                        <Tab>
                          <HStack spacing={2}>
                            <List size={16} />
                            <Text>Pola pozycji ({(templateStructure.systemFields?.length || 0) + (templateStructure.calculatedFields?.length || 0) + (templateStructure.genericFields?.length || 0)})</Text>
                          </HStack>
                        </Tab>
                      </Tooltip>
                      <Tooltip label="Dostępne waluty, jednostki miary i kategorie robót do wyboru w kosztorysie" placement="bottom" hasArrow>
                        <Tab>
                          <HStack spacing={2}>
                            <BookOpen size={16} />
                            <Text>Parametry</Text>
                          </HStack>
                        </Tab>
                      </Tooltip>
                      <Tooltip label="Kolejność i widoczność kolumn w widoku tabeli kosztorysu" placement="bottom" hasArrow>
                        <Tab>
                          <HStack spacing={2}>
                            <Layout size={16} />
                            <Text>Kolejność pól</Text>
                          </HStack>
                        </Tab>
                      </Tooltip>
                    </TabList>

                    <TabPanels>
                      {/* Pola etapów */}
                      <TabPanel>
                        <VStack spacing={4} align="stretch">
                          <Box bg="white" p={4} borderRadius="lg" shadow="sm" borderWidth="1px">
                            <HStack spacing={2} mb={4}>
                              <Tag size={18} />
                              <Text fontSize="md" fontWeight="bold">Pola nagłówka etapu</Text>
                              <Badge colorScheme="orange">{templateStructure.groupHeaderFields?.length || 0}</Badge>
                            </HStack>
                            
                            {templateStructure.groupHeaderFields && templateStructure.groupHeaderFields.length > 0 ? (
                              <Table size="sm" variant="simple">
                                <Thead>
                                  <Tr>
                                    <Th>Etykieta</Th>
                                    <Th>Widoczne</Th>
                                    <Th>Sortowalne</Th>
                                    <Th>Filtrowalne</Th>
                                    <Th>Tylko do odczytu</Th>
                                  </Tr>
                                </Thead>
                                <Tbody>
                                  {templateStructure.groupHeaderFields.map((field, idx) => (
                                    <Tr key={field.id || idx}>
                                      <Td fontWeight="medium">{field.customLabel || field.fieldTypeConfig?.namePl || getFieldTypeLabel(field.fieldType)}</Td>
                                      <Td><Checkbox isChecked={field.isVisible} isReadOnly size="sm" /></Td>
                                      <Td><Checkbox isChecked={field.isSortable} isReadOnly size="sm" /></Td>
                                      <Td><Checkbox isChecked={field.isFilterable} isReadOnly size="sm" /></Td>
                                      <Td><Checkbox isChecked={field.isReadonly} isReadOnly size="sm" /></Td>
                                    </Tr>
                                  ))}
                                </Tbody>
                              </Table>
                            ) : (
                              <Text color="gray.500" fontSize="sm">Brak zdefiniowanych pól nagłówka etapu</Text>
                            )}
                          </Box>
                        </VStack>
                      </TabPanel>

                      {/* Pola pozycji */}
                      <TabPanel>
                        <Accordion allowMultiple defaultIndex={[]}>
                          {/* Pola systemowe */}
                          <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" mb={3} overflow="hidden">
                            <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                              <HStack flex={1} spacing={2}>
                                <FileText size={18} />
                                <Text fontSize="md" fontWeight="bold">Pola systemowe</Text>
                                <Badge colorScheme="blue">{templateStructure.systemFields?.length || 0}</Badge>
                              </HStack>
                              <AccordionIcon />
                            </AccordionButton>
                            <AccordionPanel bg="white" pb={4} px={4}>
                              {templateStructure.systemFields && templateStructure.systemFields.length > 0 ? (
                                <Table size="sm" variant="simple">
                                  <Thead>
                                    <Tr>
                                      <Th>Etykieta</Th>
                                      <Th>Widoczne</Th>
                                      <Th>Sortowalne</Th>
                                      <Th>Filtrowalne</Th>
                                      <Th>Tylko do odczytu</Th>
                                    </Tr>
                                  </Thead>
                                  <Tbody>
                                    {templateStructure.systemFields.map((field, idx) => (
                                      <Tr key={field.id || idx}>
                                        <Td fontWeight="medium">{field.label}</Td>
                                        <Td><Checkbox isChecked={field.isVisible} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isSortable} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isFilterable} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isReadonly} isReadOnly size="sm" /></Td>
                                      </Tr>
                                    ))}
                                  </Tbody>
                                </Table>
                              ) : (
                                <Text color="gray.500" fontSize="sm">Brak pól systemowych</Text>
                              )}
                            </AccordionPanel>
                          </AccordionItem>

                          {/* Pola kalkulowane */}
                          <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" mb={3} overflow="hidden">
                            <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                              <HStack flex={1} spacing={2}>
                                <Calculator size={18} />
                                <Text fontSize="md" fontWeight="bold">Pola kalkulowane</Text>
                                <Badge colorScheme="green">{templateStructure.calculatedFields?.length || 0}</Badge>
                              </HStack>
                              <AccordionIcon />
                            </AccordionButton>
                            <AccordionPanel bg="white" pb={4} px={4}>
                              {templateStructure.calculatedFields && templateStructure.calculatedFields.length > 0 ? (
                                <Table size="sm" variant="simple">
                                  <Thead>
                                    <Tr>
                                      <Th>Etykieta</Th>
                                      <Th>Widoczne</Th>
                                      <Th>Sortowalne</Th>
                                      <Th>Filtrowalne</Th>
                                      <Th>Tylko do odczytu</Th>
                                    </Tr>
                                  </Thead>
                                  <Tbody>
                                    {templateStructure.calculatedFields.map((field, idx) => (
                                      <Tr key={field.id || idx}>
                                        <Td fontWeight="medium">{field.label}</Td>
                                        <Td><Checkbox isChecked={field.isVisible} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isSortable} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isFilterable} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isReadonly} isReadOnly size="sm" /></Td>
                                      </Tr>
                                    ))}
                                  </Tbody>
                                </Table>
                              ) : (
                                <Text color="gray.500" fontSize="sm">Brak pól kalkulowanych</Text>
                              )}
                            </AccordionPanel>
                          </AccordionItem>

                          {/* Pola generyczne */}
                          <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" overflow="hidden">
                            <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                              <HStack flex={1} spacing={2}>
                                <Tag size={18} />
                                <Text fontSize="md" fontWeight="bold">Pola generyczne</Text>
                                <Badge colorScheme="purple">{templateStructure.genericFields?.length || 0}</Badge>
                              </HStack>
                              <AccordionIcon />
                            </AccordionButton>
                            <AccordionPanel bg="white" pb={4} px={4}>
                              {templateStructure.genericFields && templateStructure.genericFields.length > 0 ? (
                                <Table size="sm" variant="simple">
                                  <Thead>
                                    <Tr>
                                      <Th>Etykieta</Th>
                                      <Th>Widoczne</Th>
                                      <Th>Sortowalne</Th>
                                      <Th>Filtrowalne</Th>
                                      <Th>Tylko do odczytu</Th>
                                    </Tr>
                                  </Thead>
                                  <Tbody>
                                    {templateStructure.genericFields.map((field, idx) => (
                                      <Tr key={field.id || idx}>
                                        <Td fontWeight="medium">{field.label}</Td>
                                        <Td><Checkbox isChecked={field.isVisible} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isSortable} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isFilterable} isReadOnly size="sm" /></Td>
                                        <Td><Checkbox isChecked={field.isReadonly} isReadOnly size="sm" /></Td>
                                      </Tr>
                                    ))}
                                  </Tbody>
                                </Table>
                              ) : (
                                <Text color="gray.500" fontSize="sm">Brak pól generycznych</Text>
                              )}
                            </AccordionPanel>
                          </AccordionItem>
                        </Accordion>
                      </TabPanel>

                      {/* Waluty i jednostki */}
                      <TabPanel>
                        <Accordion allowMultiple defaultIndex={[]}>
                          {/* Waluty */}
                          <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" mb={3} overflow="hidden">
                            <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                              <HStack flex={1} spacing={2}>
                                <Text fontSize="lg" lineHeight={1}>💰</Text>
                                <Text fontSize="md" fontWeight="bold">Waluty</Text>
                                <Badge colorScheme="yellow">{templateStructure.currencies?.length || 0}</Badge>
                              </HStack>
                              <AccordionIcon />
                            </AccordionButton>
                            <AccordionPanel bg="white" pb={4} px={4}>
                              {templateStructure.currencies && templateStructure.currencies.length > 0 ? (
                                <Table size="sm" variant="simple">
                                  <Thead>
                                    <Tr>
                                      <Th>Kod</Th>
                                      <Th>Nazwa</Th>
                                      <Th>Symbol</Th>
                                      <Th>Domyślna</Th>
                                    </Tr>
                                  </Thead>
                                  <Tbody>
                                    {templateStructure.currencies.sort((a, b) => a.order - b.order).map((currency) => (
                                      <Tr key={currency.id}>
                                        <Td fontWeight="medium">{currency.code}</Td>
                                        <Td>{currency.name}</Td>
                                        <Td>{currency.symbol || '-'}</Td>
                                        <Td>
                                          {currency.isDefault && <Badge colorScheme="green">Domyślna</Badge>}
                                        </Td>
                                      </Tr>
                                    ))}
                                  </Tbody>
                                </Table>
                              ) : (
                                <Text color="gray.500" fontSize="sm">Brak zdefiniowanych walut</Text>
                              )}
                            </AccordionPanel>
                          </AccordionItem>

                          {/* Jednostki */}
                          <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" mb={3} overflow="hidden">
                            <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                              <HStack flex={1} spacing={2}>
                                <Text fontSize="lg" lineHeight={1}>📏</Text>
                                <Text fontSize="md" fontWeight="bold">Jednostki miar</Text>
                                <Badge colorScheme="teal">{templateStructure.units?.length || 0}</Badge>
                              </HStack>
                              <AccordionIcon />
                            </AccordionButton>
                            <AccordionPanel bg="white" pb={4} px={4}>
                              {templateStructure.units && templateStructure.units.length > 0 ? (
                                <Table size="sm" variant="simple">
                                  <Thead>
                                    <Tr>
                                      <Th>Kod</Th>
                                      <Th>Nazwa</Th>
                                      <Th>Symbol</Th>
                                      <Th>Kategoria</Th>
                                      <Th>Domyślna</Th>
                                    </Tr>
                                  </Thead>
                                  <Tbody>
                                    {templateStructure.units.sort((a, b) => a.order - b.order).map((unit) => (
                                      <Tr key={unit.id}>
                                        <Td fontWeight="medium">{unit.code}</Td>
                                        <Td>{unit.name}</Td>
                                        <Td>{unit.symbol}</Td>
                                        <Td>{unit.category || '-'}</Td>
                                        <Td>
                                          {unit.isDefault && <Badge colorScheme="green">Domyślna</Badge>}
                                        </Td>
                                      </Tr>
                                    ))}
                                  </Tbody>
                                </Table>
                              ) : (
                                <Text color="gray.500" fontSize="sm">Brak zdefiniowanych jednostek</Text>
                              )}
                            </AccordionPanel>
                          </AccordionItem>

                          {/* Kategorie */}
                          <AccordionItem border="1px" borderColor="gray.200" borderRadius="lg" overflow="hidden">
                            <AccordionButton bg="white" _expanded={{ bg: "white" }} px={4} py={3}>
                              <HStack flex={1} spacing={2}>
                                <Text fontSize="lg" lineHeight={1}>🏷️</Text>
                                <Text fontSize="md" fontWeight="bold">Kategorie</Text>
                                <Badge colorScheme="purple">{templateStructure.categories?.length || 0}</Badge>
                              </HStack>
                              <AccordionIcon />
                            </AccordionButton>
                            <AccordionPanel bg="white" pb={4} px={4}>
                              {templateStructure.categories && templateStructure.categories.length > 0 ? (
                                <Table size="sm" variant="simple">
                                  <Thead>
                                    <Tr>
                                      <Th>Nazwa</Th>
                                      <Th>Symbol</Th>
                                    </Tr>
                                  </Thead>
                                  <Tbody>
                                    {templateStructure.categories.sort((a, b) => a.order - b.order).map((cat) => (
                                      <Tr key={cat.id}>
                                        <Td fontWeight="medium">{cat.name}</Td>
                                        <Td>{cat.symbol || '-'}</Td>
                                      </Tr>
                                    ))}
                                  </Tbody>
                                </Table>
                              ) : (
                                <Text color="gray.500" fontSize="sm">Brak zdefiniowanych kategorii</Text>
                              )}
                            </AccordionPanel>
                          </AccordionItem>
                        </Accordion>
                      </TabPanel>

                      {/* Układ kolumn */}
                      <TabPanel>
                        <Box bg="white" p={4} borderRadius="lg" shadow="sm" borderWidth="1px">
                          <HStack spacing={2} mb={4}>
                            <Layout size={18} />
                            <Text fontSize="md" fontWeight="bold">Kolejność kolumn w tabeli</Text>
                            <Badge colorScheme="cyan">{templateStructure.uiConfiguration?.columns?.length || 0}</Badge>
                          </HStack>
                          
                          {templateStructure.uiConfiguration?.columns && templateStructure.uiConfiguration.columns.length > 0 ? (
                            <Table size="sm" variant="simple">
                              <Thead>
                                <Tr>
                                  <Th width="60px">Lp.</Th>
                                  <Th>Kolumna</Th>
                                  <Th>Zakres</Th>
                                  <Th>Typ pola</Th>
                                </Tr>
                              </Thead>
                              <Tbody>
                                {templateStructure.uiConfiguration.columns.sort((a, b) => a.order - b.order).map((col, idx) => (
                                  <Tr key={col.fieldId}>
                                    <Td>{idx + 1}</Td>
                                    <Td fontWeight="medium">{col.fieldLabel}</Td>
                                    <Td>
                                      <Badge colorScheme={getScopeColor(col.fieldScope)} size="sm">
                                        {fieldScopeLabels[col.fieldScope] || `Scope ${col.fieldScope}`}
                                      </Badge>
                                    </Td>
                                    <Td>
                                      <Text fontSize="sm" color="gray.600">
                                        {getFieldTypeLabel(col.fieldType)}
                                      </Text>
                                    </Td>
                                  </Tr>
                                ))}
                              </Tbody>
                            </Table>
                          ) : (
                            <Text color="gray.500" fontSize="sm">Brak konfiguracji kolumn</Text>
                          )}
                        </Box>
                      </TabPanel>
                    </TabPanels>
                  </Tabs>
                </VStack>
              ) : (
                <Text color="gray.500">Nie udało się załadować struktury szablonu</Text>
              )}
            </ModalBody>
            <ModalFooter borderTop="1px" borderColor="gray.200">
              <HStack spacing={3}>
                <Button 
                  variant="outline" 
                  leftIcon={<Play size={16} />}
                  onClick={handleShowLivePreview}
                  isDisabled={!templateStructure}
                >
                  Podgląd kosztorysu
                </Button>
                <Button variant="ghost" onClick={handleClosePreview}>
                  Zamknij
                </Button>
                <Button
                  colorScheme="blue"
                  leftIcon={<Plus size={18} />}
                  onClick={() => {
                    handleClosePreview();
                    if (selectedTemplate) {
                      handleOpenCreateModal(selectedTemplate);
                    }
                  }}
                >
                  Użyj tego szablonu
                </Button>
              </HStack>
            </ModalFooter>
          </ModalContent>
        </Modal>

        {/* Modal: Podgląd przykładowego kosztorysu */}
        <Modal 
          isOpen={isLivePreviewOpen} 
          onClose={onLivePreviewClose} 
          size="full" 
          scrollBehavior="inside"
          closeOnOverlayClick={false}
        >
          <ModalOverlay />
          <ModalContent maxH="100vh" m={0}>
            <ModalHeader borderBottom="1px" borderColor="gray.200">
              <HStack spacing={3}>
                <Play size={24} />
                <Text>Podgląd kosztorysu: {selectedTemplate?.name}</Text>
              </HStack>
            </ModalHeader>
            <ModalCloseButton />
            <ModalBody p={6} bg="gray.50">
              {loadingStructure ? (
                <Box py={16}>
                  <LoadingSpinner />
                </Box>
              ) : previewData ? (
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
                  </VStack>
                  <CostEstimateTableView
                    details={previewData}
                    editable={false}
                    onDataChange={() => {}}
                  />
                </Box>
              ) : (
                <Text color="gray.500">Nie udało się wygenerować podglądu</Text>
              )}
            </ModalBody>
            <ModalFooter borderTop="1px" borderColor="gray.200">
              <HStack spacing={3}>
                <Button
                  variant="outline"
                  leftIcon={<Settings size={16} />}
                  onClick={() => {
                    onLivePreviewClose();
                    // Struktura już załadowana — wystarczy otworzyć modal szczegółów
                    onPreviewOpen();
                  }}
                  isDisabled={loadingStructure}
                >
                  Szczegóły
                </Button>
                <Button variant="ghost" onClick={onLivePreviewClose}>Zamknij</Button>
                <Button
                  colorScheme="blue"
                  leftIcon={<Plus size={18} />}
                  onClick={() => {
                    onLivePreviewClose();
                    if (selectedTemplate) handleOpenCreateModal(selectedTemplate);
                  }}
                >
                  Użyj tego szablonu
                </Button>
              </HStack>
            </ModalFooter>
          </ModalContent>
        </Modal>

        {/* Modal: Tworzenie szablonu */}
        <Modal isOpen={isCreateOpen} onClose={onCreateClose} size="md">
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>
              <HStack spacing={3}>
                <Plus size={24} />
                <Text>Utwórz szablon</Text>
              </HStack>
            </ModalHeader>
            <ModalCloseButton />
            <ModalBody>
              <VStack spacing={4} align="stretch">
                <Alert status="info" borderRadius="md">
                  <AlertIcon />
                  <VStack align="start" spacing={1}>
                    <Text fontSize="sm" fontWeight="medium">
                      Szablon bazowy: {selectedTemplate?.name}
                    </Text>
                    <Text fontSize="xs" color="gray.600">
                      Wszystkie pola, waluty i jednostki zostaną skopiowane. Po utworzeniu będziesz mógł je dowolnie modyfikować.
                    </Text>
                  </VStack>
                </Alert>

                <FormControl isRequired>
                  <FormLabel>Nazwa szablonu</FormLabel>
                  <Input
                    value={newTemplateName}
                    onChange={(e) => setNewTemplateName(e.target.value)}
                    placeholder="np. Kosztorys budowlany"
                    autoFocus
                  />
                  <FormHelperText>
                    Nazwa powinna jednoznacznie identyfikować szablon
                  </FormHelperText>
                </FormControl>

                <FormControl>
                  <FormLabel>Opis (opcjonalny)</FormLabel>
                  <Textarea
                    value={newTemplateDescription}
                    onChange={(e) => setNewTemplateDescription(e.target.value)}
                    placeholder="Krótki opis przeznaczenia szablonu"
                    rows={3}
                  />
                </FormControl>
              </VStack>
            </ModalBody>
            <ModalFooter>
              <HStack spacing={3}>
                <Button variant="ghost" onClick={onCreateClose} isDisabled={isCreating}>
                  Anuluj
                </Button>
                <Button
                  colorScheme="blue"
                  leftIcon={<Plus size={18} />}
                  onClick={handleCreateFromDefault}
                  isLoading={isCreating}
                  loadingText="Tworzenie..."
                >
                  Utwórz szablon
                </Button>
              </HStack>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </Box>
    </MainLayout>
  );
}

// ======================== HELPER FUNCTIONS ========================

// Kolor badge dla scope
function getScopeColor(scope: number): string {
  switch (scope) {
    case FieldScope.Group: return "orange";
    case FieldScope.ItemSystem: return "blue";
    case FieldScope.ItemCalculated: return "green";
    case FieldScope.ItemGeneric: return "purple";
    default: return "gray";
  }
}

/**
 * Generuje przykładowy kosztorys na podstawie struktury szablonu domyślnego
 */
function generateSampleCostEstimate(
  structure: CostEstimateTemplateStructureWeb,
  templateName: string
): CostEstimateDetailsWeb {
  const now = new Date().toISOString();
  const sampleGroups: CostEstimateGroupWeb[] = [];

  const { groupHeaderFields, systemFields, calculatedFields, genericFields, currencies, units, categories } = structure;

  // Generuj 2 przykładowe grupy
  for (let i = 0; i < 2; i++) {
    const groupId = `sample-group-${i}`;
    const items: CostEstimateItemWeb[] = [];
    
    // Generuj 3 przykładowe pozycje w grupie
    for (let j = 0; j < 3; j++) {
      const itemId = `sample-item-${i}-${j}`;
      const fieldValues: CostEstimateFieldValueWeb[] = [];

      // Dodaj wartości dla pól systemowych
      systemFields?.forEach((field) => {
        const fieldType = field.fieldTypeConfig?.fieldType ?? field.fieldType;
        const fieldScope = field.fieldTypeConfig?.fieldScope ?? FieldScope.ItemSystem;
        
        let stringValue: string | undefined;
        let decimalValue: number | undefined;
        let boolValue: boolean | undefined;

        if (fieldType === FieldType.ItemSystemName) {
          stringValue = `Pozycja ${i + 1}.${j + 1}`;
        } else if (fieldType === FieldType.ItemSystemQuantity) {
          decimalValue = 10 + j * 5;
        } else if (fieldType === FieldType.ItemSystemUnit) {
          stringValue = units?.[0]?.code || 'szt';
        } else if (fieldType === FieldType.ItemSystemCategory) {
          stringValue = structure.categories?.[0]?.name || undefined;
        } else if (fieldType === FieldType.ItemSystemSelected) {
          boolValue = true;
        }

        if (stringValue !== undefined || decimalValue !== undefined || boolValue !== undefined) {
          fieldValues.push({
            id: `fv-sys-${i}-${j}-${field.fieldName}`,
            fieldDefinitionId: field.id || field.fieldName,
            fieldType,
            fieldScope,
            fieldName: field.fieldName,
            fieldLabel: field.label,
            stringValue,
            decimalValue,
            boolValue,
          });
        }
      });

      // Dodaj wartości dla pól kalkulowanych
      calculatedFields?.forEach((field) => {
        const fieldType = field.fieldTypeConfig?.fieldType ?? field.fieldType;
        const fieldScope = field.fieldTypeConfig?.fieldScope ?? FieldScope.ItemCalculated;
        
        let decimalValue: number | undefined;
        
        if (fieldType === FieldType.ItemCalculatedUnitPriceNet) {
          decimalValue = 100 + j * 50;
        } else if (fieldType === FieldType.ItemCalculatedVatRate) {
          decimalValue = 23;
        } else if (fieldType === FieldType.ItemCalculatedUnitPriceGross) {
          const unitPriceNet = 100 + j * 50;
          decimalValue = unitPriceNet * 1.23;
        } else if (fieldType === FieldType.ItemCalculatedValueNet) {
          const unitPriceNet = 100 + j * 50;
          const quantity = 10 + j * 5;
          decimalValue = unitPriceNet * quantity;
        } else if (fieldType === FieldType.ItemCalculatedValueGross) {
          const unitPriceNet = 100 + j * 50;
          const quantity = 10 + j * 5;
          decimalValue = unitPriceNet * quantity * 1.23;
        } else if (fieldType === FieldType.ItemCalculatedUnitVat) {
          const unitPriceNet = 100 + j * 50;
          decimalValue = unitPriceNet * 0.23;
        } else if (fieldType === FieldType.ItemCalculatedTotalVat) {
          const unitPriceNet = 100 + j * 50;
          const quantity = 10 + j * 5;
          decimalValue = unitPriceNet * quantity * 0.23;
        }

        if (decimalValue !== undefined) {
          fieldValues.push({
            id: `fv-calc-${i}-${j}-${field.fieldName}`,
            fieldDefinitionId: field.id || field.fieldName,
            fieldType,
            fieldScope,
            fieldName: field.fieldName,
            fieldLabel: field.label,
            decimalValue,
          });
        }
      });

      // Dodaj wartości dla pól generycznych
      genericFields?.forEach((field) => {
        const fieldType = field.fieldTypeConfig?.fieldType ?? field.fieldType;
        const fieldScope = field.fieldTypeConfig?.fieldScope ?? FieldScope.ItemGeneric;
        
        let stringValue: string | undefined;
        let decimalValue: number | undefined;
        let boolValue: boolean | undefined;
        let dateTimeValue: string | undefined;

        if (fieldType === FieldType.ItemGenericString) {
          stringValue = `Przykładowy tekst ${j + 1}`;
        } else if (fieldType === FieldType.ItemGenericInteger) {
          decimalValue = 10 + j;
        } else if (fieldType === FieldType.ItemGenericDecimal) {
          decimalValue = 10.5 + j;
        } else if (fieldType === FieldType.ItemGenericBoolean) {
          boolValue = j % 2 === 0;
        } else if (fieldType === FieldType.ItemGenericDate) {
          const date = new Date();
          date.setDate(date.getDate() + j);
          dateTimeValue = date.toISOString().split('T')[0];
        } else if (fieldType === FieldType.ItemGenericDateTime) {
          const date = new Date();
          date.setDate(date.getDate() + j);
          dateTimeValue = date.toISOString();
        }

        if (stringValue !== undefined || decimalValue !== undefined || boolValue !== undefined || dateTimeValue !== undefined) {
          fieldValues.push({
            id: `fv-gen-${i}-${j}-${field.fieldName}`,
            fieldDefinitionId: field.id || field.fieldName,
            fieldType,
            fieldScope,
            fieldName: field.fieldName,
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
    const groupFieldValues: CostEstimateFieldValueWeb[] = [];
    groupHeaderFields?.forEach((field) => {
      const fieldType = field.fieldTypeConfig?.fieldType ?? field.fieldType;
      
      let stringValue: string | undefined;
      if (fieldType === FieldType.GroupName) {
        stringValue = `Przykładowy etap ${i + 1}`;
      } else if (fieldType === FieldType.GroupNumber) {
        stringValue = `${i + 1}`;
      } else if (fieldType === FieldType.GroupDescription) {
        stringValue = `Opis przykładowego etapu ${i + 1}`;
      }

      if (stringValue !== undefined) {
        groupFieldValues.push({
          id: `gfv-${i}-${field.fieldName}`,
          fieldDefinitionId: field.id || field.fieldName || `header-${fieldType}`,
          fieldType,
          fieldScope: FieldScope.Group,
          fieldLabel: field.customLabel || field.fieldTypeConfig?.namePl || `Pole ${fieldType}`,
          stringValue,
        });
      }
    });

    // Oblicz sumy dla grupy
    let groupTotalNet = 0;
    let groupTotalGross = 0;
    let groupTotalVat = 0;
    
    items.forEach(item => {
      const valueNetField = item.fieldValues.find(fv => fv.fieldType === FieldType.ItemCalculatedValueNet);
      const valueGrossField = item.fieldValues.find(fv => fv.fieldType === FieldType.ItemCalculatedValueGross);
      const totalVatField = item.fieldValues.find(fv => fv.fieldType === FieldType.ItemCalculatedTotalVat);
      
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

  // Przygotuj templateStructure
  const templateStructureForPreview: TemplateStructureWeb = {
    templateId: structure.templateId || 'preview-template',
    currencies: currencies?.map((c, idx) => ({
      id: c.id || c.code,
      code: c.code,
      name: c.name,
      symbol: c.symbol,
      isDefault: c.isDefault,
      order: idx,
    })) || [],
    units: units?.map((u, idx) => ({
      id: u.id || u.code,
      code: u.code,
      name: u.name,
      symbol: u.symbol,
      category: u.category,
      isDefault: u.isDefault,
      order: idx,
    })) || [],
    categories: categories?.map((c, idx) => ({
      id: c.id || `cat-${idx}`,
      name: c.name,
      symbol: c.symbol,
      order: idx,
    })) || [],
    groupHeaderFields: groupHeaderFields || [],
    systemFields: systemFields || [],
    calculatedFields: calculatedFields || [],
    genericFields: genericFields || [],
    summaryConfiguration: structure.summaryConfiguration,
    uiConfiguration: structure.uiConfiguration,
  };

  return {
    id: 'preview-cost-estimate',
    tenantId: 'preview-tenant',
    projectId: 'preview-project',
    templateId: structure.templateId || 'preview-template',
    templateName: templateName,
    selectedCurrencyId: currencies?.[0]?.id || currencies?.[0]?.code || 'PLN',
    selectedCurrencyCode: currencies?.[0]?.code || 'PLN',
    selectedCurrencySymbol: currencies?.[0]?.symbol || 'zł',
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
    templateStructure: templateStructureForPreview,
    accessLevel: CostEstimateAccessLevel.Full,
    sharedWithUsers: [],
  };
}
