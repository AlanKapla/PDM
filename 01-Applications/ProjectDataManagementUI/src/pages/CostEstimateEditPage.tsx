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
  Divider,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useDisclosure,
  ButtonGroup,
  Icon,
} from '@chakra-ui/react';
import { ArrowLeft, Save, RefreshCw, Trash2, Eye, Pencil } from 'lucide-react';
import { AuthContext } from '../context/AuthContext';
import { CostEstimateTableView } from '../components/CostEstimate/CostEstimateTableView';
import { costEstimateApiNew } from '../api/costEstimateApiNew';
import type { CostEstimateDetailsWeb, CostEstimateGroupWeb, CostEstimateItemWeb, CostEstimateFieldValueWeb } from '../types/costEstimate.types.new';
import { CostEstimateStatus, convertGroupWebToDto, isTemporaryId, getFieldValueAsNumber, getFieldValueAsBoolean } from '../types/costEstimate.types.new';
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
  
  // Przełącznik trybu: edycja / podgląd
  const [isEditMode, setIsEditMode] = useState(true);

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
      
      // Przelicz wartości kalkulowane i podsumowania przy załadowaniu
      // (nowo dodane pola w szablonie mogą nie mieć jeszcze wartości w pozycjach)
      const recalculated = recalculateCostEstimate(data);
      setDetails(recalculated);
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
    // Helper: pobierz wartość pola z pozycji (number lub undefined gdy brak)
    const getItemFieldValue = (item: CostEstimateItemWeb, fieldId: string): number => {
      const fv = item.fieldValues.find((v) => v.fieldDefinitionId === fieldId);
      return getFieldValueAsNumber(fv);
    };

    // Helper: pobierz wartość źródłową — undefined gdy pole NIE ma wpisu w fieldValues
    const getSourceFieldValue = (item: CostEstimateItemWeb, fieldId: string): number | undefined => {
      const fv = item.fieldValues.find((v) => v.fieldDefinitionId === fieldId);
      if (!fv) return undefined;
      if (fv.decimalValue !== null && fv.decimalValue !== undefined) return fv.decimalValue;
      if (fv.stringValue) { const p = parseFloat(fv.stringValue); return isNaN(p) ? undefined : p; }
      return undefined;
    };

    // Helper: ustaw wartość pola w pozycji (typowane jako decimalValue)
    const setItemFieldValue = (item: CostEstimateItemWeb, fieldId: string, value: number): CostEstimateItemWeb => {
      const existingIndex = item.fieldValues.findIndex((v) => v.fieldDefinitionId === fieldId);
      const newFieldValues = [...item.fieldValues];
      
      if (existingIndex >= 0) {
        newFieldValues[existingIndex] = { 
          ...newFieldValues[existingIndex], 
          decimalValue: value,
          stringValue: undefined,
          boolValue: undefined,
          dateTimeValue: undefined
        };
      } else {
        const newFieldValue: CostEstimateFieldValueWeb = {
          id: `calc_${Date.now()}_${fieldId}`,
          fieldDefinitionId: fieldId, 
          fieldType: 0,
          fieldScope: 2,
          decimalValue: value,
          stringValue: undefined,
          boolValue: undefined,
          dateTimeValue: undefined
        };
        newFieldValues.push(newFieldValue);
      }
      
      return { ...item, fieldValues: newFieldValues };
    };

    // Helper: pobierz fieldType z definicji pola
    const getFieldType = (f: any) => f.fieldType ?? f.fieldTypeConfig?.fieldType;

    // Helper: oblicz wartości kalkulowane dla pozycji
    const calculateItemValues = (item: CostEstimateItemWeb): CostEstimateItemWeb => {
      let updatedItem = { ...item };
      
      // Znajdź pole systemowe "Selected" (Zaznaczenie) - fieldType 104 lub fieldName 'selected'
      const selectedFieldDef = data.templateStructure.systemFields.find(
        (f) => f.fieldName === 'selected' || getFieldType(f) === 104
      );
      
      // Znajdź pola systemowe (quantity = fieldType 101)
      const quantityFieldDef = data.templateStructure.systemFields.find(
        (f) => f.fieldName === 'quantity' || getFieldType(f) === 101
      );
      
      // Znajdź pola kalkulowane - po fieldName LUB fieldType
      const unitPriceNetDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'unitPriceNet' || getFieldType(f) === 200
      );
      const vatRateDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'vatRate' || getFieldType(f) === 201
      );
      const unitPriceGrossDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'unitPriceGross' || getFieldType(f) === 202
      );
      const valueNetDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'valueNet' || getFieldType(f) === 203
      );
      const valueGrossDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'valueGross' || getFieldType(f) === 204
      );
      const unitVatDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'unitVat' || getFieldType(f) === 205
      );
      const totalVatDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'totalVat' || getFieldType(f) === 206
      );
      
      // Lista wszystkich pól kalkulowanych do kopiowania
      const calculatedFieldDefs = [
        unitPriceNetDef, vatRateDef, unitPriceGrossDef, 
        valueNetDef, valueGrossDef, unitVatDef, totalVatDef
      ].filter(Boolean);

      // === KOMPONENTY: gdy pozycja ma komponenty, przelicz je i zsumuj do pozycji ===
      if (updatedItem.components && updatedItem.components.length > 0) {
        // Przelicz każdy komponent jak zwykłą pozycję (zachowaj options komponentu)
        const recalculatedComponents = updatedItem.components.map((comp) => {
          const recalculated = calculateItemValues({ ...comp, components: undefined });
          return { ...recalculated, options: comp.options };
        });
        updatedItem = { ...updatedItem, components: recalculatedComponents };

        // Sumuj wartości z komponentów do pozycji nadrzędnej: valueNet (203), valueGross (204), totalVat (206)
        const summableFields = [
          { def: valueNetDef, fieldType: 203 },
          { def: valueGrossDef, fieldType: 204 },
          { def: totalVatDef, fieldType: 206 },
        ];

        for (const { def } of summableFields) {
          if (!def) continue;
          let sum = 0;
          for (const comp of recalculatedComponents) {
            sum += getItemFieldValue(comp, def.id);
          }
          updatedItem = setItemFieldValue(updatedItem, def.id, sum);
        }

        return updatedItem;
      }

      // Sprawdź czy pozycja ma opcje i czy któraś opcja jest zaznaczona
      if (selectedFieldDef && updatedItem.options && updatedItem.options.length > 0) {
        // Znajdź zaznaczoną opcję
        const selectedOption = updatedItem.options.find((option) => {
          const selectedValue = option.fieldValues.find(
            (fv) => fv.fieldDefinitionId === selectedFieldDef.id
          );
          return getFieldValueAsBoolean(selectedValue);
        });

        // Jeśli znaleziono zaznaczoną opcję, skopiuj wartości pól kalkulowanych do pozycji
        if (selectedOption) {
          for (const fieldDef of calculatedFieldDefs) {
            if (!fieldDef) continue;
            
            // Znajdź wartość pola w opcji
            const optionFieldValue = selectedOption.fieldValues.find(
              (fv) => fv.fieldDefinitionId === fieldDef.id
            );
            
            if (optionFieldValue) {
              const numValue = getFieldValueAsNumber(optionFieldValue);
              updatedItem = setItemFieldValue(updatedItem, fieldDef.id, numValue);
            }
          }
          
          // Po skopiowaniu wartości z opcji, przelicz pochodne używając ŹRÓDŁOWYCH wartości
          const quantity = quantityFieldDef ? getSourceFieldValue(updatedItem, quantityFieldDef.id) : undefined;
          const unitPriceNet = unitPriceNetDef ? getSourceFieldValue(updatedItem, unitPriceNetDef.id) : undefined;
          const vatRate = vatRateDef ? getSourceFieldValue(updatedItem, vatRateDef.id) : undefined;

          const hasQuantity = quantity !== undefined;
          const hasUnitPriceNet = unitPriceNet !== undefined;
          const hasVatRate = vatRate !== undefined;

          let unitPriceGross: number | undefined;
          if (unitPriceGrossDef) {
            if (hasUnitPriceNet && hasVatRate) {
              unitPriceGross = unitPriceNet! * (1 + vatRate! / 100);
              updatedItem = setItemFieldValue(updatedItem, unitPriceGrossDef.id, unitPriceGross);
            } else {
              unitPriceGross = getSourceFieldValue(updatedItem, unitPriceGrossDef.id);
            }
          }

          let unitVat: number | undefined;
          if (unitVatDef) {
            if (hasUnitPriceNet && hasVatRate) {
              unitVat = unitPriceNet! * (vatRate! / 100);
              updatedItem = setItemFieldValue(updatedItem, unitVatDef.id, unitVat);
            } else {
              unitVat = getSourceFieldValue(updatedItem, unitVatDef.id);
            }
          }

          let valueNet: number | undefined;
          if (valueNetDef) {
            if (hasUnitPriceNet && hasQuantity) {
              valueNet = unitPriceNet! * quantity!;
              updatedItem = setItemFieldValue(updatedItem, valueNetDef.id, valueNet);
            } else {
              valueNet = getSourceFieldValue(updatedItem, valueNetDef.id);
            }
          }

          const hasValueNet = valueNet !== undefined;
          const hasUnitVat = unitVat !== undefined;
          const hasUnitPriceGross = unitPriceGross !== undefined;

          let totalVat: number | undefined;
          if (totalVatDef) {
            if (hasValueNet && hasVatRate) {
              totalVat = valueNet! * (vatRate! / 100);
              updatedItem = setItemFieldValue(updatedItem, totalVatDef.id, totalVat);
            } else if (hasUnitVat && hasQuantity) {
              totalVat = unitVat! * quantity!;
              updatedItem = setItemFieldValue(updatedItem, totalVatDef.id, totalVat);
            } else {
              totalVat = getSourceFieldValue(updatedItem, totalVatDef.id);
            }
          }

          const hasTotalVat = totalVat !== undefined;

          if (valueGrossDef) {
            if (hasUnitPriceGross && hasQuantity) {
              const valueGross = unitPriceGross! * quantity!;
              updatedItem = setItemFieldValue(updatedItem, valueGrossDef.id, valueGross);
            } else if (hasValueNet && hasTotalVat) {
              const valueGross = valueNet! + totalVat!;
              updatedItem = setItemFieldValue(updatedItem, valueGrossDef.id, valueGross);
            } else if (hasValueNet) {
              // Brak VAT → brutto = netto
              updatedItem = setItemFieldValue(updatedItem, valueGrossDef.id, valueNet!);
            }
          }

          return updatedItem;
        }
      }

      // Standardowe obliczenia (gdy brak zaznaczonej opcji)
      // Pobierz wartości ŹRÓDŁOWE — undefined gdy brak wpisu w fieldValues
      const quantity = quantityFieldDef ? getSourceFieldValue(updatedItem, quantityFieldDef.id) : undefined;
      const unitPriceNet = unitPriceNetDef ? getSourceFieldValue(updatedItem, unitPriceNetDef.id) : undefined;
      const vatRate = vatRateDef ? getSourceFieldValue(updatedItem, vatRateDef.id) : undefined;

      // Walidacja: pole musi istnieć (undefined = nie wpisane)
      const hasQuantity = quantity !== undefined;
      const hasUnitPriceNet = unitPriceNet !== undefined;
      const hasVatRate = vatRate !== undefined;

      // unitPriceGross: wymaga unitPriceNet + vatRate
      let unitPriceGross: number | undefined;
      if (unitPriceGrossDef) {
        if (hasUnitPriceNet && hasVatRate) {
          unitPriceGross = unitPriceNet! * (1 + vatRate! / 100);
          updatedItem = setItemFieldValue(updatedItem, unitPriceGrossDef.id, unitPriceGross);
        } else {
          // Nie nadpisuj — zostaw wartość ręczną (jeśli jest)
          unitPriceGross = getSourceFieldValue(updatedItem, unitPriceGrossDef.id);
        }
      }

      // unitVat: wymaga unitPriceNet + vatRate
      let unitVat: number | undefined;
      if (unitVatDef) {
        if (hasUnitPriceNet && hasVatRate) {
          unitVat = unitPriceNet! * (vatRate! / 100);
          updatedItem = setItemFieldValue(updatedItem, unitVatDef.id, unitVat);
        } else {
          // Nie nadpisuj — zostaw wartość ręczną
          unitVat = getSourceFieldValue(updatedItem, unitVatDef.id);
        }
      }

      // valueNet: wymaga unitPriceNet + quantity
      let valueNet: number | undefined;
      if (valueNetDef) {
        if (hasUnitPriceNet && hasQuantity) {
          valueNet = unitPriceNet! * quantity!;
          updatedItem = setItemFieldValue(updatedItem, valueNetDef.id, valueNet);
        } else {
          valueNet = getSourceFieldValue(updatedItem, valueNetDef.id);
        }
      }

      // Sprawdź czy mamy wartości (obliczone lub ręczne)
      const hasValueNet = valueNet !== undefined;
      const hasUnitVat = unitVat !== undefined;
      const hasUnitPriceGross = unitPriceGross !== undefined;

      // totalVat: wymaga (valueNet + vatRate) LUB (unitVat + quantity)
      let totalVat: number | undefined;
      if (totalVatDef) {
        if (hasValueNet && hasVatRate) {
          totalVat = valueNet! * (vatRate! / 100);
          updatedItem = setItemFieldValue(updatedItem, totalVatDef.id, totalVat);
        } else if (hasUnitVat && hasQuantity) {
          totalVat = unitVat! * quantity!;
          updatedItem = setItemFieldValue(updatedItem, totalVatDef.id, totalVat);
        } else {
          totalVat = getSourceFieldValue(updatedItem, totalVatDef.id);
        }
      }

      // Sprawdź czy mamy totalVat (obliczone lub ręczne)
      const hasTotalVat = totalVat !== undefined;

      // valueGross: wymaga (unitPriceGross + quantity) LUB (valueNet + totalVat) LUB (valueNet gdy brak VAT)
      if (valueGrossDef) {
        if (hasUnitPriceGross && hasQuantity) {
          const valueGross = unitPriceGross! * quantity!;
          updatedItem = setItemFieldValue(updatedItem, valueGrossDef.id, valueGross);
        } else if (hasValueNet && hasTotalVat) {
          const valueGross = valueNet! + totalVat!;
          updatedItem = setItemFieldValue(updatedItem, valueGrossDef.id, valueGross);
        } else if (hasValueNet) {
          // Brak VAT → brutto = netto
          updatedItem = setItemFieldValue(updatedItem, valueGrossDef.id, valueNet!);
        }
        // Brak danych → zostaw wartość ręczną
      }

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

      if (showGroupSummary) {
        // Zbierz pola do sumowania: z groupSummaryFields config + pola z sumInGroup=true
        const summaryFieldIds = new Set<string>();
        
        // Z konfiguracji summaryFields
        for (const sf of groupSummaryFields) {
          summaryFieldIds.add(sf.fieldId);
        }
        
        // Z definicji pól kalkulowanych z flagą sumInGroup
        for (const cf of data.templateStructure.calculatedFields) {
          if (cf.sumInGroup === true) {
            summaryFieldIds.add(cf.id);
          }
        }
        
        // Sumuj wartości pozycji dla zebranych pól
        for (const fieldId of summaryFieldIds) {
          groupSummaryValues[fieldId] = updatedItems.reduce((sum, item) => {
            return sum + getItemFieldValue(item, fieldId);
          }, 0);
        }
        
        // Sumuj wartości z podgrup
        for (const childGroup of updatedChildGroups) {
          for (const fieldId of summaryFieldIds) {
            const childValue = (childGroup as any).summaryValues?.[fieldId] || 0;
            groupSummaryValues[fieldId] = (groupSummaryValues[fieldId] || 0) + childValue;
          }
        }
      }

      // Dla kompatybilności - oblicz standardowe totalNet/totalGross/totalVat
      // UWAGA: Sumy są liczone tylko gdy mamy odpowiednie pola w szablonie
      // Szukaj po fieldName LUB fieldType (dla kompatybilności)
      const valueNetDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'valueNet' || getFieldType(f) === 203
      );
      const valueGrossDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'valueGross' || getFieldType(f) === 204
      );
      const totalVatDef = data.templateStructure.calculatedFields.find(
        (f) => f.fieldName === 'totalVat' || getFieldType(f) === 206
      );

      // totalNet - tylko jeśli mamy pole valueNet w szablonie
      if (valueNetDef) {
        groupTotalNet = updatedItems.reduce((sum, item) => sum + getItemFieldValue(item, valueNetDef.id), 0);
        groupTotalNet += updatedChildGroups.reduce((sum, child) => sum + (child.totalNet || 0), 0);
      }

      // totalGross - tylko jeśli mamy pole valueGross w szablonie
      if (valueGrossDef) {
        groupTotalGross = updatedItems.reduce((sum, item) => sum + getItemFieldValue(item, valueGrossDef.id), 0);
        groupTotalGross += updatedChildGroups.reduce((sum, child) => sum + (child.totalGross || 0), 0);
      }

      // totalVat - tylko jeśli mamy pole totalVat w szablonie
      if (totalVatDef) {
        groupTotalVat = updatedItems.reduce((sum, item) => sum + getItemFieldValue(item, totalVatDef.id), 0);
        groupTotalVat += updatedChildGroups.reduce((sum, child) => sum + (child.totalVat || 0), 0);
      }

      return {
        ...group,
        items: updatedItems,
        childGroups: updatedChildGroups,
        // Zwracaj sumy tylko gdy showGroupSummary jest włączone I mamy odpowiednie pola
        totalNet: showGroupSummary && valueNetDef ? groupTotalNet : undefined,
        totalGross: showGroupSummary && valueGrossDef ? groupTotalGross : undefined,
        totalVat: showGroupSummary && totalVatDef ? groupTotalVat : undefined,
        lastCalculatedAt: new Date().toISOString(),
        // Dodatkowe sumy z konfiguracji (dla rozszerzenia w przyszłości)
        summaryValues: showGroupSummary ? groupSummaryValues : undefined,
      } as CostEstimateGroupWeb & { summaryValues?: Record<string, number> };
    };

    // Przelicz wszystkie root groups
    const recalculatedRootGroups = data.rootGroups.map(recalculateGroup);

    // Oblicz sumy całkowite według konfiguracji
    // UWAGA: Sumy całkowite są liczone tylko gdy showTotalSummary I mamy odpowiednie pola
    // Szukaj po fieldName LUB fieldType (dla kompatybilności) - używamy getFieldType z góry
    const valueNetDef = data.templateStructure.calculatedFields.find((f) => 
      f.fieldName === 'valueNet' || getFieldType(f) === 203
    );
    const valueGrossDef = data.templateStructure.calculatedFields.find((f) => 
      f.fieldName === 'valueGross' || getFieldType(f) === 204
    );
    const totalVatDef = data.templateStructure.calculatedFields.find((f) => 
      f.fieldName === 'totalVat' || getFieldType(f) === 206
    );

    let totalNet: number | undefined;
    let totalGross: number | undefined;
    let totalVat: number | undefined;
    let totalSummaryValues: Record<string, number> = {};

    // Helper: rekursywnie zbierz wszystkie pozycje
    const collectAllItems = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb[] => {
      let allItems: CostEstimateItemWeb[] = [];
      for (const group of groups) {
        if (group.items) {
          allItems = allItems.concat(group.items);
        }
        if (group.childGroups) {
          allItems = allItems.concat(collectAllItems(group.childGroups));
        }
      }
      return allItems;
    };

    if (showTotalSummary) {
      // Zbierz wszystkie pozycje bezpośrednio (niezależnie od showGroupSummary)
      const allItems = collectAllItems(recalculatedRootGroups);
      
      // totalNet - tylko jeśli mamy pole valueNet w szablonie
      if (valueNetDef) {
        totalNet = allItems.reduce((sum, item) => sum + getItemFieldValue(item, valueNetDef.id), 0);
      }
      
      // totalGross - tylko jeśli mamy pole valueGross w szablonie
      if (valueGrossDef) {
        totalGross = allItems.reduce((sum, item) => sum + getItemFieldValue(item, valueGrossDef.id), 0);
      }
      
      // totalVat - tylko jeśli mamy pole totalVat w szablonie
      if (totalVatDef) {
        totalVat = allItems.reduce((sum, item) => sum + getItemFieldValue(item, totalVatDef.id), 0);
      }

      // Zbierz pola do sumowania: z totalSummaryFields config + pola z sumInTotal=true
      const totalSummaryFieldIds = new Set<string>();
      
      for (const sf of totalSummaryFields) {
        totalSummaryFieldIds.add(sf.fieldId);
      }
      
      for (const cf of data.templateStructure.calculatedFields) {
        if (cf.sumInTotal === true) {
          totalSummaryFieldIds.add(cf.id);
        }
      }
      
      for (const fieldId of totalSummaryFieldIds) {
        totalSummaryValues[fieldId] = allItems.reduce((sum, item) => sum + getItemFieldValue(item, fieldId), 0);
      }
    }

    return {
      ...data,
      rootGroups: recalculatedRootGroups,
      totalNet: showTotalSummary && valueNetDef ? totalNet : undefined,
      totalGross: showTotalSummary && valueGrossDef ? totalGross : undefined,
      totalVat: showTotalSummary && totalVatDef ? totalVat : undefined,
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

  const handleAddGroup = (): string | undefined => {
    if (!details) return undefined;

    const newGroupId = `temp_${Date.now()}`;
    const newGroup: CostEstimateGroupWeb = {
      id: newGroupId,
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
    return newGroupId;
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

  const handleAddSubGroup = (parentGroupId: string): string | undefined => {
    if (!details) return undefined;

    const newSubGroupId = `temp_${Date.now()}`;
    
    const findAndAddSubGroup = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
      return groups.map((group) => {
        if (group.id === parentGroupId) {
          const childGroups = group.childGroups || [];
          const newSubGroup: CostEstimateGroupWeb = {
            id: newSubGroupId,
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
    return newSubGroupId;
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
              Szablon: {details.templateName}
            </Text>
          </Box>
        </Stack>

        <Divider my={2} />

        {/* Podsumowanie — wyświetlaj tylko pola z sumInTotal === true w szablonie */}
        {(() => {
          const sumInTotalFields = (details.templateStructure.calculatedFields || [])
            .filter((f) => f.sumInTotal === true)
            .sort((a, b) => a.order - b.order);
          
          if (sumInTotalFields.length === 0) return null;

          return (
            <>
              <Stack direction="row" spacing={4} mb={2} flexWrap="wrap">
                {sumInTotalFields.map((fieldDef) => {
                  let value: number | undefined;
                  
                  // Pobierz wartość sumy z details (standardowe pola)
                  if (fieldDef.fieldName === 'valueNet' || (fieldDef.fieldType ?? (fieldDef as any).fieldTypeConfig?.fieldType) === 203) {
                    value = details.totalNet;
                  } else if (fieldDef.fieldName === 'valueGross' || (fieldDef.fieldType ?? (fieldDef as any).fieldTypeConfig?.fieldType) === 204) {
                    value = details.totalGross;
                  } else if (fieldDef.fieldName === 'totalVat' || (fieldDef.fieldType ?? (fieldDef as any).fieldTypeConfig?.fieldType) === 206) {
                    value = details.totalVat;
                  } else {
                    // Fallback: szukaj w summaryValues
                    value = (details as any).summaryValues?.[fieldDef.id];
                  }

                  return (
                    <Box key={fieldDef.id}>
                      <Text fontSize="xs" color="gray.600">{fieldDef.label || fieldDef.fieldName}</Text>
                      <Text fontSize="lg" fontWeight="semibold">
                        {typeof value === 'number' ? value.toFixed(2) : '0.00'} {details.selectedCurrencySymbol || details.selectedCurrencyCode}
                      </Text>
                    </Box>
                  );
                })}
              </Stack>
              <Divider my={2} />
            </>
          );
        })()}

        {/* Przyciski akcji */}
        <Stack direction="row" spacing={2} alignItems="center" justifyContent="space-between">
          <Stack direction="row" spacing={2}>
            <Button
              colorScheme="blue"
              leftIcon={saving ? <Spinner size="sm" /> : <Save size={16} />}
              onClick={handleSave}
              isDisabled={!hasChanges || saving || !isEditMode}
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
          
          {/* Przełącznik Edycja / Podgląd */}
          <ButtonGroup isAttached variant="outline" size="sm">
            <Button
              leftIcon={<Icon as={Pencil} boxSize={4} />}
              colorScheme={isEditMode ? 'blue' : 'gray'}
              variant={isEditMode ? 'solid' : 'outline'}
              onClick={() => setIsEditMode(true)}
            >
              Edycja
            </Button>
            <Button
              leftIcon={<Icon as={Eye} boxSize={4} />}
              colorScheme={!isEditMode ? 'blue' : 'gray'}
              variant={!isEditMode ? 'solid' : 'outline'}
              onClick={() => setIsEditMode(false)}
            >
              Podgląd
            </Button>
          </ButtonGroup>
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
        editable={isEditMode}
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
