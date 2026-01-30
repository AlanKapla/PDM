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
import { costEstimateTemplateApi, type CostEstimateTemplateListItem, type CostEstimateTemplateDetails } from "../api/costEstimateTemplateApi";
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
  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);
  const [selectedTemplateDetails, setSelectedTemplateDetails] = useState<CostEstimateTemplateDetails | null>(null);
  const [loadingTemplates, setLoadingTemplates] = useState(false);
  const [loadingTemplateDetails, setLoadingTemplateDetails] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Load templates when modal opens
  useEffect(() => {
    if (isOpen) {
      const loadTemplates = async () => {
        setLoadingTemplates(true);
        try {
          const data = await costEstimateTemplateApi.getTemplates();
          setTemplates(Array.isArray(data) ? data : []);
        } catch (error) {
          console.error("Error loading templates:", error);
          toast({
            title: "Błąd",
            description: "Nie udało się pobrać szablonów",
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

  const handleClose = () => {
    setName("");
    setDescription("");
    setSelectedTemplateId("");
    setSelectedTemplateDetails(null);
    onClose();
  };

  const handleTemplateChange = async (templateId: string) => {
    setSelectedTemplateId(templateId);
    
    if (!templateId) {
      setSelectedTemplateDetails(null);
      return;
    }

    setLoadingTemplateDetails(true);
    try {
      const details = await costEstimateTemplateApi.getTemplateDetails(templateId);
      setSelectedTemplateDetails(details);
    } catch (error) {
      console.error('Error loading template details:', error);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać szczegółów szablonu",
        status: "error",
        duration: 5000,
      });
      setSelectedTemplateDetails(null);
    } finally {
      setLoadingTemplateDetails(false);
    }
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

    setIsSubmitting(true);

    try {
      // Backend tworzy pusty kosztorys na podstawie szablonu
      await costEstimateApi.createCostEstimate(tenantId, projectId, {
        templateId: selectedTemplateId,
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
                onChange={(e) => handleTemplateChange(e.target.value)}
                placeholder="Wybierz szablon"
                isDisabled={loadingTemplates || loadingTemplateDetails}
                >
                {templates.map((template) => (
                  <option key={template.id} value={template.id}>
                    {template.name}
                  </option>
                ))}
              </Select>              {loadingTemplates && (
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
                      Musisz najpierw utworzyć szablon kosztorysu w sekcji Szablony kosztorysów.
                    </Text>
                  </Box>
                </Alert>
              )}
              
              {!loadingTemplates && templates.length > 0 && (
                <Text fontSize="xs" color="gray.600" mt={1}>
                  Znaleziono {templates.length} {templates.length === 1 ? 'szablon' : 'szablonów'}
                </Text>
              )}
              {loadingTemplateDetails && (
                <Text fontSize="xs" color="gray.500" mt={1}>
                  Ładowanie szczegółów szablonu...
                </Text>
              )}
            </FormControl>

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
                <HStack spacing={2} flexWrap="wrap">
                  <Badge colorScheme="blue" fontSize="xs">
                    Szablon
                  </Badge>
                  <Text fontSize="xs" color="gray.500">
                    Autor: {selectedTemplate.ownerName}
                  </Text>
                </HStack>
                
                {selectedTemplateDetails && (
                  <Box mt={2} pt={2} borderTopWidth="1px" borderColor="blue.300">
                    <Text fontSize="xs" color="gray.600">
                      <strong>Struktura:</strong>
                    </Text>
                    <VStack align="stretch" spacing={1} mt={1}>
                      {selectedTemplateDetails.templateStructure.canAddGroups && (
                        <Text fontSize="xs" color="gray.600">
                          • Można dodawać grupy{selectedTemplateDetails.templateStructure.maxGroupLevel ? ` (maks. poziom: ${selectedTemplateDetails.templateStructure.maxGroupLevel})` : ''}
                        </Text>
                      )}
                      {selectedTemplateDetails.templateStructure.canBranchGroups && (
                        <Text fontSize="xs" color="gray.600">
                          • Można tworzyć podgrupy
                        </Text>
                      )}
                      <Text fontSize="xs" color="gray.600">
                        • Pola kalkulowane: {selectedTemplateDetails.templateStructure.workScopeFieldsDefinition.calculatedFields.length}
                      </Text>
                      <Text fontSize="xs" color="gray.600">
                        • Pola dodatkowe: {selectedTemplateDetails.templateStructure.workScopeFieldsDefinition.genericFields.length}
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
