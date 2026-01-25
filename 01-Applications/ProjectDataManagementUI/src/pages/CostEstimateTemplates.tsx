import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
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
  Badge,
} from "@chakra-ui/react";
import { FileText, Plus, Edit, Copy } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import {
  costEstimateTemplateApi,
  type CostEstimateTemplateListItem,
  type CostEstimateTemplateDetails,
} from "../api/costEstimateTemplateApi";
import { CostEstimateTemplateVersionStatus } from "../types/costEstimate.types";
import { convertFieldTypeToLegacy } from "../utils/fieldTypeLabels";

export default function CostEstimateTemplates() {
  const { showSuccess, showError } = useToastNotification();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);

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

  const handleEditTemplate = (templateId: string) => {
    navigate(`/cost-estimate-templates/${templateId}/edit`);
  };



  const handleDuplicateTemplate = async (templateId: string) => {
    try {
      const details = await costEstimateTemplateApi.getTemplateDetails(templateId);
      if (!details.selectedVersion) {
        showError('Nie można zduplikować szablonu', 'Szablon nie ma żadnej wersji');
        return;
      }
      
      // Pobierz pełną strukturę wersji
      const structure = await costEstimateTemplateApi.getTemplateVersionStructure(
        templateId,
        details.selectedVersion.id
      );
      
      // Mapuj pola z web modeli do DTO
      const groupHeaderFields = structure.groupHeaderFields.map(f => ({
        fieldName: f.id || crypto.randomUUID(),  // Używamy ID z backendu jako fieldName (GUID)
        fieldType: f.fieldType,
        label: f.customLabel || `Pole grupy`,
        isSortable: false,
        isFilterable: false,
      }));
      
      const systemFields = structure.systemFields.map(f => ({
        fieldName: f.fieldName,
        fieldType: convertFieldTypeToLegacy(f.fieldType), // FieldType (100-102) → SystemFieldType (0-2)
        label: f.label,
        isSortable: false,
        isFilterable: false,
      }));
      
      const calculatedFields = structure.calculatedFields.map(f => ({
        fieldName: f.fieldName,
        fieldType: convertFieldTypeToLegacy(f.fieldType), // FieldType (200-206) → CalculatedFieldType (0-6)
        label: f.label,
        isSortable: f.isSortable,
        isFilterable: f.isFilterable,
      }));
      
      const genericFields = structure.genericFields.map(f => ({
        fieldName: f.fieldName,
        fieldType: convertFieldTypeToLegacy(f.fieldType), // FieldType (300-305) → GenericFieldType (0-5)
        label: f.label,
        isSortable: f.isSortable,
        isFilterable: f.isFilterable,
      }));
      
      // Krok 1: Utwórz nowy szablon z nazwą i opisem
      const newTemplateId = await costEstimateTemplateApi.createTemplate({
        name: `${details.name} (kopia)`,
        description: details.description,
      });
      
      // Krok 2: Pobierz draft version ID nowo utworzonego szablonu
      const newTemplateDetails = await costEstimateTemplateApi.getTemplateDetails(newTemplateId);
      const draftVersionId = newTemplateDetails.selectedVersion?.id;
      
      if (!draftVersionId) {
        showError('Błąd', 'Nie można znaleźć wersji Draft nowego szablonu');
        return;
      }
      
      // Krok 3: Zaktualizuj nowy szablon całą strukturą
      await costEstimateTemplateApi.updateTemplate(newTemplateId, {
        templateId: newTemplateId,
        currentVersionId: draftVersionId,
        name: `${details.name} (kopia)`,
        description: details.description,
        category: details.category,
        canAddGroups: details.canAddGroups,
        canBranchGroups: details.canBranchGroups,
        maxGroupLevel: details.maxGroupLevel,
        autoNumberGroups: details.autoNumberGroups,
        groupNumberFormat: details.groupNumberFormat,
        updateStructure: true,
        currencies: structure.currencies.map(c => ({
          code: c.code,
          name: c.name,
          symbol: c.symbol,
          isDefault: c.isDefault,
          order: c.order,
        })),
        units: structure.units.map(u => ({
          code: u.code,
          name: u.name,
          symbol: u.symbol,
          category: u.category,
          isDefault: u.isDefault,
          order: u.order,
        })),
        groupHeaderFields,
        systemFields,
        calculatedFields,
        genericFields,
        summaryConfiguration: structure.summaryConfiguration ? {
          showGroupSummary: structure.summaryConfiguration.showGroupSummary,
          showTotalSummary: structure.summaryConfiguration.showTotalSummary,
          groupSummaryFields: structure.summaryConfiguration.groupSummaryFields.map(f => f.fieldName),
          totalSummaryFields: structure.summaryConfiguration.totalSummaryFields.map(f => f.fieldName),
        } : undefined,
        uiConfiguration: structure.uiConfiguration ? {
          columnLayout: structure.uiConfiguration.columns.map(col => col.fieldName),
          columnWidths: Object.fromEntries(
            structure.uiConfiguration.columns
              .filter(col => col.width)
              .map(col => [col.fieldName, col.width!])
          ),
        } : undefined,
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
              onClick={() => navigate('/cost-estimate-templates/new')}
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
                  onClick={() => handleEditTemplate(template.id)}
                >
                  <CardBody>
                    <VStack align="stretch" spacing={3}>
                      <HStack justify="space-between">
                        <HStack spacing={2}>
                          <FileText size={20} />
                          <VStack align="start" spacing={0}>
                            <Text fontWeight="bold" fontSize="md">
                              {template.name}
                            </Text>
                            <HStack spacing={2}>
                              {template.latestVersionNumber && (
                                <Badge fontSize="xs">
                                  v{template.latestVersionNumber}
                                </Badge>
                              )}
                              {template.latestVersionStatus !== undefined && (
                                <Badge
                                  colorScheme={
                                    template.latestVersionStatus === CostEstimateTemplateVersionStatus.Approved
                                      ? "green"
                                      : "gray"
                                  }
                                  fontSize="xs"
                                >
                                  {template.latestVersionStatus === CostEstimateTemplateVersionStatus.Approved
                                    ? "Zatwierdzony"
                                    : "Szkic"}
                                </Badge>
                              )}
                            </HStack>
                          </VStack>
                        </HStack>
                        <HStack spacing={1} flexShrink={0}>
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
                        </HStack>
                      </HStack>

                      {template.description && (
                        <Text fontSize="sm" color="gray.600" noOfLines={2}>
                          {template.description}
                        </Text>
                      )}

                      <VStack align="stretch" spacing={1} fontSize="xs" color="gray.500">
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


      </Box>
    </MainLayout>
  );
}
