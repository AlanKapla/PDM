import { useState, useEffect } from "react";
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
  FormControl,
  FormLabel,
  Input,
  Textarea,
  Select,
  Text,
  Box,
  Badge,
  HStack,
  Alert,
  AlertIcon,
  Spinner,
} from "@chakra-ui/react";
import { Plus, FileText } from "lucide-react";
import { Link as RouterLink } from "react-router-dom";
import { costEstimateTemplateApi, type CostEstimateTemplateListItem, type CostEstimateTemplateStructureWeb } from "../api/costEstimateTemplateApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import { useProjectDetails } from "../hooks/queries";

interface TemplateWithStructure extends CostEstimateTemplateListItem {
  structure?: CostEstimateTemplateStructureWeb;
}

interface CreateCostEstimateModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  onCostEstimateCreated: () => void;
}
const iconSize = 20;
export default function CreateCostEstimateModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  onCostEstimateCreated,
}: CreateCostEstimateModalProps) {
  const { showSuccess, showError, showWarning, showInfo, toast } = useToastNotification();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [selectedTemplateId, setSelectedTemplateId] = useState("");
  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);
  const [selectedTemplateDetails, setSelectedTemplateDetails] = useState<TemplateWithStructure | null>(null);
  const [loadingTemplates, setLoadingTemplates] = useState(false);
  const [loadingTemplateDetails, setLoadingTemplateDetails] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Waluta projektu — do wyświetlenia (read-only). Backend pobierze ją z ProjectCurrency.
  const { data: projectDetails } = useProjectDetails(tenantId, projectId);
  const projectCurrency = projectDetails?.currency;

  // Load template list when modal opens
  useEffect(() => {
    if (isOpen) {
      const loadTemplates = async () => {
        setLoadingTemplates(true);
        try {
          const templateList = await costEstimateTemplateApi.getTemplates();
          setTemplates(templateList);
        } catch (error) {
          showError("Błąd", "Nie udało się pobrać szablonów kosztorysów");
          setTemplates([]);
        } finally {
          setLoadingTemplates(false);
        }
      };
      loadTemplates();
    }
  }, [isOpen, toast]);

  // Load details (structure) only for the selected template
  useEffect(() => {
    if (!selectedTemplateId) {
      setSelectedTemplateDetails(null);
      return;
    }

    const loadDetails = async () => {
      setLoadingTemplateDetails(true);
      try {
        const details = await costEstimateTemplateApi.getTemplateDetails(selectedTemplateId);
        const templateBase = templates.find((t) => t.id === selectedTemplateId);
        if (!templateBase) return;
        const withDetails: TemplateWithStructure = {
          ...templateBase,
          structure: details.structure,
        };
        setSelectedTemplateDetails(withDetails);
      } catch {
        showError("Błąd", "Nie udało się pobrać szczegółów szablonu");
        setSelectedTemplateDetails(null);
      } finally {
        setLoadingTemplateDetails(false);
      }
    };
    loadDetails();
  }, [selectedTemplateId, templates, toast]);

  const handleClose = () => {
    setName("");
    setDescription("");
    setSelectedTemplateId("");
    setSelectedTemplateDetails(null);
    onClose();
  };

  const handleSubmit = async () => {
    if (!name.trim()) {
      showError("Sprawdź formularz", "Nazwa kosztorysu jest wymagana");
      return;
    }

    if (!selectedTemplateId) {
      showError("Sprawdź formularz", "Wybierz szablon kosztorysu");
      return;
    }

    const selectedTemplate = selectedTemplateDetails;
    if (!selectedTemplate) {
      showError("Sprawdź formularz", "Nie znaleziono wybranego szablonu");
      return;
    }

    if (!projectCurrency) {
      showError("Brak waluty projektu", "Ustaw walutę projektu w Parametrach przed utworzeniem kosztorysu");
      return;
    }

    setIsSubmitting(true);

    try {
      // Backend tworzy pusty kosztorys na podstawie szablonu, walutę pobiera z ProjectCurrency.
      await costEstimateApi.createCostEstimate(tenantId, projectId, {
        templateId: selectedTemplateId,
        name: name.trim(),
        description: description.trim() || undefined,
      });

      showSuccess("Sukces", "Kosztorys został utworzony");

      onCostEstimateCreated();
      handleClose();
    } catch (error: unknown) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectedTemplate = selectedTemplateDetails;

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size={{ base: "full", md: "xl" }} scrollBehavior="inside">
      <ModalOverlay />
      <ModalContent mx={{ base: 0, md: "auto" }} maxH="90vh">
        <ModalHeader fontSize={{ base: "lg", md: "xl" }}>
          <HStack spacing={3}>
            <FileText size={iconSize} />
            <Text>Nowy kosztorys</Text>
          </HStack>
        </ModalHeader>
        <ModalCloseButton />

        <ModalBody>
          <VStack spacing={4} align="stretch">
            <FormControl isRequired>
              <FormLabel>Nazwa kosztorysu</FormLabel>
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="np. Kosztorys podstawowy"
              />
            </FormControl>

            <FormControl>
              <FormLabel>Opis</FormLabel>
              <Textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Opcjonalny opis kosztorysu"
                rows={3}
              />
            </FormControl>

            <FormControl isRequired>
              <FormLabel>
                Szablon kosztorysu
              </FormLabel>
              <Select
                value={selectedTemplateId}
                onChange={(e) => setSelectedTemplateId(e.target.value)}
                placeholder="Wybierz szablon"
                isDisabled={loadingTemplates}
              >
                {templates.map((template) => (
                  <option key={template.id} value={template.id}>
                    {template.name} {template.category ? `(${template.category})` : ''}
                  </option>
                ))}
              </Select>
              {loadingTemplates && (
                <HStack mt={2} spacing={2}>
                  <Spinner size="xs" />
                  <Text fontSize="sm" color="neutral.500">
                    Ładowanie szablonów...
                  </Text>
                </HStack>
              )}
              
              {!loadingTemplates && templates.length === 0 && (
                <Alert status="info" mt={2} borderRadius="md">
                  <AlertIcon />
                  <Box>
                    <Text fontSize="sm" fontWeight="medium">
                      Brak dostępnych szablonów
                    </Text>
                    <Text fontSize="xs" color="neutral.600" mt={1}>
                      Nie masz jeszcze własnych szablonów. Możesz{" "}
                      <RouterLink to="/cost-estimate-templates" style={{ textDecoration: "underline", color: "var(--chakra-colors-primary-500)" }}>
                        stworzyć własny szablon
                      </RouterLink>
                      {" "}lub{" "}
                      <RouterLink to="/cost-estimate-templates/select" style={{ textDecoration: "underline", color: "var(--chakra-colors-primary-500)" }}>
                        wybrać szablon domyślny
                      </RouterLink>
                      .
                    </Text>
                  </Box>
                </Alert>
              )}
              
              {!loadingTemplates && templates.length > 0 && (
                <Text fontSize="xs" color="neutral.600" mt={1}>
                  Znaleziono {templates.length} {templates.length === 1 ? 'szablon' : 'szablonów'}
                </Text>
              )}
            </FormControl>

              {loadingTemplateDetails && (
                <HStack mt={2} spacing={2}>
                  <Spinner size="xs" />
                  <Text fontSize="sm" color="neutral.500">
                    Ładowanie szczegółów szablonu...
                  </Text>
                </HStack>
              )}

            {selectedTemplate && (
              <FormControl>
                <FormLabel>Waluta projektu</FormLabel>
                {projectCurrency ? (
                  <Text fontWeight="semibold">
                    {projectCurrency.name}
                    {projectCurrency.symbol ? ` (${projectCurrency.symbol})` : ""}
                  </Text>
                ) : (
                  <Text color="orange.500" fontSize="sm">
                    Projekt nie ma ustawionej waluty. Ustaw ją w Parametrach projektu.
                  </Text>
                )}
                <Text fontSize="xs" color="neutral.600" mt={1}>
                  Kosztorys zostanie utworzony w walucie projektu.
                </Text>
              </FormControl>
            )}

            {selectedTemplate && (
              <Box p={3} borderWidth="1px" borderRadius="md" bg="primary.50" borderColor="primary.200">
                <Text fontSize="sm" fontWeight="bold" mb={1}>
                  {selectedTemplate.name}
                </Text>
                {selectedTemplate.description && (
                  <Text fontSize="xs" color="neutral.600" mb={2}>
                    {selectedTemplate.description}
                  </Text>
                )}
                {selectedTemplate.structure && (
                  <Box mt={2} pt={2} borderTopWidth="1px" borderColor="primary.300">
                    <Text fontSize="xs" color="neutral.600">
                      <strong>Struktura:</strong>
                    </Text>
                    <VStack align="stretch" spacing={1} mt={1}>
                      <Text fontSize="xs" color="neutral.600">
                        • Pola nagłówka etapu: {selectedTemplate.structure.groupHeaderFields?.length ?? 0}
                      </Text>
                      <Text fontSize="xs" color="neutral.600">
                        • Pola kalkulowane: {selectedTemplate.structure.calculatedFields?.length ?? 0}
                      </Text>
                      <Text fontSize="xs" color="neutral.600">
                        • Pola dodatkowe: {selectedTemplate.structure.genericFields?.length ?? 0}
                      </Text>
                    </VStack>
                  </Box>
                )}
              </Box>
            )}

            <Box p={3} borderWidth="1px" borderRadius="md" bg="yellow.50" borderColor="yellow.200">
              <Text fontSize="xs" color="neutral.600">
                <strong>Uwaga:</strong> Kosztorys zostanie utworzony z początkową strukturą zgodną z szablonem. 
                Będzie widoczny tylko dla Ciebie i możesz go edytować według potrzeb.
              </Text>
            </Box>
          </VStack>
        </ModalBody>

        <ModalFooter>
          <HStack spacing={3}>
            <Button variant="ghost" onClick={handleClose}>
              Anuluj
            </Button>
            <Button
              colorScheme="primary"
              onClick={handleSubmit}
              isLoading={isSubmitting}
              loadingText="Tworzenie..."
              leftIcon={<Plus size={18} />}
            >
              Utwórz kosztorys
            </Button>
          </HStack>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
