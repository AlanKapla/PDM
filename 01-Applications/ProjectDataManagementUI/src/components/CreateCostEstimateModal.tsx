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
  useToast,
  Text,
  Box,
  Badge,
  HStack,
  Alert,
  AlertIcon,
  Spinner,
} from "@chakra-ui/react";
import { Plus, FileText } from "lucide-react";
import { costEstimateTemplateApi, type CostEstimateTemplateListItem, type CurrencyWeb } from "../api/costEstimateTemplateApi";
import { costEstimateApi } from "../api/costEstimateApi";

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
  const toast = useToast();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [selectedTemplateId, setSelectedTemplateId] = useState("");
  const [selectedCurrencyId, setSelectedCurrencyId] = useState("");
  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);
  const [loadingTemplates, setLoadingTemplates] = useState(false);
  const [loadingTemplateDetails, setLoadingTemplateDetails] = useState(false);
  const [selectedTemplateCurrencies, setSelectedTemplateCurrencies] = useState<CurrencyWeb[]>([]);
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
          toast({
            title: "Błąd",
            description: "Nie udało się pobrać szablonów kosztorysów",
            status: "error",
            duration: 5000,
          });
          setTemplates([]);
        } finally {
          setLoadingTemplates(false);
        }
      };
      loadTemplates();
    }
  }, [isOpen, toast]);

  // Lazy-load template details (currencies) when a template is selected
  useEffect(() => {
    if (!selectedTemplateId) {
      setSelectedTemplateCurrencies([]);
      setSelectedCurrencyId("");
      return;
    }
    const loadDetails = async () => {
      setLoadingTemplateDetails(true);
      try {
        const details = await costEstimateTemplateApi.getTemplateDetails(selectedTemplateId);
        const currencies = details.structure?.currencies || [];
        setSelectedTemplateCurrencies(currencies);
        // Wybierz walutę domyślną lub pierwszą z listy; jeśli lista jest pusta, id pozostaje ""
        const defaultCurrency = currencies.find(c => c.isDefault) ?? currencies[0];
        setSelectedCurrencyId(defaultCurrency?.id ?? "");
      } catch {
        setSelectedTemplateCurrencies([]);
        setSelectedCurrencyId("");
      } finally {
        setLoadingTemplateDetails(false);
      }
    };
    loadDetails();
  }, [selectedTemplateId]);

  const handleClose = () => {
    setName("");
    setDescription("");
    setSelectedTemplateId("");
    setSelectedCurrencyId("");
    setSelectedTemplateCurrencies([]);
    onClose();
  };

  const handleSubmit = async () => {
    if (!name.trim()) {
      toast({
        title: "Błąd",
        description: "Nazwa kosztorysu jest wymagana",
        status: "error",
        duration: 3000,
      });
      return;
    }

    if (!selectedTemplateId) {
      toast({
        title: "Błąd",
        description: "Wybierz szablon kosztorysu",
        status: "error",
        duration: 3000,
      });
      return;
    }

    if (!selectedCurrencyId) {
      toast({
        title: "Błąd",
        description: "Wybierz walutę kosztorysu",
        status: "error",
        duration: 3000,
      });
      return;
    }

    const selectedTemplate = templates.find(t => t.id === selectedTemplateId);
    if (!selectedTemplate) {
      toast({
        title: "Błąd",
        description: "Nie znaleziono wybranego szablonu",
        status: "error",
        duration: 3000,
      });
      return;
    }

    const selectedCurrency = selectedTemplateCurrencies.find(c => c.id === selectedCurrencyId);
    if (!selectedCurrency) {
      toast({
        title: "Błąd",
        description: "Wybrana waluta nie jest dostępna w tym szablonie",
        status: "error",
        duration: 3000,
      });
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

      toast({
        title: "Sukces",
        description: "Kosztorys został utworzony",
        status: "success",
        duration: 3000,
      });

      onCostEstimateCreated();
      handleClose();
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message 
        || error?.message 
        || "Nie udało się utworzyć kosztorysu";
      
      toast({
        title: "Błąd",
        description: errorMessage,
        status: "error",
        duration: 5000,
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectedTemplate = templates.find((t) => t.id === selectedTemplateId);

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size={{ base: "full", md: "xl" }}>
      <ModalOverlay />
      <ModalContent mx={{ base: 0, md: "auto" }}>
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
                  <Text fontSize="sm" color="gray.500">
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
                    <Text fontSize="xs" color="gray.600">
                      Musisz najpierw utworzyć szablon kosztorysu.
                    </Text>
                  </Box>
                </Alert>
              )}
              
              {!loadingTemplates && templates.length > 0 && (
                <Text fontSize="xs" color="gray.600" mt={1}>
                  Znaleziono {templates.length} {templates.length === 1 ? 'szablon' : 'szablonów'}
                </Text>
              )}
            </FormControl>

            {selectedTemplateId && loadingTemplateDetails && (
              <HStack mt={2} spacing={2}>
                <Spinner size="xs" />
                <Text fontSize="sm" color="gray.500">
                  Ładowanie szczegółów szablonu...
                </Text>
              </HStack>
            )}

            {selectedTemplateId && !loadingTemplateDetails && selectedTemplateCurrencies.length > 0 && (
              <FormControl isRequired>
                <FormLabel>Waluta kosztorysu</FormLabel>
                <Select
                  value={selectedCurrencyId}
                  onChange={(e) => setSelectedCurrencyId(e.target.value)}
                  placeholder="Wybierz walutę"
                >
                  {selectedTemplateCurrencies.map((currency) => (
                    <option key={currency.id} value={currency.id}>
                      {currency.name} ({currency.code}){currency.symbol ? ` - ${currency.symbol}` : ''}{currency.isDefault ? ' [Domyślna]' : ''}
                    </option>
                  ))}
                </Select>
                <Text fontSize="xs" color="gray.600" mt={1}>
                  Wybierz walutę, w której będzie prowadzony kosztorys
                </Text>
              </FormControl>
            )}

            {selectedTemplateId && !loadingTemplateDetails && selectedTemplateCurrencies.length === 0 && (
              <Alert status="error" borderRadius="md">
                <AlertIcon />
                <Text fontSize="sm">
                  Wybrany szablon nie ma zdefiniowanych walut. Skontaktuj się z administratorem.
                </Text>
              </Alert>
            )}

            {selectedTemplate && (
              <Box p={3} borderWidth="1px" borderRadius="md" bg="blue.50" borderColor="blue.200">
                <Text fontSize="sm" fontWeight="bold" mb={1}>
                  {selectedTemplate.name}
                </Text>
                {selectedTemplate.description && (
                  <Text fontSize="xs" color="gray.600" mb={2}>
                    {selectedTemplate.description}
                  </Text>
                )}
              </Box>
            )}

            <Box p={3} borderWidth="1px" borderRadius="md" bg="yellow.50" borderColor="yellow.200">
              <Text fontSize="xs" color="gray.600">
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
              colorScheme="blue"
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
