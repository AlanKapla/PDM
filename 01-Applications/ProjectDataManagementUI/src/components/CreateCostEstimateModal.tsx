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
import { costEstimateTemplateApi } from "../api/costEstimateTemplateApi";
import { costEstimateApi } from "../api/costEstimateApi";
import type { ApprovedTemplateVersionItem } from "../types/costEstimate.types";

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
  const [selectedVersionId, setSelectedVersionId] = useState("");
  const [selectedCurrencyId, setSelectedCurrencyId] = useState("");
  const [approvedVersions, setApprovedVersions] = useState<ApprovedTemplateVersionItem[]>([]);
  const [loadingVersions, setLoadingVersions] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Load approved versions when modal opens
  useEffect(() => {
    if (isOpen) {
      const loadApprovedVersions = async () => {
        setLoadingVersions(true);
        try {
          const data = await costEstimateTemplateApi.getAllApprovedVersions();
          setApprovedVersions(Array.isArray(data) ? data : []);
        } catch (error) {
          console.error("Error loading approved versions:", error);
          toast({
            title: "Błąd",
            description: "Nie udało się pobrać zatwierdzonych wersji szablonów",
            status: "error",
            duration: 5000,
          });
          setApprovedVersions([]);
        } finally {
          setLoadingVersions(false);
        }
      };
      loadApprovedVersions();
    }
  }, [isOpen, toast]);

  // Auto-select default currency when template version changes
  useEffect(() => {
    if (selectedVersionId) {
      const version = approvedVersions.find(v => v.versionId === selectedVersionId);
      if (version && version.currencies && version.currencies.length > 0) {
        const defaultCurrency = version.currencies.find(c => c.isDefault);
        if (defaultCurrency) {
          setSelectedCurrencyId(defaultCurrency.id);
        } else {
          // Jeśli brak domyślnej, wybierz pierwszą
          setSelectedCurrencyId(version.currencies[0].id);
        }
      } else {
        setSelectedCurrencyId("");
      }
    } else {
      setSelectedCurrencyId("");
    }
  }, [selectedVersionId, approvedVersions]);

  const handleClose = () => {
    setName("");
    setDescription("");
    setSelectedVersionId("");
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

    if (!selectedVersionId) {
      toast({
        title: "Błąd",
        description: "Wybierz wersję szablonu kosztorysu",
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

    const selectedVersion = approvedVersions.find(v => v.versionId === selectedVersionId);
    if (!selectedVersion) {
      toast({
        title: "Błąd",
        description: "Nie znaleziono wybranej wersji szablonu",
        status: "error",
        duration: 3000,
      });
      return;
    }

    const selectedCurrency = selectedVersion.currencies?.find(c => c.id === selectedCurrencyId);
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
        templateId: selectedVersion.templateId,
        templateVersionId: selectedVersion.versionId,
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

  const selectedVersion = approvedVersions.find((v) => v.versionId === selectedVersionId);

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
                Wersja szablonu kosztorysu
              </FormLabel>
              <Select
                value={selectedVersionId}
                onChange={(e) => setSelectedVersionId(e.target.value)}
                placeholder="Wybierz zatwierdzoną wersję szablonu"
                isDisabled={loadingVersions}
              >
                {approvedVersions.map((version) => (
                  <option key={version.versionId} value={version.versionId}>
                    {version.templateName} - v{version.versionNumber} ({version.templateCurrency || 'brak waluty'})
                  </option>
                ))}
              </Select>
              {loadingVersions && (
                <HStack mt={2} spacing={2}>
                  <Spinner size="xs" />
                  <Text fontSize="sm" color="gray.500">
                    Ładowanie zatwierdzonych wersji...
                  </Text>
                </HStack>
              )}
              
              {!loadingVersions && approvedVersions.length === 0 && (
                <Alert status="warning" mt={2} borderRadius="md">
                  <AlertIcon />
                  <Box>
                    <Text fontSize="sm" fontWeight="medium">
                      Brak zatwierdzonych wersji szablonów
                    </Text>
                    <Text fontSize="xs" color="gray.600">
                      Musisz najpierw utworzyć i zatwierdzić wersję szablonu kosztorysu.
                    </Text>
                  </Box>
                </Alert>
              )}
              
              {!loadingVersions && approvedVersions.length > 0 && (
                <Text fontSize="xs" color="gray.600" mt={1}>
                  Znaleziono {approvedVersions.length} {approvedVersions.length === 1 ? 'zatwierdzoną wersję' : 'zatwierdzonych wersji'}
                </Text>
              )}
            </FormControl>

            {selectedVersion && selectedVersion.currencies && selectedVersion.currencies.length > 0 && (
              <FormControl isRequired>
                <FormLabel>Waluta kosztorysu</FormLabel>
                <Select
                  value={selectedCurrencyId}
                  onChange={(e) => setSelectedCurrencyId(e.target.value)}
                  placeholder="Wybierz walutę"
                >
                  {selectedVersion.currencies.map((currency) => (
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

            {selectedVersion && selectedVersion.currencies && selectedVersion.currencies.length === 0 && (
              <Alert status="error" borderRadius="md">
                <AlertIcon />
                <Text fontSize="sm">
                  Wybrany szablon nie ma zdefiniowanych walut. Skontaktuj się z administratorem.
                </Text>
              </Alert>
            )}

            {selectedVersion && (
              <Box p={3} borderWidth="1px" borderRadius="md" bg="blue.50" borderColor="blue.200">
                <Text fontSize="sm" fontWeight="bold" mb={1}>
                  {selectedVersion.templateName}
                </Text>
                <HStack spacing={2} flexWrap="wrap" mb={2}>
                  <Badge colorScheme="green" fontSize="xs">
                    Zatwierdzona v{selectedVersion.versionNumber}
                  </Badge>
                  {selectedVersion.templateCurrency && (
                    <Badge colorScheme="purple" fontSize="xs">
                      {selectedVersion.templateCurrency}
                    </Badge>
                  )}
                </HStack>
                <VStack align="flex-start" spacing={1} fontSize="xs" color="gray.600">
                  <Text>
                    • Zatwierdzona: {new Date(selectedVersion.approvedAt).toLocaleDateString('pl-PL')}
                  </Text>
                  <Text>
                    • Zatwierdził: {selectedVersion.approvedByUserName}
                  </Text>
                </VStack>
                <Box mt={2} pt={2} borderTopWidth="1px" borderColor="blue.300">
                  <Text fontSize="xs" color="gray.600">
                    <strong>Struktura:</strong>
                  </Text>
                  {selectedVersion.templateStructure ? (
                    <VStack align="stretch" spacing={1} mt={1}>
                      {selectedVersion.templateStructure.canAddGroups && (
                        <Text fontSize="xs" color="gray.600">
                          • Można dodawać grupy{selectedVersion.templateStructure.maxGroupLevel ? ` (maks. poziom: ${selectedVersion.templateStructure.maxGroupLevel})` : ''}
                        </Text>
                      )}
                      {selectedVersion.templateStructure.canBranchGroups && (
                        <Text fontSize="xs" color="gray.600">
                          • Można tworzyć podgrupy
                        </Text>
                      )}
                      <Text fontSize="xs" color="gray.600">
                        • Pola kalkulowane: {selectedVersion.templateStructure.workScopeFieldsDefinition?.calculatedFields.length ?? 0}
                      </Text>
                      <Text fontSize="xs" color="gray.600">
                        • Pola dodatkowe: {selectedVersion.templateStructure.workScopeFieldsDefinition?.genericFields.length ?? 0}
                      </Text>
                    </VStack>
                  ) : (
                    <Text fontSize="xs" color="gray.500" mt={1}>
                      Struktura nie jest zdefiniowana
                    </Text>
                  )}
                </Box>
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
