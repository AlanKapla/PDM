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
  FormControl,
  FormLabel,
  Input,
  Textarea,
  Text,
  Alert,
  AlertIcon,
} from "@chakra-ui/react";
import { Plus } from "lucide-react";
import { costEstimateApi } from "../api/costEstimateApi";
import { useToastNotification } from "../hooks/useToastNotification";
import { useProjectDetails } from "../hooks/queries";

interface CreateCostEstimateModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  onCostEstimateCreated: () => void;
}

const iconSize = 20;

/**
 * Modal tworzenia nowego kosztorysu.
 * 
 * Od wersji schema-based: kosztorysy nie wymagają szablonu.
 * Każdy nowy kosztorys otrzymuje domyślny schemat z 10 podstawowymi polami.
 */
export default function CreateCostEstimateModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  onCostEstimateCreated,
}: CreateCostEstimateModalProps) {
  const { showSuccess, showError, showApiError } = useToastNotification();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Waluta projektu — do wyświetlenia (read-only). Backend pobierze ją z ProjectCurrency.
  const { data: projectDetails } = useProjectDetails(tenantId, projectId);
  const projectCurrency = projectDetails?.currency;

  const handleClose = (): void => {
    setName("");
    setDescription("");
    onClose();
  };

  const handleSubmit = async (): Promise<void> => {
    if (!name.trim()) {
      showError("Błąd walidacji", "Nazwa kosztorysu jest wymagana");
      return;
    }

    setIsSubmitting(true);
    try {
      const desc = description.trim();
      await costEstimateApi.createCostEstimate(
        tenantId,
        projectId,
        {
          name: name.trim(),
          description: desc || undefined,
        }
      );

      showSuccess(
        "Kosztorys utworzony",
        `Kosztorys "${name.trim()}" został pomyślnie utworzony z domyślnym schematem pól.`
      );

      handleClose();
      onCostEstimateCreated();
    } catch (error) {
      showApiError(error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent): void => {
    if (e.key === "Enter" && e.ctrlKey && !isSubmitting) {
      handleSubmit();
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size="lg">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Nowy kosztorys</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            {/* Info Alert */}
            <Alert status="info" borderRadius="md">
              <AlertIcon />
              <Text fontSize="sm">
                Nowy kosztorys zostanie utworzony z domyślnym zestawem kolumn. 
                Możesz później zarządzać kolumnami poprzez przycisk "Kolumny" w edytorze.
              </Text>
            </Alert>

            {/* Project Currency Display */}
            {projectCurrency && (
              <FormControl>
                <FormLabel fontSize="sm" color="neutral.600">
                  Waluta projektu
                </FormLabel>
                <Text fontWeight="medium">
                  {projectCurrency.name} ({projectCurrency.code})
                </Text>
              </FormControl>
            )}

            {/* Name Input */}
            <FormControl isRequired>
              <FormLabel fontSize="sm">Nazwa kosztorysu</FormLabel>
              <Input
                placeholder="np. Kosztorys budowy domu"
                value={name}
                onChange={(e) => setName(e.target.value)}
                onKeyDown={handleKeyDown}
                autoFocus
              />
            </FormControl>

            {/* Description Textarea */}
            <FormControl>
              <FormLabel fontSize="sm">Opis (opcjonalnie)</FormLabel>
              <Textarea
                placeholder="Dodatkowe informacje o kosztorysie..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
              />
            </FormControl>
          </VStack>
        </ModalBody>

        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={handleClose} isDisabled={isSubmitting}>
            Anuluj
          </Button>
          <Button
            leftIcon={<Plus size={iconSize} />}
            colorScheme="primary"
            onClick={handleSubmit}
            isLoading={isSubmitting}
            loadingText="Tworzenie..."
          >
            Utwórz kosztorys
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
