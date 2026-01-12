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
} from "@chakra-ui/react";
import { FileText, Plus, Edit, Trash2, Copy } from "lucide-react";
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

  const { isOpen: isCreateModalOpen, onOpen: onCreateModalOpen, onClose: onCreateModalClose } = useDisclosure();
  const { isOpen: isEditModalOpen, onOpen: onEditModalOpen, onClose: onEditModalClose } = useDisclosure();

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

  const handleDeleteTemplate = async (templateId: string) => {
    if (!confirm("Czy na pewno chcesz usunąć ten szablon?")) return;

    try {
      await costEstimateTemplateApi.deleteTemplate(templateId);
      showSuccess("Szablon został usunięty");
      fetchTemplates();
    } catch (error: any) {
      console.error('Error deleting template:', error);
      showError('Nie udało się usunąć szablonu', error?.message || 'Wystąpił nieoczekiwany błąd');
    }
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
      <Box p={{ base: 3, sm: 4, md: 8 }}>
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
                                handleDeleteTemplate(template.id);
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
      </Box>
    </MainLayout>
  );
}
