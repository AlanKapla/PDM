import { useState, useEffect } from "react";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Button,
  useColorModeValue,
  Card,
  CardBody,
  SimpleGrid,
  IconButton,
  Tooltip,
  useDisclosure,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Alert,
  AlertIcon,
  AlertTitle,
  AlertDescription,
} from "@chakra-ui/react";
import { FileText, Plus, Edit, Trash2, Copy, AlertTriangle } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import CreateCustomTemplateModal from "../components/CreateCustomTemplateModal.tsx";
import {
  costEstimateTemplateApi,
  type CostEstimateTemplateListItem,
  type CostEstimateTemplateDetails,
} from "../api/costEstimateTemplateApi";

export default function CostEstimateTemplates() {
  const { showSuccess, showError } = useToastNotification();

  const [loading, setLoading] = useState(true);
  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<CostEstimateTemplateDetails | null>(null);
  const [templateToDelete, setTemplateToDelete] = useState<{ id: string; name: string } | null>(null);
  const [deleting, setDeleting] = useState(false);

  const { isOpen: isCreateModalOpen, onOpen: onCreateModalOpen, onClose: onCreateModalClose } = useDisclosure();
  const { isOpen: isEditModalOpen, onOpen: onEditModalOpen, onClose: onEditModalClose } = useDisclosure();
  const { isOpen: isDeleteModalOpen, onOpen: onDeleteModalOpen, onClose: onDeleteModalClose } = useDisclosure();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    fetchTemplates();
  }, []);

  const fetchTemplates = async () => {
    setLoading(true);
    try {
      const data = await costEstimateTemplateApi.getTemplates();
      setTemplates(Array.isArray(data) ? data : []);
    } catch (error: any) {
      console.error('Error fetching templates:', error);
      showError('Nie udało się załadować szablonów', error?.message || 'Wystąpił nieoczekiwany błąd');
      setTemplates([]);
    } finally {
      setLoading(false);
    }
  };

  const handleEditTemplate = async (templateId: string) => {
    try {
      const details = await costEstimateTemplateApi.getTemplateDetails(templateId);
      setSelectedTemplate(details);
      onEditModalOpen();
    } catch (error: any) {
      console.error('Error loading template details:', error);
      showError('Nie udało się załadować szablonu', error?.message || 'Wystąpił nieoczekiwany błąd');
    }
  };

  const handleDeleteTemplate = async () => {
    if (!templateToDelete) return;

    setDeleting(true);
    try {
      await costEstimateTemplateApi.deleteTemplate(templateToDelete.id);
      showSuccess("Szablon został usunięty");
      fetchTemplates();
      onDeleteModalClose();
      setTemplateToDelete(null);
    } catch (error: any) {
      console.error('Error deleting template:', error);
      showError('Nie udało się usunąć szablonu', error?.message || 'Wystąpił nieoczekiwany błąd');
    } finally {
      setDeleting(false);
    }
  };

  const openDeleteModal = (templateId: string, templateName: string) => {
    setTemplateToDelete({ id: templateId, name: templateName });
    onDeleteModalOpen();
  };

  const handleDuplicateTemplate = async (templateId: string) => {
    try {
      const details = await costEstimateTemplateApi.getTemplateDetails(templateId);
      await costEstimateTemplateApi.createTemplate({
        name: `${details.name} (kopia)`,
        description: details.description,
        templateStructure: details.templateStructure,
      });
      showSuccess("Szablon został zduplikowany");
      fetchTemplates();
    } catch (error: any) {
      console.error('Error duplicating template:', error);
      showError('Nie udało się zduplikować szablonu', error?.message || 'Wystąpił nieoczekiwany błąd');
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
      <Box p={8}>
        <VStack spacing={6} align="stretch">
          <HStack justify="space-between">
            <Heading size="lg">Szablony kosztorysów</Heading>
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="green"
              onClick={onCreateModalOpen}
            >
              Nowy szablon
            </Button>
          </HStack>

          <Text fontSize="sm" color="gray.600">
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
                  bg={cardBg}
                  borderWidth="1px"
                  borderColor={borderColor}
                  _hover={{ bg: hoverBg, transform: "translateY(-2px)" }}
                  transition="all 0.2s"
                  cursor="pointer"
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
                        <HStack spacing={1}>
                          <Tooltip label="Edytuj szablon">
                            <IconButton
                              aria-label="Edytuj"
                              icon={<Edit size={16} />}
                              size="sm"
                              variant="ghost"
                              onClick={(e) => {
                                e.stopPropagation();
                                handleEditTemplate(template.id);
                              }}
                            />
                          </Tooltip>
                          <Tooltip label="Duplikuj szablon">
                            <IconButton
                              aria-label="Duplikuj"
                              icon={<Copy size={16} />}
                              size="sm"
                              variant="ghost"
                              onClick={(e) => {
                                e.stopPropagation();
                                handleDuplicateTemplate(template.id);
                              }}
                            />
                          </Tooltip>
                          <Tooltip label="Usuń szablon">
                            <IconButton
                              aria-label="Usuń"
                              icon={<Trash2 size={16} />}
                              size="sm"
                              variant="ghost"
                              colorScheme="red"
                              onClick={(e) => {
                                e.stopPropagation();
                                openDeleteModal(template.id, template.name);
                              }}
                            />
                          </Tooltip>
                        </HStack>
                      </HStack>

                      {template.description && (
                        <Text fontSize="sm" color="gray.600" noOfLines={2}>
                          {template.description}
                        </Text>
                      )}

                      <VStack align="stretch" spacing={1} fontSize="xs" color="gray.500">
                        <Text>Utworzony: {formatDate(template.createdAt)}</Text>
                        <Text>Autor: {template.ownerName}</Text>
                        {template.updatedAt && <Text>Zaktualizowano: {formatDate(template.updatedAt)}</Text>}
                      </VStack>
                    </VStack>
                  </CardBody>
                </Card>
              ))}
            </SimpleGrid>
          )}
        </VStack>

        {/* MODAL: CREATE TEMPLATE */}
        <CreateCustomTemplateModal
          isOpen={isCreateModalOpen}
          onClose={onCreateModalClose}
          onTemplateCreated={fetchTemplates}
        />

        {/* MODAL: EDIT TEMPLATE */}
        {selectedTemplate && (
          <CreateCustomTemplateModal
            isOpen={isEditModalOpen}
            onClose={() => {
              onEditModalClose();
              setSelectedTemplate(null);
            }}
            onTemplateCreated={fetchTemplates}
            existingTemplate={selectedTemplate}
          />
        )}

        {/* MODAL: DELETE CONFIRMATION */}
        <Modal isOpen={isDeleteModalOpen} onClose={onDeleteModalClose} isCentered>
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>
              <HStack spacing={2}>
                <AlertTriangle size={24} color="red" />
                <Text>Usuń szablon kosztorysu</Text>
              </HStack>
            </ModalHeader>
            <ModalCloseButton />
            <ModalBody>
              <VStack spacing={4} align="stretch">
                <Text>
                  Czy na pewno chcesz usunąć szablon <Text as="span" fontWeight="bold">"{templateToDelete?.name}"</Text>?
                </Text>
                
                <Alert status="warning" borderRadius="md">
                  <AlertIcon />
                  <Box>
                    <AlertTitle fontSize="sm">Uwaga!</AlertTitle>
                    <AlertDescription fontSize="sm">
                      Usunięcie szablonu spowoduje również usunięcie wszystkich kosztorysów utworzonych na jego podstawie. 
                      Ta operacja jest nieodwracalna.
                    </AlertDescription>
                  </Box>
                </Alert>
              </VStack>
            </ModalBody>
            <ModalFooter gap={2}>
              <Button variant="ghost" onClick={onDeleteModalClose} isDisabled={deleting}>
                Anuluj
              </Button>
              <Button 
                colorScheme="red" 
                onClick={handleDeleteTemplate}
                isLoading={deleting}
                loadingText="Usuwanie..."
              >
                Usuń szablon
              </Button>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </Box>
    </MainLayout>
  );
}
