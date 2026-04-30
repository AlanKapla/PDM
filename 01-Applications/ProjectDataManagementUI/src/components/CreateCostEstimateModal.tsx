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
import { costEstimateTemplateApi, type CostEstimateTemplateListItem, type CostEstimateTemplateStructureWeb, type CurrencyWeb } from "../api/costEstimateTemplateApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";

interface TemplateWithStructure extends CostEstimateTemplateListItem {
  structure?: CostEstimateTemplateStructureWeb;
  currencies?: CurrencyWeb[];
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
  const [selectedCurrencyId, setSelectedCurrencyId] = useState("");
  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);
  const [selectedTemplateDetails, setSelectedTemplateDetails] = useState<TemplateWithStructure | null>(null);
  const [loadingTemplates, setLoadingTemplates] = useState(false);
  const [loadingTemplateDetails, setLoadingTemplateDetails] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

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

  // Load details (currencies, structure) only for the selected template
  useEffect(() => {
    if (!selectedTemplateId) {
      setSelectedTemplateDetails(null);
      setSelectedCurrencyId("");
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
          currencies: details.structure?.currencies || [],
        };
        setSelectedTemplateDetails(withDetails);

        // Auto-select default currency
        const currencies = details.structure?.currencies || [];
        const defaultCurrency = currencies.find((c) => c.isDefault) ?? currencies[0];
        setSelectedCurrencyId(defaultCurrency?.id ?? "");
      } catch {
        showError("Błąd", "Nie udało się pobrać szczegółów szablonu");
        setSelectedTemplateDetails(null);
        setSelectedCurrencyId("");
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
    setSelectedCurrencyId("");
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

    if (!selectedCurrencyId) {
      showError("Sprawdź formularz", "Wybierz walutę kosztorysu");
      return;
    }

    const selectedTemplate = selectedTemplateDetails;
    if (!selectedTemplate) {
      showError("Sprawdź formularz", "Nie znaleziono wybranego szablonu");
      return;
    }

    const selectedCurrency = selectedTemplate.currencies?.find(c => c.id === selectedCurrencyId);
    if (!selectedCurrency) {
      showError("Sprawdź formularz", "Wybrana waluta nie jest dostępna w tym szablonie");
      return;
    }

    setIsSubmitting(true);

    try {
      // Backend tworzy pusty kosztorys na podstawie szablonu
      await costEstimateApi.createCostEstimate(tenantId, projectId, {
        templateId: selectedTemplateId,
        selectedCurrencyId: selectedCurrencyId,
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
                <Alert status="warning" mt={2} borderRadius="md">
                  <AlertIcon />
                  <Box>
                    <Text fontSize="sm" fontWeight="medium">
                      Brak dostępnych szablonów
                    </Text>
                    <Text fontSize="xs" color="neutral.600">
                      Musisz najpierw utworzyć szablon kosztorysu.
                    </Text>
                    <RouterLink to="/cost-estimate-templates">
                      <Text fontSize="xs" color="primary.500" mt={1} textDecoration="underline">
                        Przejdź do zarządzania szablonami →
                      </Text>
                    </RouterLink>
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

            {selectedTemplate && selectedTemplate.currencies && selectedTemplate.currencies.length > 0 && (
              <FormControl isRequired>
                <FormLabel>Waluta kosztorysu</FormLabel>
                <Select
                  value={selectedCurrencyId}
                  onChange={(e) => setSelectedCurrencyId(e.target.value)}
                  placeholder="Wybierz walutę"
                >
                  {selectedTemplate.currencies.map((currency) => (
                    <option key={currency.id} value={currency.id}>
                      {currency.name} ({currency.code}){currency.symbol ? ` - ${currency.symbol}` : ''}{currency.isDefault ? ' [Domyślna]' : ''}
                    </option>
                  ))}
                </Select>
                <Text fontSize="xs" color="neutral.600" mt={1}>
                  Wybierz walutę, w której będzie prowadzony kosztorys
                </Text>
              </FormControl>
            )}

            {selectedTemplate && selectedTemplate.currencies && selectedTemplate.currencies.length === 0 && (
              <Alert status="error" borderRadius="md">
                <AlertIcon />
                <Text fontSize="sm">
                  Wybrany szablon nie ma zdefiniowanych walut. Skontaktuj się z administratorem.
                </Text>
              </Alert>
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
                      <Text fontSize="xs" color="neutral.600">
                        • Waluty: {selectedTemplate.structure.currencies?.length ?? 0}
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
