import { useState, useEffect } from "react";
import type { MouseEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Button,
  Card,
  CardBody,
  SimpleGrid,
  IconButton,
  Tooltip,
  Badge,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useDisclosure,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalFooter,
  ModalBody,
  ModalCloseButton,
  FormControl,
  FormLabel,
  Input,
  Textarea,
} from "@chakra-ui/react";
import { FileText, Plus, Edit, Copy, Trash2 } from "lucide-react";
import { useRef } from "react";
import MainLayout from "../layout/MainLayout";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import { handleApiError } from "../utils/handleApiError";
import {
  costEstimateTemplateApi,
  type CostEstimateTemplateListItem,
} from "../api/costEstimateTemplateApi";

export default function CostEstimateTemplates() {
  const { showApiSuccess, showError } = useToastNotification();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);
  const [templateToDelete, setTemplateToDelete] = useState<CostEstimateTemplateListItem | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();

  // Stan dla modalu duplikowania
  const [templateToDuplicate, setTemplateToDuplicate] = useState<CostEstimateTemplateListItem | null>(null);
  const [duplicateName, setDuplicateName] = useState("");
  const [duplicateDescription, setDuplicateDescription] = useState("");
  const [isDuplicating, setIsDuplicating] = useState(false);
  const { isOpen: isDuplicateOpen, onOpen: onDuplicateOpen, onClose: onDuplicateClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    fetchTemplates();
  }, []);

  const fetchTemplates = async () => {
    setLoading(true);
    try {
      const data = await costEstimateTemplateApi.getTemplates();
      setTemplates(Array.isArray(data) ? data : []);
    } catch (error: unknown) {
      const { title, description } = handleApiError(error);
      showError(title, description);
      setTemplates([]);
    } finally {
      setLoading(false);
    }
  };

  const handleEditTemplate = (templateId: string) => {
    navigate(`/cost-estimate-templates/${templateId}/edit`);
  };

  const openDeleteConfirmation = (template: CostEstimateTemplateListItem, e: MouseEvent) => {
    e.stopPropagation();
    setTemplateToDelete(template);
    onDeleteOpen();
  };

  const handleDeleteTemplate = async () => {
    if (!templateToDelete) return;
    
    setIsDeleting(true);
    try {
      await costEstimateTemplateApi.deleteTemplate(templateToDelete.id);
      showApiSuccess('deleted');
      onDeleteClose();
      setTemplateToDelete(null);
      fetchTemplates();
    } catch (error: unknown) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setIsDeleting(false);
    }
  };

  const openDuplicateModal = (template: CostEstimateTemplateListItem, e: MouseEvent) => {
    e.stopPropagation();
    setTemplateToDuplicate(template);
    setDuplicateName(`Kopia — ${template.name}`);
    setDuplicateDescription(template.description || "");
    onDuplicateOpen();
  };

  const handleCloseDuplicateModal = () => {
    onDuplicateClose();
    setTemplateToDuplicate(null);
    setDuplicateName("");
    setDuplicateDescription("");
  };

  const handleDuplicateTemplate = async () => {
    if (!templateToDuplicate) return;
    if (!duplicateName.trim()) {
      showError('Sprawdź formularz', 'Wprowadź nazwę dla nowego szablonu');
      return;
    }

    setIsDuplicating(true);
    try {
      const newTemplateId = await costEstimateTemplateApi.duplicateTemplate(templateToDuplicate.id, {
        name: duplicateName.trim(),
        description: duplicateDescription.trim() || undefined,
      });
      
      showApiSuccess('estimateCopied');
      handleCloseDuplicateModal();
      // Przekieruj do edycji nowego szablonu
      navigate(`/cost-estimate-templates/${newTemplateId}/edit`);
    } catch (error: unknown) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setIsDuplicating(false);
    }
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
          <HStack justify="space-between">
            <Heading size="lg">Szablony kosztorysów</Heading>
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="primary"
              onClick={() => navigate('/cost-estimate-templates/select')}
            >
              Nowy szablon
            </Button>
          </HStack>

          <Text fontSize="sm" color="neutral.600">
            Szablony kosztorysów pozwalają na szybkie tworzenie nowych kosztorysów z predefiniowanymi polami i strukturą
          </Text>

          {templates.length === 0 ? (
            <EmptyState
              icon={FileText}
              title="Brak szablonów"
              description="Utwórz pierwszy szablon kosztorysu"
            />
          ) : (
            <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={4}>
              {templates.map((template) => (
                <Card
                  key={template.id}
                  bg="white"
                  borderWidth="1px"
                  borderColor="neutral.200"
                  _hover={{ bg: "neutral.25", borderColor: "primary.300" }}
                  transition="all 0.2s"
                  cursor="pointer"
                  onClick={() => handleEditTemplate(template.id)}
                >
                  <CardBody>
                    <VStack align="stretch" spacing={3}>
                      <HStack justify="space-between">
                        <HStack spacing={2}>
                          <FileText size={20} />
                          <Text fontWeight="bold" fontSize="md">
                            {template.name}
                          </Text>
                        </HStack>
                        <HStack spacing={1} flexShrink={0}>
                          <Tooltip label="Duplikuj szablon">
                            <IconButton
                              aria-label="Duplikuj"
                              icon={<Copy size={16} />}
                              size="sm"
                              variant="ghost"
                              onClick={(e) => openDuplicateModal(template, e)}
                            />
                          </Tooltip>
                          <Tooltip label="Usuń szablon">
                            <IconButton
                              aria-label="Usuń"
                              icon={<Trash2 size={16} />}
                              size="sm"
                              variant="ghost"
                              colorScheme="red"
                              onClick={(e) => openDeleteConfirmation(template, e)}
                            />
                          </Tooltip>
                        </HStack>
                      </HStack>

                      {template.description && (
                        <Text fontSize="sm" color="neutral.600" noOfLines={2}>
                          {template.description}
                        </Text>
                      )}

                      <VStack align="stretch" spacing={1} fontSize="xs" color="neutral.500">
                        <Text>Utworzony: {formatDate(template.createdAt)}</Text>
                        {template.updatedAt && <Text>Zaktualizowano: {formatDate(template.updatedAt)}</Text>}
                      </VStack>
                    </VStack>
                  </CardBody>
                </Card>
              ))}
            </SimpleGrid>
          )}
        </VStack>

        {/* Modal potwierdzenia usunięcia */}
        <AlertDialog
          isOpen={isDeleteOpen}
          leastDestructiveRef={cancelRef}
          onClose={onDeleteClose}
        >
          <AlertDialogOverlay>
            <AlertDialogContent>
              <AlertDialogHeader fontSize="lg" fontWeight="bold">
                Usuń szablon
              </AlertDialogHeader>

              <AlertDialogBody>
                Czy na pewno chcesz usunąć szablon <strong>{templateToDelete?.name}</strong>?
                <Text mt={2} fontSize="sm" color="neutral.600">
                  Istniejące kosztorysy korzystające z tego szablonu nadal będą działać.
                </Text>
              </AlertDialogBody>

              <AlertDialogFooter>
                <Button ref={cancelRef} onClick={onDeleteClose} isDisabled={isDeleting} variant="ghost" colorScheme="gray">
                  Anuluj
                </Button>
                <Button
                  colorScheme="red"
                  onClick={handleDeleteTemplate}
                  ml={3}
                  isLoading={isDeleting}
                  loadingText="Usuwanie..."
                >
                  Usuń
                </Button>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialogOverlay>
        </AlertDialog>

        {/* Modal duplikowania szablonu */}
        <Modal isOpen={isDuplicateOpen} onClose={handleCloseDuplicateModal} size={{ base: "full", md: "md" }}>
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>Duplikuj szablon</ModalHeader>
            <ModalCloseButton />
            <ModalBody pb={6}>
              <VStack spacing={4}>
                <FormControl isRequired>
                  <FormLabel>Nazwa nowego szablonu</FormLabel>
                  <Input
                    value={duplicateName}
                    onChange={(e) => setDuplicateName(e.target.value)}
                    placeholder="Wprowadź nazwę"
                    maxLength={200}
                  />
                </FormControl>
                <FormControl>
                  <FormLabel>Opis</FormLabel>
                  <Textarea
                    value={duplicateDescription}
                    onChange={(e) => setDuplicateDescription(e.target.value)}
                    placeholder="Opcjonalny opis szablonu"
                    maxLength={2000}
                    rows={3}
                  />
                </FormControl>
                <Text fontSize="sm" color="neutral.600">
                  Wszystkie pola, waluty i jednostki zostaną skopiowane do nowego szablonu.
                </Text>
              </VStack>
            </ModalBody>
            <ModalFooter>
              <Button onClick={handleCloseDuplicateModal} mr={3} isDisabled={isDuplicating} variant="ghost" colorScheme="gray">
                Anuluj
              </Button>
              <Button
                colorScheme="primary"
                onClick={handleDuplicateTemplate}
                isLoading={isDuplicating}
                loadingText="Duplikowanie..."
                isDisabled={!duplicateName.trim()}
              >
                Duplikuj
              </Button>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </Box>
    </MainLayout>
  );
}
