import React, { useState, useEffect, useContext } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Heading,
  HStack,
  VStack,
  Button,
  IconButton,
  Text,
  Badge,
  Divider,
  useToast,
  Spinner,
  Alert,
  AlertIcon,
  AlertTitle,
  AlertDescription,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  Table,
  Tbody,
  Tr,
  Td,
} from '@chakra-ui/react';
import {
  ArrowLeft,
  Save,
  Plus,
  Trash2,
  FolderTree,
  Calculator,
  FileText,
  Eye,
  Edit,
} from 'lucide-react';
import { costEstimateApi } from '../api/costEstimateApi';
import { costEstimateTemplateApi } from '../api/costEstimateTemplateApi';
import type {
  CostEstimateDetails,
  CostEstimateDataModel,
  CostEstimateGroup,
  CostEstimateWorkScope,
  CostEstimateStatus,
} from '../types/costEstimate.types';
import type { CostEstimateTemplateDetails } from '../api/costEstimateTemplateApi';
import { useCalculations } from '../hooks/useCalculations';
import { formatCalculatedValue } from '../utils/calculationEngine';
import { formatDate } from '../utils/formatters';
import { useResourcePermissions } from '../hooks/useResourcePermissions';

import { CostEstimateTable } from '../components/CostEstimateTable';
import { CostEstimateViewer } from '../components/CostEstimateViewer';
import { CostEstimateExcelView } from '../components/CostEstimateExcelView';
import { AuthContext } from '../context/AuthContext';
import { convertDataModelFromBackend, convertDataModelForBackend } from '../utils/enumMapper';

const costEstimateStatusLabels: Record<CostEstimateStatus, string> = {
  0: 'Roboczy',
  1: 'W trakcie',
  2: 'Do przeglądu',
  3: 'Zatwierdzony',
  4: 'Odrzucony',
  5: 'Zarchiwizowany',
};

const costEstimateStatusColors: Record<CostEstimateStatus, string> = {
  0: 'gray',
  1: 'blue',
  2: 'orange',
  3: 'green',
  4: 'red',
  5: 'purple',
};

