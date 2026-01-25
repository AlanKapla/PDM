import React, { useContext, useState, useEffect, useMemo, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  Box, 
  Container, 
  Button, 
  Alert, 
  AlertIcon, 
  Spinner, 
  Stack, 
  Text, 
  Badge, 
  Divider,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useDisclosure,
} from '@chakra-ui/react';
import { ArrowLeft, Save, RefreshCw, Trash2 } from 'lucide-react';
import { AuthContext } from '../context/AuthContext';
import { CostEstimateTableView } from '../components/CostEstimate/CostEstimateTableView';
import { costEstimateApiNew } from '../api/costEstimateApiNew';
import type { CostEstimateDetailsWeb, CostEstimateGroupWeb, CostEstimateItemWeb } from '../types/costEstimate.types.new';
import { CostEstimateStatus, convertGroupWebToDto, isTemporaryId } from '../types/costEstimate.types.new';
import type { SummaryFieldWeb } from '../types/costEstimate.types';

/**
 * CostEstimateEditPage - Edycja kosztorysu w formie tabeli Excel
 */
export const CostEstimateEditPage: React.FC = () => {
  const { projectId, estimateId } = useParams<{
    projectId: string;
    estimateId: string;
  }>();
  
  const { user } = useContext(AuthContext);
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [details, setDetails] = useState<CostEstimateDetailsWeb | null>(null);
  const [hasChanges, setHasChanges] = useState(false);
  
  // Modal usuwania grupy
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();
  const [groupToDelete, setGroupToDelete] = useState<string | null>(null);
  const cancelRef = useRef<HTMLButtonElement>(null);

  // Załaduj szczegóły kosztorysu
  useEffect(() => {
    if (user?.activeTenantId && projectId && estimateId) {
      loadCostEstimate();
    }
  }, [user?.activeTenantId, projectId, estimateId]);

  const loadCostEstimate = async () => {
    if (!user?.activeTenantId || !projectId || !estimateId) return;

    try {
      setLoading(true);
      setError(null);

      const data = await costEstimateApiNew.getCostEstimateDetails(
        user.activeTenantId,
        projectId,
        estimateId
      );
      
      setDetails(data);
      setHasChanges(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas ładowania kosztorysu');
      console.error('Error loading cost estimate:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!user?.activeTenantId || !projectId || !estimateId || !details) return;

    try {
      setSaving(true);
      setError(null);

      const updateDto = {
        name: details.name,
        description: details.description,
        status: details.status,
        rootGroups: details.rootGroups.map((group) => convertGroupWebToDto(group)),
      };
      
      await costEstimateApiNew.updateCostEstimate(
        user.activeTenantId,
        projectId,
        estimateId,
        updateDto
      );

      // Przeładuj dane po zapisie
      await loadCostEstimate();
      setHasChanges(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas zapisywania kosztorysu');
      console.error('Error saving cost estimate:', err);
    } finally {
      setSaving(false);
    }
  };

  // Automatyczne przeliczanie sum (live preview) na podstawie konfiguracji szablonu
  const recalculateCostEstimate = (data: CostEstimateDetailsWeb): CostEstimateDetailsWeb => {
    const summaryConfig = data.templateStructure.summaryConfiguration;
    const showGroupSummary = summaryConfig?.showGroupSummary ?? true;
    const showTotalSummary = summaryConfig?.showTotalSummary ?? true;
    const groupSummaryFields = summaryConfig?.groupSummaryFields || [];
    const totalSummaryFields = summaryConfig?.totalSummaryFields || [];

    // Helper: znajdź definicję pola po ID
    const findFieldDefinition = (fieldId: string) => {
      // Sprawdź w systemFields
      const systemField = data.templateStructure.systemFields.find((f) => f.id === fieldId);
      if (systemField) return { ...systemField, source: 'system' as const };

      // Sprawdź w calculatedFields
      const calcField = data.templateStructure.calculatedFields.find((f) => f.id === fieldId);
      if (calcField) return { ...calcField, source: 'calculated' as const };

      // Sprawdź w genericFields
      const genericField = data.templateStructure.genericFields.find((f) => f.id === fieldId);
      if (genericField) return { ...genericField, source: 'generic' as const };

      return null;
    };

    // Helper: pobierz wartość pola z pozycji
    const getItemFieldValue = (item: CostEstimateItemWeb, fieldId: string): number => {
      const fv = item.fieldValues.find((v) => v.fieldDefinitionId === fieldId);
      return parseFloat(fv?.value || '0') || 0;
    };

    // Helper: ustaw wartość pola w pozycji
    const setItemFieldValue = (item: CostEstimateItemWeb, fieldId: string, value: number): CostEstimateItemWeb => {
      const existingIndex = item.fieldValues.findIndex((v) => v.fieldDefinitionId === fieldId);
      const newFieldValues = [...item.fieldValues];
      
      if (existingIndex >= 0) {
        newFieldValues[existingIndex] = { ...newFieldValues[existingIndex], value: value.toFixed(2) };
      } else {
        newFieldValues.push({ 
          id: `calc_${Date.now()}_${fieldId}`,
          fieldDefinitionId: fieldId, 
          fieldType: 0,
          fieldScope: 2,
          value: value.toFixed(2) 
        });
      }
      
      return { ...item, fieldValues: newFieldValues };
    };

    // Helper: oblicz wartości kalkulowane dla pozycji
    const calculateItemValues = (item: CostEstimateItemWeb): CostEstimateItemWeb => {
      let updatedItem = { ...item };
      
      // Znajdź pole systemowe "Selected" (Zaznaczenie) - fieldType 104 lub fieldName 'selected'
      const selectedFieldDef = data.templateStructure.systemFields.find(
        (f) => f.fieldName === 'selected' || f.fieldType === 104
      );
      
      // Znajdź pola systemowe
      const quantityFieldDef = data.templateStructure.systemFields.find((f) => f.fieldName === 'quantity');
      
      // Znajdź pola kalkulowane
      const unitPriceNetDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'unitPriceNet');
      const vatRateDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'vatRate');
      const unitPriceGrossDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'unitPriceGross');
      const valueNetDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'valueNet');
      const valueGrossDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'valueGross');
      const unitVatDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'unitVat');
      const totalVatDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'totalVat');
      
      // Lista wszystkich pól kalkulowanych do kopiowania
      const calculatedFieldDefs = [
        unitPriceNetDef, vatRateDef, unitPriceGrossDef, 
        valueNetDef, valueGrossDef, unitVatDef, totalVatDef
      ].filter(Boolean);

      // Sprawdź czy pozycja ma opcje i czy któraś opcja jest zaznaczona
      if (selectedFieldDef && updatedItem.options && updatedItem.options.length > 0) {
        // Znajdź zaznaczoną opcję
        const selectedOption = updatedItem.options.find((option) => {
          const selectedValue = option.fieldValues.find(
            (fv) => fv.fieldDefinitionId === selectedFieldDef.id
          );
          return selectedValue?.value === 'true' || selectedValue?.value === '1';
        });

        // Jeśli znaleziono zaznaczoną opcję, skopiuj wartości pól kalkulowanych do pozycji
        if (selectedOption) {
          for (const fieldDef of calculatedFieldDefs) {
            if (!fieldDef) continue;
            
            // Znajdź wartość pola w opcji
            const optionFieldValue = selectedOption.fieldValues.find(
              (fv) => fv.fieldDefinitionId === fieldDef.id
            );
            
            if (optionFieldValue?.value !== undefined) {
              const numValue = parseFloat(optionFieldValue.value) || 0;
              updatedItem = setItemFieldValue(updatedItem, fieldDef.id, numValue);
            }
          }
          
          // Po skopiowaniu wartości z opcji, przelicz pochodne dla pozycji
          const quantity = quantityFieldDef ? getItemFieldValue(updatedItem, quantityFieldDef.id) : 0;
          const unitPriceNet = unitPriceNetDef ? getItemFieldValue(updatedItem, unitPriceNetDef.id) : 0;
          const vatRate = vatRateDef ? getItemFieldValue(updatedItem, vatRateDef.id) : 0;

          const unitPriceGross = unitPriceNet * (1 + vatRate / 100);
          const valueNet = unitPriceNet * quantity;
          const valueGross = unitPriceGross * quantity;
          const unitVat = unitPriceNet * (vatRate / 100);
          const totalVat = valueNet * (vatRate / 100);

          if (unitPriceGrossDef) updatedItem = setItemFieldValue(updatedItem, unitPriceGrossDef.id, unitPriceGross);
          if (valueNetDef) updatedItem = setItemFieldValue(updatedItem, valueNetDef.id, valueNet);
          if (valueGrossDef) updatedItem = setItemFieldValue(updatedItem, valueGrossDef.id, valueGross);
          if (unitVatDef) updatedItem = setItemFieldValue(updatedItem, unitVatDef.id, unitVat);
          if (totalVatDef) updatedItem = setItemFieldValue(updatedItem, totalVatDef.id, totalVat);

          return updatedItem;
        }
      }

      // Standardowe obliczenia (gdy brak zaznaczonej opcji)
      const quantity = quantityFieldDef ? getItemFieldValue(updatedItem, quantityFieldDef.id) : 0;
      const unitPriceNet = unitPriceNetDef ? getItemFieldValue(updatedItem, unitPriceNetDef.id) : 0;
      const vatRate = vatRateDef ? getItemFieldValue(updatedItem, vatRateDef.id) : 0;

      // Oblicz wartości pochodne
      const unitPriceGross = unitPriceNet * (1 + vatRate / 100);
      const valueNet = unitPriceNet * quantity;
      const valueGross = unitPriceGross * quantity;
      const unitVat = unitPriceNet * (vatRate / 100);
      const totalVat = valueNet * (vatRate / 100);

      // Ustaw obliczone wartości
      if (unitPriceGrossDef) updatedItem = setItemFieldValue(updatedItem, unitPriceGrossDef.id, unitPriceGross);
      if (valueNetDef) updatedItem = setItemFieldValue(updatedItem, valueNetDef.id, valueNet);
      if (valueGrossDef) updatedItem = setItemFieldValue(updatedItem, valueGrossDef.id, valueGross);
      if (unitVatDef) updatedItem = setItemFieldValue(updatedItem, unitVatDef.id, unitVat);
      if (totalVatDef) updatedItem = setItemFieldValue(updatedItem, totalVatDef.id, totalVat);

      return updatedItem;
    };

    // Helper: sumuj wartości pól z listy pozycji według konfiguracji
    const sumFieldsFromItems = (items: CostEstimateItemWeb[], summaryFields: SummaryFieldWeb[]): Record<string, number> => {
      const sums: Record<string, number> = {};
      
      for (const summaryField of summaryFields) {
        const fieldId = summaryField.fieldId;
        const fieldDef = findFieldDefinition(fieldId);
        
        if (fieldDef) {
          sums[fieldId] = items.reduce((sum, item) => {
            return sum + getItemFieldValue(item, fieldId);
          }, 0);
        }
      }
      
      return sums;
    };

    // Rekursywnie przelicz grupy (bottom-up)
    const recalculateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
      // Najpierw przelicz podgrupy
      const updatedChildGroups = group.childGroups.map(recalculateGroup);

      // Przelicz wartości kalkulowane dla każdej pozycji
      const updatedItems = (group.items || []).map(calculateItemValues);

      // Jeśli showGroupSummary jest włączone, oblicz sumy na poziomie grupy
      let groupTotalNet = 0;
      let groupTotalGross = 0;
      let groupTotalVat = 0;
      let groupSummaryValues: Record<string, number> = {};

      if (showGroupSummary && groupSummaryFields.length > 0) {
        // Sumuj wartości pozycji według konfiguracji pól
        groupSummaryValues = sumFieldsFromItems(updatedItems, groupSummaryFields);
        
        // Sumuj wartości z podgrup
        for (const childGroup of updatedChildGroups) {
          for (const fieldId of Object.keys(groupSummaryValues)) {
            const childValue = (childGroup as any).summaryValues?.[fieldId] || 0;
            groupSummaryValues[fieldId] = (groupSummaryValues[fieldId] || 0) + childValue;
          }
        }
      }

      // Dla kompatybilności - oblicz standardowe totalNet/totalGross/totalVat
      const valueNetDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'valueNet');
      const valueGrossDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'valueGross');
      const totalVatDef = data.templateStructure.calculatedFields.find((f) => f.fieldName === 'totalVat');

      if (valueNetDef) {
        groupTotalNet = updatedItems.reduce((sum, item) => sum + getItemFieldValue(item, valueNetDef.id), 0);
        groupTotalNet += updatedChildGroups.reduce((sum, child) => sum + (child.totalNet || 0), 0);
      }

      if (valueGrossDef) {
        groupTotalGross = updatedItems.reduce((sum, item) => sum + getItemFieldValue(item, valueGrossDef.id), 0);
        groupTotalGross += updatedChildGroups.reduce((sum, child) => sum + (child.totalGross || 0), 0);
      }

      if (totalVatDef) {
        groupTotalVat = updatedItems.reduce((sum, item) => sum + getItemFieldValue(item, totalVatDef.id), 0);
        groupTotalVat += updatedChildGroups.reduce((sum, child) => sum + (child.totalVat || 0), 0);
      }

      return {
        ...group,
        items: updatedItems,
        childGroups: updatedChildGroups,
        totalNet: showGroupSummary ? groupTotalNet : undefined,
        totalGross: showGroupSummary ? groupTotalGross : undefined,
        totalVat: showGroupSummary ? groupTotalVat : undefined,
        lastCalculatedAt: new Date().toISOString(),
        // Dodatkowe sumy z konfiguracji (dla rozszerzenia w przyszłości)
        summaryValues: showGroupSummary ? groupSummaryValues : undefined,
      } as CostEstimateGroupWeb & { summaryValues?: Record<string, number> };
    };

    // Przelicz wszystkie root groups
    const recalculatedRootGroups = data.rootGroups.map(recalculateGroup);

    // Oblicz sumy całkowite według konfiguracji
    let totalNet: number | undefined;
    let totalGross: number | undefined;
    let totalVat: number | undefined;
    let totalSummaryValues: Record<string, number> = {};

    if (showTotalSummary) {
      // Sumuj wartości z root groups
      totalNet = recalculatedRootGroups.reduce((sum, group) => sum + (group.totalNet || 0), 0);
      totalGross = recalculatedRootGroups.reduce((sum, group) => sum + (group.totalGross || 0), 0);
      totalVat = recalculatedRootGroups.reduce((sum, group) => sum + (group.totalVat || 0), 0);

      // Sumuj według konfiguracji totalSummaryFields
      if (totalSummaryFields.length > 0) {
        for (const summaryField of totalSummaryFields) {
          const fieldId = summaryField.fieldId;
          totalSummaryValues[fieldId] = recalculatedRootGroups.reduce((sum, group) => {
            return sum + ((group as any).summaryValues?.[fieldId] || 0);
          }, 0);
        }
      }
    }

    return {
      ...data,
      rootGroups: recalculatedRootGroups,
      totalNet: showTotalSummary ? totalNet : undefined,
      totalGross: showTotalSummary ? totalGross : undefined,
      totalVat: showTotalSummary ? totalVat : undefined,
      lastCalculatedAt: new Date().toISOString(),
      // Dodatkowe sumy z konfiguracji
      summaryValues: showTotalSummary ? totalSummaryValues : undefined,
    } as CostEstimateDetailsWeb & { summaryValues?: Record<string, number> };
  };

  const handleDataChange = (updatedDetails: CostEstimateDetailsWeb) => {
    // Przelicz sumy (live preview)
    const recalculated = recalculateCostEstimate(updatedDetails);
    setDetails(recalculated);
    setHasChanges(true);
  };

  const handleAddGroup = () => {
    if (!details) return;

    const newGroup: CostEstimateGroupWeb = {
      id: `temp_${Date.now()}`,
      parentGroupId: undefined,
      level: 0,
      order: details.rootGroups.length,
      fieldValues: [],
      totalNet: 0,
      totalGross: 0,
      totalVat: 0,
      lastCalculatedAt: undefined,
      childGroups: [],
      items: [],
      createdAt: new Date().toISOString(),
      updatedAt: undefined,
    };

    const updatedDetails = {
      ...details,
      rootGroups: [...details.rootGroups, newGroup],
    };

    setDetails(updatedDetails);
    setHasChanges(true);
  };

  const handleDeleteGroup = (groupId: string) => {
    if (!details) return;
    setGroupToDelete(groupId);
    onDeleteOpen();
  };

  const confirmDeleteGroup = () => {
    if (!details || !groupToDelete) return;

    const deleteGroupRecursive = (groups: any[]): any[] => {
      return groups
        .filter((g) => g.id !== groupToDelete)
        .map((g) => ({
          ...g,
          childGroups: deleteGroupRecursive(g.childGroups || []),
        }));
    };

    const updatedDetails = {
      ...details,
      rootGroups: deleteGroupRecursive(details.rootGroups),
    };

    setDetails(updatedDetails);
    setHasChanges(true);
    setGroupToDelete(null);
    onDeleteClose();
  };

  const handleAddSubGroup = (parentGroupId: string) => {
    if (!details) return;

    const findAndAddSubGroup = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map((group) => {
        if (group.id === parentGroupId) {
          const childGroups = group.childGroups || [];
          const newSubGroup: CostEstimateGroupWeb = {
            id: `temp_${Date.now()}`,
            parentGroupId,
            level: group.level + 1,
            order: childGroups.length,
            fieldValues: [],
            totalNet: 0,
            totalGross: 0,
            totalVat: 0,
            lastCalculatedAt: undefined,
            childGroups: [],
            items: [],
            createdAt: new Date().toISOString(),
            updatedAt: undefined,
          };

          return {
            ...group,
            childGroups: [...childGroups, newSubGroup],
          };
        }

        return {
          ...group,
          childGroups: findAndAddSubGroup(group.childGroups || []),
        };
      });
    };

    const updatedDetails = {
      ...details,
      rootGroups: findAndAddSubGroup(details.rootGroups),
    };

    setDetails(updatedDetails);
    setHasChanges(true);
  };

  const handleAddItem = (groupId: string) => {
    if (!details) return;

    const findAndAddItem = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map((group) => {
        if (group.id === groupId) {
          const items = group.items || [];
          const newItem: CostEstimateItemWeb = {
            id: `temp_${Date.now()}`,
            groupId: groupId,
            parentItemId: undefined,
            order: items.length,
            fieldValues: [],
            options: [],
            createdAt: new Date().toISOString(),
            updatedAt: undefined,
          };

          return {
            ...group,
            items: [...items, newItem],
          };
        }

        return {
          ...group,
          childGroups: findAndAddItem(group.childGroups || []),
        };
      });
    };

    const updatedDetails = {
      ...details,
      rootGroups: findAndAddItem(details.rootGroups),
    };

    setDetails(updatedDetails);
    setHasChanges(true);
  };

  const handleDeleteItem = (groupId: string, itemId: string) => {
    if (!details) return;

    const findAndDeleteItem = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map((group) => {
        if (group.id === groupId) {
          return {
            ...group,
            items: (group.items || []).filter((item) => item.id !== itemId),
          };
        }

        return {
          ...group,
          childGroups: findAndDeleteItem(group.childGroups || []),
        };
      });
    };

    const updatedDetails = {
      ...details,
      rootGroups: findAndDeleteItem(details.rootGroups),
    };

    setDetails(updatedDetails);
    setHasChanges(true);
  };

  if (!user?.activeTenantId || !projectId || !estimateId) {
    return (
      <Container>
        <Box p={3} mt={3} bg="white" borderRadius="md" shadow="sm">
          Brak wymaganych parametrów (tenantId, projectId, estimateId)
        </Box>
      </Container>
    );
  }

  if (loading) {
    return (
      <Container maxW="100%" py={3}>
        <Box display="flex" justifyContent="center" alignItems="center" minHeight={400}>
          <Spinner size="xl" />
        </Box>
      </Container>
    );
  }

  if (error && !details) {
    return (
      <Container maxW="100%" py={3}>
        <Alert status="error" mb={2}>
          <AlertIcon />
          {error}
          <Button onClick={loadCostEstimate} ml={2}>
            Spróbuj ponownie
          </Button>
        </Alert>
      </Container>
    );
  }

  if (!details) {
    return (
      <Container maxW="100%" py={3}>
        <Alert status="warning">
          <AlertIcon />
          Nie znaleziono kosztorysu
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxW="100%" py={3}>
      {/* Header z przyciskami */}
      <Box p={4} mb={4} bg="white" borderRadius="md" shadow="sm">
        <Stack direction="row" spacing={2} alignItems="center" mb={2}>
          <Button
            leftIcon={<ArrowLeft size={18} />}
            onClick={() => navigate(`/projects/${projectId}/cost-estimates`)}
            variant="ghost"
          >
            Powrót
          </Button>
          
          <Box flex={1}>
            <Text fontSize="xl" fontWeight="bold">
              {details.name}
            </Text>
            <Text fontSize="sm" color="gray.600">
              Szablon: {details.templateName} • Wersja: {details.templateVersionNumber}
            </Text>
          </Box>
          
          <Badge colorScheme={details.status === CostEstimateStatus.Draft ? 'gray' : 'blue'}>
            {details.status}
          </Badge>
        </Stack>

        <Divider my={2} />

        {/* Podsumowanie - tylko jeśli showTotalSummary jest włączone */}
        {details.templateStructure.summaryConfiguration?.showTotalSummary !== false && (
          <>
            <Stack direction="row" spacing={4} mb={2} flexWrap="wrap">
              {/* Wyświetl pola z totalSummaryFields lub domyślne */}
              {(details.templateStructure.summaryConfiguration?.totalSummaryFields?.length ?? 0) > 0 ? (
                // Wyświetl tylko pola z konfiguracji
                details.templateStructure.summaryConfiguration?.totalSummaryFields?.map((summaryField) => {
                  // Znajdź wartość sumy dla tego pola
                  const fieldId = summaryField.fieldId;
                  const summaryValues = (details as any).summaryValues || {};
                  let value = summaryValues[fieldId];
                  
                  // Fallback na standardowe pola jeśli nie ma w summaryValues
                  if (value === undefined) {
                    const fieldDef = [
                      ...details.templateStructure.calculatedFields,
                      ...details.templateStructure.systemFields,
                    ].find((f) => f.id === fieldId);
                    
                    if (fieldDef?.fieldName === 'valueNet') value = details.totalNet;
                    else if (fieldDef?.fieldName === 'valueGross') value = details.totalGross;
                    else if (fieldDef?.fieldName === 'totalVat') value = details.totalVat;
                  }
                  
                  return (
                    <Box key={fieldId}>
                      <Text fontSize="xs" color="gray.600">{summaryField.fieldLabel}</Text>
                      <Text fontSize="lg" fontWeight="semibold">
                        {typeof value === 'number' ? value.toFixed(2) : '0.00'} {details.selectedCurrencyCode}
                      </Text>
                    </Box>
                  );
                })
              ) : (
                // Domyślne podsumowanie (kompatybilność wsteczna)
                <>
                  <Box>
                    <Text fontSize="xs" color="gray.600">Wartość netto</Text>
                    <Text fontSize="lg" fontWeight="semibold">
                      {details.totalNet?.toFixed(2) || '0.00'} {details.selectedCurrencyCode}
                    </Text>
                  </Box>
                  <Box>
                    <Text fontSize="xs" color="gray.600">VAT</Text>
                    <Text fontSize="lg" fontWeight="semibold">
                      {details.totalVat?.toFixed(2) || '0.00'} {details.selectedCurrencyCode}
                    </Text>
                  </Box>
                  <Box>
                    <Text fontSize="xs" color="gray.600">Wartość brutto</Text>
                    <Text fontSize="lg" fontWeight="semibold" color="blue.600">
                      {details.totalGross?.toFixed(2) || '0.00'} {details.selectedCurrencyCode}
                    </Text>
                  </Box>
                </>
              )}
            </Stack>

            <Divider my={2} />
          </>
        )}

        {/* Przyciski akcji */}
        <Stack direction="row" spacing={2}>
          <Button
            colorScheme="blue"
            leftIcon={saving ? <Spinner size="sm" /> : <Save size={16} />}
            onClick={handleSave}
            isDisabled={!hasChanges || saving}
          >
            {saving ? 'Zapisywanie...' : 'Zapisz zmiany'}
          </Button>
          
          <Button
            variant="outline"
            leftIcon={<RefreshCw size={16} />}
            onClick={loadCostEstimate}
            isDisabled={loading}
          >
            Odśwież
          </Button>
        </Stack>

        {hasChanges && (
          <Alert status="info" mt={2}>
            <AlertIcon />
            Masz niezapisane zmiany
          </Alert>
        )}

        {error && (
          <Alert status="error" mt={2}>
            <AlertIcon />
            {error}
          </Alert>
        )}
      </Box>

      {/* Tabela kosztorysu */}
      <CostEstimateTableView
        details={details}
        editable={true}
        onDataChange={handleDataChange}
        onAddGroup={handleAddGroup}
        onDeleteGroup={handleDeleteGroup}
        onAddSubGroup={handleAddSubGroup}
        onAddItem={handleAddItem}
        onDeleteItem={handleDeleteItem}
      />

      {/* Modal potwierdzenia usunięcia grupy */}
      <AlertDialog
        isOpen={isDeleteOpen}
        leastDestructiveRef={cancelRef}
        onClose={onDeleteClose}
        isCentered
      >
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader fontSize="lg" fontWeight="bold" display="flex" alignItems="center" gap={2}>
              <Trash2 size={20} color="var(--chakra-colors-red-500)" />
              Usuń grupę
            </AlertDialogHeader>

            <AlertDialogBody>
              <Text mb={2}>
                Czy na pewno chcesz usunąć tę grupę?
              </Text>
              <Text fontSize="sm" color="gray.600">
                Wszystkie podgrupy i pozycje w tej grupie zostaną trwale usunięte. 
                Tej operacji nie można cofnąć.
              </Text>
            </AlertDialogBody>

            <AlertDialogFooter gap={3}>
              <Button ref={cancelRef} onClick={onDeleteClose}>
                Anuluj
              </Button>
              <Button colorScheme="red" onClick={confirmDeleteGroup} leftIcon={<Trash2 size={16} />}>
                Usuń grupę
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </Container>
  );
};

/**
 * Example: CostEstimateViewPage (readonly)
 */
export const CostEstimateViewPage: React.FC = () => {
  const { tenantId, projectId, costEstimateId } = useParams<{
    tenantId: string;
    projectId: string;
    costEstimateId: string;
  }>();
  
  const navigate = useNavigate();

  if (!tenantId || !projectId || !costEstimateId) {
    return null;
  }

  return (
    <Container maxW="container.xl" py={3}>
      <Box mb={2}>
        <Button
          leftIcon={<ArrowLeft size={16} />}
          onClick={() => navigate(`/tenants/${tenantId}/projects/${projectId}/cost-estimates`)}
        >
          Powrót do listy kosztorysów
        </Button>
      </Box>

      <Alert status="info">
        <AlertIcon />
        Edytor kosztorysu został usunięty. Użyj komponentu CostEstimateExcelView dla wyświetlania w formie tabeli Excel.
      </Alert>
    </Container>
  );
};
