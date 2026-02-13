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
import { costEstimateTemplateApi, type CostEstimateTemplateListItem, type CostEstimateTemplateStructureWeb, type CurrencyWeb } from "../api/costEstimateTemplateApi";
import { costEstimateApi } from "../api/costEstimateApi";

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
  const [templates, setTemplates] = useState<TemplateWithStructure[]>([]);
  const [loadingTemplates, setLoadingTemplates] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Load templates when modal opens
  useEffect(() => {
    if (isOpen) {
      const loadTemplates = async () => {
        setLoadingTemplates(true);
        try {
          const templateList = await costEstimateTemplateApi.getTemplates();
          // Pobierz szczegóły dla każdego szablonu, aby uzyskać strukturę i waluty
          const templatesWithStructure: TemplateWithStructure[] = await Promise.all(
            templateList.map(async (t) => {
              try {
                const details = await costEstimateTemplateApi.getTemplateDetails(t.id);
                return {
                  ...t,
                  structure: details.structure,
                  currencies: details.structure?.currencies || [],
                };
              } catch {
                return { ...t, currencies: [] };
              }
            })
          );
          setTemplates(templatesWithStructure);
        } catch (error) {
          console.error("Error loading templates:", error);
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

  // Auto-select default currency when template changes
  useEffect(() => {
    if (selectedTemplateId) {
      const template = templates.find(t => t.id === selectedTemplateId);
      if (template && template.currencies && template.currencies.length > 0) {
        const defaultCurrency = template.currencies.find(c => c.isDefault);
        if (defaultCurrency) {
          setSelectedCurrencyId(defaultCurrency.id);
        } else {
          // Jeśli brak domyślnej, wybierz pierwszą
          setSelectedCurrencyId(template.currencies[0].id);
        }
      } else {
        setSelectedCurrencyId("");
      }
    } else {
      setSelectedCurrencyId("");
    }
  }, [selectedTemplateId, templates]);

  const handleClose = () => {
    setName("");
    setDescription("");
    setSelectedTemplateId("");
    setSelectedCurrencyId("");
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

    const selectedCurrency = selectedTemplate.currencies?.find(c => c.id === selectedCurrencyId);
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
    <Modal isOpen={isOpen} onClose={handleClose} size="xl">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>
          <HStack spacing={3}>
            <FileText size={24} />
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
                <Text fontSize="xs" color="gray.600" mt={1}>
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
              <Box p={3} borderWidth="1px" borderRadius="md" bg="blue.50" borderColor="blue.200">
                <Text fontSize="sm" fontWeight="bold" mb={1}>
                  {selectedTemplate.name}
                </Text>
                {selectedTemplate.description && (
                  <Text fontSize="xs" color="gray.600" mb={2}>
                    {selectedTemplate.description}
                  </Text>
                )}
                {selectedTemplate.structure && (
                  <Box mt={2} pt={2} borderTopWidth="1px" borderColor="blue.300">
                    <Text fontSize="xs" color="gray.600">
                      <strong>Struktura:</strong>
                    </Text>
                    <VStack align="stretch" spacing={1} mt={1}>
                      <Text fontSize="xs" color="gray.600">
                        • Pola nagłówka grupy: {selectedTemplate.structure.groupHeaderFields?.length ?? 0}
                      </Text>
                      <Text fontSize="xs" color="gray.600">
                        • Pola kalkulowane: {selectedTemplate.structure.calculatedFields?.length ?? 0}
                      </Text>
                      <Text fontSize="xs" color="gray.600">
                        • Pola dodatkowe: {selectedTemplate.structure.genericFields?.length ?? 0}
                      </Text>
                      <Text fontSize="xs" color="gray.600">
                        • Waluty: {selectedTemplate.structure.currencies?.length ?? 0}
                      </Text>
                    </VStack>
                  </Box>
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