export const CostEstimateEditor: React.FC = () => {
  const { projectId, estimateId } = useParams<{ projectId: string; estimateId: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  const { user } = useContext(AuthContext);
  const permissions = useResourcePermissions(projectId);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [estimate, setEstimate] = useState<CostEstimateDetails | null>(null);
  const [template, setTemplate] = useState<CostEstimateTemplateDetails | null>(null);
  const [dataModel, setDataModel] = useState<CostEstimateDataModel | null>(null);
  const [projectName, setProjectName] = useState<string>('');
  const [hasChanges, setHasChanges] = useState(false);
  const [viewMode, setViewMode] = useState<'edit' | 'preview'>('edit');

  const calculations = useCalculations({
    calculatedFields: template?.templateStructure.workScopeFieldsDefinition.calculatedFields || [],
    genericFields: template?.templateStructure.workScopeFieldsDefinition.genericFields || [],
    summaryConfig: template?.templateStructure.summaryConfiguration,
  });

  // Load estimate and template
  useEffect(() => {
    const fetchData = async () => {
      if (!projectId || !estimateId || !user?.activeTenantId) return;

      setLoading(true);
      try {
        // Load estimate details
        const estimateData = await costEstimateApi.getCostEstimateDetails(
          user.activeTenantId,
          projectId,
          estimateId
        );
        setEstimate(estimateData);
        // Konwertuj numeryczne enumy z backendu na stringi
        const convertedData = convertDataModelFromBackend(estimateData.data);
        
        // Load template structure
        const templateData = await costEstimateTemplateApi.getTemplateDetails(
          estimateData.templateId
        );
        setTemplate(templateData);

        // Zapisz oryginalne wartości UnitPriceNet dla każdego workScope
        // To umożliwi przywrócenie wartości po odznaczeniu opcji kolekcji
        const mainUnitPriceNetField = templateData.templateStructure.workScopeFieldsDefinition.calculatedFields?.find((f: any) => f.type === 0);
        
        if (mainUnitPriceNetField) {
          const saveOriginalValues = (groups: CostEstimateGroup[]): CostEstimateGroup[] => {
            return groups.map((group) => ({
              ...group,
              workScopes: group.workScopes.map((ws) => ({
                ...ws,
                _originalUnitPriceNet: ws.calculatedFieldValues[mainUnitPriceNetField.name],
              } as any)),
              subGroups: group.subGroups ? saveOriginalValues(group.subGroups) : undefined,
            }));
          };

          const dataWithOriginals = {
            ...convertedData,
            groups: saveOriginalValues(convertedData.groups),
          };
          
          // Przelicz model, aby obliczyć totals
          const recalculated = calculations.recalculateAll(dataWithOriginals);
          setDataModel(recalculated);
        } else {
          // Przelicz model, aby obliczyć totals
          const recalculated = calculations.recalculateAll(convertedData);
          setDataModel(recalculated);
        }
        
        setProjectName(estimateData.projectName);
      } catch (error: any) {
        toast({
          title: 'Błąd',
          description: 'Nie udało się załadować kosztorysu',
          status: 'error',
          duration: 5000,
        });
        navigate(`/projects/${projectId}/cost-estimates`);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [projectId, estimateId, user?.activeTenantId, toast, navigate]);

  // Handle save
  const handleSave = async () => {
    if (!estimate || !dataModel || !user?.activeTenantId || !projectId) return;

    setSaving(true);
    try {
      // Recalculate all values before saving
      const recalculated = calculations.recalculateAll(dataModel);

      // Znajdź pola ValueNet (4) i ValueGross (5) z summable=true
      const valueNetField = template?.templateStructure.workScopeFieldsDefinition.calculatedFields.find(
        f => f.type === 4 && f.summable
      );
      const valueGrossField = template?.templateStructure.workScopeFieldsDefinition.calculatedFields.find(
        f => f.type === 5 && f.summable
      );

      // Pobierz totalNet i totalGross z przeliczonych totals używając nazw pól
      const totalNet = valueNetField && recalculated.totals?.[valueNetField.name] 
        ? recalculated.totals[valueNetField.name] 
        : 0;
      const totalGross = valueGrossField && recalculated.totals?.[valueGrossField.name]
        ? recalculated.totals[valueGrossField.name]
        : 0;

      // Konwertuj stringi enumów na liczby dla backendu
      const dataForBackend = convertDataModelForBackend(recalculated);

      // Save via API
      await costEstimateApi.updateCostEstimate(user.activeTenantId, projectId, estimateId!, {
        name: estimate.name,
        description: estimate.description,
        status: estimate.status,
        data: dataForBackend,
        totalNet,
        totalGross,
      });

      setDataModel(recalculated);
      setHasChanges(false);

      toast({
        title: 'Zapisano pomyślnie',
        description: 'Kosztorys został zaktualizowany',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (error: any) {
      toast({
        title: 'Błąd',
        description: 'Nie udało się zapisać kosztorysu',
        status: 'error',
        duration: 5000,
      });
    } finally {
      setSaving(false);
    }
  };

  // Add new group
  const handleAddGroup = () => {
    if (!dataModel || !template?.templateStructure.canAddGroups) return;

    const newGroup: CostEstimateGroup = {
      id: `group-${Date.now()}`,
      level: 0,
      order: dataModel.groups.length,
      headerValues: {},
      workScopes: [],
    };

    setDataModel({
      ...dataModel,
      groups: [...dataModel.groups, newGroup],
    });
    setHasChanges(true);
  };

  // Add work scope to group
  const handleAddWorkScope = (groupId: string) => {
    if (!dataModel) return;

    const updateGroup = (group: CostEstimateGroup): CostEstimateGroup => {
      if (group.id === groupId) {
        const newWorkScope: CostEstimateWorkScope = {
          id: `ws-${Date.now()}`,
          order: group.workScopes.length,
          calculatedFieldValues: {},
          genericFieldValues: {},
        };

        return {
          ...group,
          workScopes: [...group.workScopes, newWorkScope],
        };
      }

      if (group.subGroups) {
        return {
          ...group,
          subGroups: group.subGroups.map(updateGroup),
        };
      }

      return group;
    };

    setDataModel({
      ...dataModel,
      groups: dataModel.groups.map(updateGroup),
    });
    setHasChanges(true);
  };

  // Add subgroup
  const handleAddSubGroup = (parentGroupId: string) => {
    if (!dataModel) return;

    const updateGroup = (group: CostEstimateGroup): CostEstimateGroup => {
      if (group.id === parentGroupId) {
        const newSubGroup: CostEstimateGroup = {
          id: `group-${Date.now()}`,
          parentId: group.id,
          level: group.level + 1,
          order: (group.subGroups?.length || 0),
          headerValues: {},
          workScopes: [],
        };

        return {
          ...group,
          subGroups: [...(group.subGroups || []), newSubGroup],
        };
      }

      if (group.subGroups) {
        return {
          ...group,
          subGroups: group.subGroups.map(updateGroup),
        };
      }

      return group;
    };

    setDataModel({
      ...dataModel,
      groups: dataModel.groups.map(updateGroup),
    });
    setHasChanges(true);
  };

  // Delete group
  const handleDeleteGroup = (groupId: string) => {
    if (!dataModel) return;

    const filterGroups = (groups: CostEstimateGroup[]): CostEstimateGroup[] => {
      return groups
        .filter(g => g.id !== groupId)
        .map(g => ({
          ...g,
          subGroups: g.subGroups ? filterGroups(g.subGroups) : undefined,
        }));
    };

    setDataModel({
      ...dataModel,
      groups: filterGroups(dataModel.groups),
    });
    setHasChanges(true);
  };

  // Delete work scope
  const handleDeleteWorkScope = (groupId: string, workScopeId: string) => {
    if (!dataModel) return;

    const updateGroup = (group: CostEstimateGroup): CostEstimateGroup => {
      if (group.id === groupId) {
        return {
          ...group,
          workScopes: group.workScopes.filter(ws => ws.id !== workScopeId),
        };
      }

      if (group.subGroups) {
        return {
          ...group,
          subGroups: group.subGroups.map(updateGroup),
        };
      }

      return group;
    };

    setDataModel({
      ...dataModel,
      groups: dataModel.groups.map(updateGroup),
    });
    setHasChanges(true);
  };

  // Update work scope
  const handleWorkScopeChange = (groupId: string, workScope: CostEstimateWorkScope) => {
    if (!dataModel) return;

    const updateGroup = (group: CostEstimateGroup): CostEstimateGroup => {
      if (group.id === groupId) {
        return {
          ...group,
          workScopes: group.workScopes.map(ws => (ws.id === workScope.id ? workScope : ws)),
        };
      }

      if (group.subGroups) {
        return {
          ...group,
          subGroups: group.subGroups.map(updateGroup),
        };
      }

      return group;
    };

    setDataModel({
      ...dataModel,
      groups: dataModel.groups.map(updateGroup),
    });
    setHasChanges(true);
  };

  // Add collection item
  const handleAddCollectionItem = (groupId: string, workScopeId: string, collectionFieldName: string) => {
    if (!dataModel || !template) return;

    const genericFields = template.templateStructure.workScopeFieldsDefinition.genericFields || [];
    const collectionField = genericFields.find((f) => f.name === collectionFieldName);
    
    if (!collectionField || !collectionField.nestedFields) return;

    const updateGroup = (group: CostEstimateGroup): CostEstimateGroup => {
      if (group.id === groupId) {
        const updatedWorkScopes = group.workScopes.map((ws) => {
          if (ws.id === workScopeId) {
            const existingItems = ws.collectionFieldValues?.[collectionFieldName] || [];
            
            const newItem: any = {
              id: `item-${Date.now()}`,
              isSelected: false,
              calculatedFieldValues: {},
              genericFieldValues: {},
            };

            return {
              ...ws,
              collectionFieldValues: {
                ...ws.collectionFieldValues,
                [collectionFieldName]: [...existingItems, newItem],
              },
            };
          }
          return ws;
        });

        return {
          ...group,
          workScopes: updatedWorkScopes,
        };
      }

      if (group.subGroups) {
        return {
          ...group,
          subGroups: group.subGroups.map(updateGroup),
        };
      }

      return group;
    };

    setDataModel({
      ...dataModel,
      groups: dataModel.groups.map(updateGroup),
    });
    setHasChanges(true);
  };

  // Delete collection item
  const handleDeleteCollectionItem = (
    groupId: string,
    workScopeId: string,
    collectionFieldName: string,
    itemId: string
  ) => {
    if (!dataModel) return;

    const updateGroup = (group: CostEstimateGroup): CostEstimateGroup => {
      if (group.id === groupId) {
        const updatedWorkScopes = group.workScopes.map((ws) => {
          if (ws.id === workScopeId) {
            const existingItems = ws.collectionFieldValues?.[collectionFieldName] || [];
            const filteredItems = existingItems.filter((item) => item.id !== itemId);

            return {
              ...ws,
              collectionFieldValues: {
                ...ws.collectionFieldValues,
                [collectionFieldName]: filteredItems,
              },
            };
          }
          return ws;
        });

        return {
          ...group,
          workScopes: updatedWorkScopes,
        };
      }

      if (group.subGroups) {
        return {
          ...group,
          subGroups: group.subGroups.map(updateGroup),
        };
      }

      return group;
    };

    const updatedDataModel = {
      ...dataModel,
      groups: dataModel.groups.map(updateGroup),
    };

    // Przelicz po usunięciu
    const recalculated = calculations.recalculateAll(updatedDataModel);
    setDataModel(recalculated);
    setHasChanges(true);
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minH="400px">
        <Spinner size="xl" />
      </Box>
    );
  }

  if (!estimate || !template || !dataModel) {
    return (
      <Container maxW="container.xl" py={8}>
        <Alert status="error">
          <AlertIcon />
          <AlertTitle>Błąd</AlertTitle>
          <AlertDescription>Nie można załadować kosztorysu</AlertDescription>
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxW="container.xl" py={6}>
      {/* Breadcrumbs */}
      <Breadcrumb mb={4} fontSize="sm">
        <BreadcrumbItem>
          <BreadcrumbLink onClick={() => navigate('/projects')}>Projekty</BreadcrumbLink>
        </BreadcrumbItem>
        <BreadcrumbItem>
          <BreadcrumbLink onClick={() => navigate(`/projects/${projectId}`)}>
            {projectName}
          </BreadcrumbLink>
        </BreadcrumbItem>
        <BreadcrumbItem>
          <BreadcrumbLink onClick={() => navigate(`/projects/${projectId}/cost-estimates`)}>
            Kosztorysy
          </BreadcrumbLink>
        </BreadcrumbItem>
        <BreadcrumbItem isCurrentPage>
          <BreadcrumbLink>{estimate.name}</BreadcrumbLink>
        </BreadcrumbItem>
      </Breadcrumb>

      {/* Header */}
      <HStack justify="space-between" mb={{ base: 4, md: 6 }} flexWrap="wrap" gap={{ base: 2, md: 4 }}>
        <HStack spacing={{ base: 2, md: 4 }}>
          <Box>
            <HStack spacing={{ base: 2, md: 3 }} mb={1} flexWrap="wrap">
              <Heading size={{ base: "sm", md: "lg" }}>{estimate.name}</Heading>
              <Badge colorScheme={costEstimateStatusColors[estimate.status]} fontSize={{ base: "xs", md: "md" }}>
                {costEstimateStatusLabels[estimate.status]}
              </Badge>
            </HStack>
            {estimate.description && (
              <Text color="gray.600" fontSize={{ base: "xs", md: "sm" }}>
                {estimate.description}
              </Text>
            )}
            <Text fontSize={{ base: "10px", md: "xs" }} color="gray.500" mt={1}>
              Szablon: {estimate.templateName} • Utworzono: {formatDate(estimate.createdAt)}
            </Text>
          </Box>
        </HStack>

        <HStack spacing={{ base: 2, md: 3 }}>
          {hasChanges && (
            <Badge colorScheme="orange" fontSize="sm" p={2}>
              Niezapisane zmiany
            </Badge>
          )}
          
          {/* View Mode Toggle */}
          <HStack spacing={1} bg="gray.100" p={1} borderRadius="md">
            <Button
              size="sm"
              leftIcon={<Edit size={16} />}
              colorScheme={viewMode === 'edit' ? 'blue' : 'gray'}
              variant={viewMode === 'edit' ? 'solid' : 'ghost'}
              onClick={() => setViewMode('edit')}
              isDisabled={!permissions.mine.canEdit && !permissions.all.canEdit && !permissions.shared.canEdit}
            >
              Edycja
            </Button>
            <Button
              size="sm"
              leftIcon={<Eye size={16} />}
              colorScheme={viewMode === 'preview' ? 'blue' : 'gray'}
              variant={viewMode === 'preview' ? 'solid' : 'ghost'}
              onClick={() => setViewMode('preview')}
            >
              Podgląd
            </Button>
          </HStack>
          
          <Button
            leftIcon={<Save size={18} />}
            colorScheme="blue"
            onClick={handleSave}
            isLoading={saving}
            isDisabled={!hasChanges || viewMode === 'preview'}
          >
            Zapisz
          </Button>
        </HStack>
      </HStack>

      {/* Groups */}
      <Box>
        <HStack justify="space-between" mb={4}>
          <HStack spacing={2}>
            <FileText size={20} />
            <Heading size="md">Grupy i pozycje</Heading>
            {dataModel.groups.length > 0 && (
              <Badge colorScheme="blue">{dataModel.groups.length} grup</Badge>
            )}
          </HStack>
        </HStack>

        {dataModel.groups.length === 0 ? (
          // No groups
          <VStack spacing={4}>
            <Alert status="info">
              <AlertIcon />
              <AlertTitle>Brak grup</AlertTitle>
              <AlertDescription>
                Rozpocznij tworzenie kosztorysu
              </AlertDescription>
            </Alert>
            {viewMode === 'edit' && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
              <Button
                leftIcon={<Plus size={16} />}
                colorScheme="green"
                onClick={handleAddGroup}
                isDisabled={!template.templateStructure.canAddGroups}
              >
                Dodaj grupę
              </Button>
            )}
          </VStack>
        ) : (
          // Excel-style view - działa w obu trybach
          <CostEstimateExcelView
            dataModel={dataModel} 
            template={template}
            editable={viewMode === 'edit' && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit)}
            onDataChange={(updatedDataModel) => {
              const recalculated = calculations.recalculateAll(updatedDataModel);
              setDataModel(recalculated);
              setHasChanges(true);
            }}
            onAddGroup={(() => {
              const canAdd = template.templateStructure.canAddGroups;
              const hasPermission = permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit;
              const result = viewMode === 'edit' && hasPermission ? (canAdd ? handleAddGroup : undefined) : undefined;
              console.log('[CostEstimateEditor] onAddGroup:', { viewMode, canAdd, hasPermission, result: result !== undefined });
              return result;
            })()}
            onAddSubGroup={viewMode === 'edit' && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) ? (template.templateStructure.canBranchGroups ? handleAddSubGroup : undefined) : undefined}
            onDeleteGroup={viewMode === 'edit' && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) ? handleDeleteGroup : undefined}
            onAddWorkScope={viewMode === 'edit' && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) ? handleAddWorkScope : undefined}
            onDeleteWorkScope={viewMode === 'edit' && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) ? handleDeleteWorkScope : undefined}
            onAddCollectionItem={viewMode === 'edit' && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) ? handleAddCollectionItem : undefined}
            onDeleteCollectionItem={viewMode === 'edit' && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) ? handleDeleteCollectionItem : undefined}
          />
        )}
      </Box>
    </Container>
  );
};
