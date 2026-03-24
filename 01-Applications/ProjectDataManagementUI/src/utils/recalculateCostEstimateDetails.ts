/**
 * Przeliczanie sum i wartości kalkulowanych kosztorysu (live preview).
 *
 * Odpowiada za:
 *  1. Przeliczenie wartości kalkulowanych w każdej pozycji (quantity × unitPrice itp.)
 *  2. Obsługę komponentów (sumowanie z komponentów do pozycji nadrzędnej)
 *  3. Obsługę opcji z zaznaczeniem (kopiowanie wartości z zaznaczonej opcji)
 *  4. Agregację sum na poziomie grup i całego kosztorysu
 */

import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
  CostEstimateFieldValueWeb,
} from '../types/costEstimate.types.new';
import { getFieldValueAsNumber, getFieldValueAsBoolean } from '../types/costEstimate.types.new';
import type { SummaryFieldWeb } from '../types/costEstimate.types';

// ---------------------------------------------------------------------------
// Helpery wewnętrzne
// ---------------------------------------------------------------------------

/** Pobiera fieldType z definicji pola (obsługuje oba formaty) */
const getFieldType = (f: any): number | undefined =>
  f.fieldType ?? f.fieldTypeConfig?.fieldType;

/** Pobiera wartość numeryczną z fieldValues — 0 jeśli brak */
const getItemFieldValue = (item: CostEstimateItemWeb, fieldId: string): number => {
  const fv = item.fieldValues.find((v) => v.fieldDefinitionId === fieldId);
  return getFieldValueAsNumber(fv);
};

/** Pobiera wartość źródłową — undefined gdy pole NIE ma wpisu */
const getSourceFieldValue = (
  item: CostEstimateItemWeb,
  fieldId: string,
): number | undefined => {
  const fv = item.fieldValues.find((v) => v.fieldDefinitionId === fieldId);
  if (!fv) return undefined;
  if (fv.decimalValue !== null && fv.decimalValue !== undefined) return fv.decimalValue;
  if (fv.stringValue) {
    const p = parseFloat(fv.stringValue);
    return isNaN(p) ? undefined : p;
  }
  return undefined;
};

/** 
 * Sprawdza czy pozycja jest zaznaczona do sumowania (pole Selected = 104).
 * Logika musi być spójna z backendem (CostEstimateCalculationService.IsItemSelectedForSumming):
 * - Jeśli szablon nie ma pola Selected → wszystkie pozycje są sumowane
 * - Jeśli ma pole Selected → tylko pozycje z BoolValue == true są sumowane
 *   (null, undefined, false → pozycja NIE jest sumowana)
 */
const isItemSelected = (
  item: CostEstimateItemWeb,
  selectedFieldId: string | undefined,
): boolean => {
  // Jeśli szablon nie ma pola Selected, wszystkie pozycje są sumowane
  if (!selectedFieldId) return true;
  
  const fv = item.fieldValues.find((v) => v.fieldDefinitionId === selectedFieldId);
  // Jeśli pole nie ma wartości, pozycja NIE jest sumowana (spójne z backendem)
  if (!fv) return false;
  // Sprawdź boolValue - tylko true oznacza zaznaczone (spójne z backendem: BoolValue == true)
  if (fv.boolValue === true) return true;
  // Fallback: stringValue 'true'
  if (fv.stringValue?.toLowerCase() === 'true') return true;
  // Domyślnie NIE sumowana (false, null, undefined)
  return false;
};

/** Ustawia wartość pola w pozycji (typowane jako decimalValue) */
const setItemFieldValue = (
  item: CostEstimateItemWeb,
  fieldId: string,
  value: number,
): CostEstimateItemWeb => {
  const existingIndex = item.fieldValues.findIndex(
    (v) => v.fieldDefinitionId === fieldId,
  );
  const newFieldValues = [...item.fieldValues];

  if (existingIndex >= 0) {
    newFieldValues[existingIndex] = {
      ...newFieldValues[existingIndex],
      decimalValue: value,
      stringValue: undefined,
      boolValue: undefined,
      dateTimeValue: undefined,
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
      dateTimeValue: undefined,
    };
    newFieldValues.push(newFieldValue);
  }

  return { ...item, fieldValues: newFieldValues };
};

// ---------------------------------------------------------------------------
// Główna funkcja
// ---------------------------------------------------------------------------

/**
 * Przelicza cały kosztorys: wartości pozycji, sumy grup i sumy całkowite.
 * Zwraca nowy obiekt details z uaktualnionymi wartościami.
 */
export function recalculateCostEstimateDetails(
  data: CostEstimateDetailsWeb,
): CostEstimateDetailsWeb {
  const ts = data.templateStructure;
  const summaryConfig = ts.summaryConfiguration;
  const showGroupSummary = summaryConfig?.showGroupSummary ?? true;
  const showTotalSummary = summaryConfig?.showTotalSummary ?? true;
  const groupSummaryFields: SummaryFieldWeb[] = summaryConfig?.groupSummaryFields || [];
  const totalSummaryFields: SummaryFieldWeb[] = summaryConfig?.totalSummaryFields || [];

  // Znajdź kluczowe definicje pól
  const selectedFieldDef = ts.systemFields.find(
    (f) => f.fieldName === 'selected' || getFieldType(f) === 104,
  );
  const quantityFieldDef = ts.systemFields.find(
    (f) => f.fieldName === 'quantity' || getFieldType(f) === 101,
  );
  const unitPriceNetDef = ts.calculatedFields.find(
    (f) => f.fieldName === 'unitPriceNet' || getFieldType(f) === 200,
  );
  const vatRateDef = ts.calculatedFields.find(
    (f) => f.fieldName === 'vatRate' || getFieldType(f) === 201,
  );
  const unitPriceGrossDef = ts.calculatedFields.find(
    (f) => f.fieldName === 'unitPriceGross' || getFieldType(f) === 202,
  );
  const valueNetDef = ts.calculatedFields.find(
    (f) => f.fieldName === 'valueNet' || getFieldType(f) === 203,
  );
  const valueGrossDef = ts.calculatedFields.find(
    (f) => f.fieldName === 'valueGross' || getFieldType(f) === 204,
  );
  const unitVatDef = ts.calculatedFields.find(
    (f) => f.fieldName === 'unitVat' || getFieldType(f) === 205,
  );
  const totalVatDef = ts.calculatedFields.find(
    (f) => f.fieldName === 'totalVat' || getFieldType(f) === 206,
  );

  const calculatedFieldDefs = [
    unitPriceNetDef, vatRateDef, unitPriceGrossDef,
    valueNetDef, valueGrossDef, unitVatDef, totalVatDef,
  ].filter(Boolean);

  // =========================================================================
  // Oblicz wartości pochodne dla jednej pozycji
  // =========================================================================
  const calculateDerivedValues = (item: CostEstimateItemWeb): CostEstimateItemWeb => {
    let updated = { ...item };

    const quantity = quantityFieldDef
      ? getSourceFieldValue(updated, quantityFieldDef.id)
      : undefined;
    const unitPriceNet = unitPriceNetDef
      ? getSourceFieldValue(updated, unitPriceNetDef.id)
      : undefined;
    const vatRate = vatRateDef
      ? getSourceFieldValue(updated, vatRateDef.id)
      : undefined;

    const has = {
      quantity: quantity !== undefined,
      unitPriceNet: unitPriceNet !== undefined,
      vatRate: vatRate !== undefined,
    };

    let unitPriceGross: number | undefined;
    if (unitPriceGrossDef) {
      if (has.unitPriceNet && has.vatRate) {
        unitPriceGross = unitPriceNet! * (1 + vatRate!);
        updated = setItemFieldValue(updated, unitPriceGrossDef.id, unitPriceGross);
      } else {
        unitPriceGross = getSourceFieldValue(updated, unitPriceGrossDef.id);
      }
    }

    let unitVat: number | undefined;
    if (unitVatDef) {
      if (has.unitPriceNet && has.vatRate) {
        unitVat = unitPriceNet! * vatRate!;
        updated = setItemFieldValue(updated, unitVatDef.id, unitVat);
      } else {
        unitVat = getSourceFieldValue(updated, unitVatDef.id);
      }
    }

    let valueNet: number | undefined;
    if (valueNetDef) {
      if (has.unitPriceNet && has.quantity) {
        valueNet = unitPriceNet! * quantity!;
        updated = setItemFieldValue(updated, valueNetDef.id, valueNet);
      } else {
        valueNet = getSourceFieldValue(updated, valueNetDef.id);
      }
    }

    let totalVat: number | undefined;
    if (totalVatDef) {
      if (valueNet !== undefined && has.vatRate) {
        totalVat = valueNet * vatRate!;
        updated = setItemFieldValue(updated, totalVatDef.id, totalVat);
      } else if (unitVat !== undefined && has.quantity) {
        totalVat = unitVat * quantity!;
        updated = setItemFieldValue(updated, totalVatDef.id, totalVat);
      } else {
        totalVat = getSourceFieldValue(updated, totalVatDef.id);
      }
    }

    if (valueGrossDef) {
      if (unitPriceGross !== undefined && has.quantity) {
        updated = setItemFieldValue(updated, valueGrossDef.id, unitPriceGross * quantity!);
      } else if (valueNet !== undefined && totalVat !== undefined) {
        updated = setItemFieldValue(updated, valueGrossDef.id, valueNet + totalVat);
      } else if (valueNet !== undefined) {
        updated = setItemFieldValue(updated, valueGrossDef.id, valueNet);
      }
    }

    return updated;
  };

  // =========================================================================
  // Przelicz jedną pozycję (z obsługą komponentów i opcji)
  // =========================================================================
  const calculateItemValues = (item: CostEstimateItemWeb): CostEstimateItemWeb => {
    let updated = { ...item };

    // --- Komponenty: przelicz każdy, potem zsumuj do pozycji nadrzędnej ---
    if (updated.components && updated.components.length > 0) {
      const recalcComponents = updated.components.map((comp) => {
        const recalced = calculateItemValues({ ...comp, components: undefined });
        return { ...recalced, options: comp.options };
      });
      updated = { ...updated, components: recalcComponents };

      const summable = [
        { def: valueNetDef, ft: 203 },
        { def: valueGrossDef, ft: 204 },
        { def: totalVatDef, ft: 206 },
      ];
      for (const { def } of summable) {
        if (!def) continue;
        let sum = 0;
        for (const comp of recalcComponents) {
          sum += getItemFieldValue(comp, def.id);
        }
        updated = setItemFieldValue(updated, def.id, sum);
      }
      return updated;
    }

    // --- Opcje z zaznaczeniem: kopiuj wartości z wybranej opcji ---
    if (selectedFieldDef && updated.options && updated.options.length > 0) {
      const selectedOption = updated.options.find((opt) => {
        const sv = opt.fieldValues.find(
          (fv) => fv.fieldDefinitionId === selectedFieldDef.id,
        );
        return getFieldValueAsBoolean(sv);
      });

      if (selectedOption) {
        for (const fieldDef of calculatedFieldDefs) {
          if (!fieldDef) continue;
          const optFv = selectedOption.fieldValues.find(
            (fv) => fv.fieldDefinitionId === fieldDef.id,
          );
          if (optFv) {
            updated = setItemFieldValue(updated, fieldDef.id, getFieldValueAsNumber(optFv));
          }
        }
        // Przelicz pochodne na bazie skopiowanych wartości
        return calculateDerivedValues(updated);
      }
    }

    // --- Standardowe obliczenia ---
    return calculateDerivedValues(updated);
  };

  // =========================================================================
  // Przelicz grupę (bottom-up)
  // =========================================================================
  const recalculateGroup = (group: CostEstimateGroupWeb): CostEstimateGroupWeb => {
    const updatedChildGroups = group.childGroups.map(recalculateGroup);
    const updatedItems = (group.items || []).map(calculateItemValues);

    let groupTotalNet = 0;
    let groupTotalGross = 0;
    let groupTotalVat = 0;
    let groupSummaryValues: Record<string, number> = {};

    if (showGroupSummary) {
      const summaryFieldIds = new Set<string>();
      for (const sf of groupSummaryFields) summaryFieldIds.add(sf.fieldId);
      for (const cf of ts.calculatedFields) {
        if (cf.sumInGroup === true) summaryFieldIds.add(cf.id);
      }

      // Filtruj pozycje po polu Selected (tylko zaznaczone biorą udział w sumowaniu)
      const selectedItems = updatedItems.filter((itm) =>
        isItemSelected(itm, selectedFieldDef?.id)
      );

      for (const fieldId of summaryFieldIds) {
        groupSummaryValues[fieldId] = selectedItems.reduce(
          (sum, itm) => sum + getItemFieldValue(itm, fieldId),
          0,
        );
      }

      for (const child of updatedChildGroups) {
        for (const fieldId of summaryFieldIds) {
          const childVal = (child as any).summaryValues?.[fieldId] || 0;
          groupSummaryValues[fieldId] = (groupSummaryValues[fieldId] || 0) + childVal;
        }
      }
    }

    if (valueNetDef) {
      // Filtruj pozycje po polu Selected
      const selectedItems = updatedItems.filter((itm) =>
        isItemSelected(itm, selectedFieldDef?.id)
      );
      groupTotalNet = selectedItems.reduce(
        (sum, itm) => sum + getItemFieldValue(itm, valueNetDef.id),
        0,
      );
      groupTotalNet += updatedChildGroups.reduce(
        (sum, ch) => sum + (ch.totalNet || 0),
        0,
      );
    }
    if (valueGrossDef) {
      const selectedItems = updatedItems.filter((itm) =>
        isItemSelected(itm, selectedFieldDef?.id)
      );
      groupTotalGross = selectedItems.reduce(
        (sum, itm) => sum + getItemFieldValue(itm, valueGrossDef.id),
        0,
      );
      groupTotalGross += updatedChildGroups.reduce(
        (sum, ch) => sum + (ch.totalGross || 0),
        0,
      );
    }
    if (totalVatDef) {
      const selectedItems = updatedItems.filter((itm) =>
        isItemSelected(itm, selectedFieldDef?.id)
      );
      groupTotalVat = selectedItems.reduce(
        (sum, itm) => sum + getItemFieldValue(itm, totalVatDef.id),
        0,
      );
      groupTotalVat += updatedChildGroups.reduce(
        (sum, ch) => sum + (ch.totalVat || 0),
        0,
      );
    }

    return {
      ...group,
      items: updatedItems,
      childGroups: updatedChildGroups,
      totalNet: showGroupSummary && valueNetDef ? groupTotalNet : undefined,
      totalGross: showGroupSummary && valueGrossDef ? groupTotalGross : undefined,
      totalVat: showGroupSummary && totalVatDef ? groupTotalVat : undefined,
      lastCalculatedAt: new Date().toISOString(),
      summaryValues: showGroupSummary ? groupSummaryValues : undefined,
    } as CostEstimateGroupWeb & { summaryValues?: Record<string, number> };
  };

  // =========================================================================
  // Przelicz wszystko i oblicz sumy globalne
  // =========================================================================
  const recalculatedRootGroups = data.rootGroups.map(recalculateGroup);

  let totalNet: number | undefined;
  let totalGross: number | undefined;
  let totalVat: number | undefined;
  let totalSummaryValues: Record<string, number> = {};

  if (showTotalSummary) {
    const collectAllItems = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb[] => {
      let all: CostEstimateItemWeb[] = [];
      for (const g of groups) {
        if (g.items) all = all.concat(g.items);
        if (g.childGroups) all = all.concat(collectAllItems(g.childGroups));
      }
      return all;
    };

    const allItems = collectAllItems(recalculatedRootGroups);
    
    // Filtruj pozycje po polu Selected (tylko zaznaczone biorą udział w sumowaniu)
    const selectedItems = allItems.filter((itm) =>
      isItemSelected(itm, selectedFieldDef?.id)
    );

    if (valueNetDef) {
      totalNet = selectedItems.reduce(
        (sum, itm) => sum + getItemFieldValue(itm, valueNetDef.id),
        0,
      );
    }
    if (valueGrossDef) {
      totalGross = selectedItems.reduce(
        (sum, itm) => sum + getItemFieldValue(itm, valueGrossDef.id),
        0,
      );
    }
    if (totalVatDef) {
      totalVat = selectedItems.reduce(
        (sum, itm) => sum + getItemFieldValue(itm, totalVatDef.id),
        0,
      );
    }

    const totalSummaryFieldIds = new Set<string>();
    for (const sf of totalSummaryFields) totalSummaryFieldIds.add(sf.fieldId);
    for (const cf of ts.calculatedFields) {
      if (cf.sumInTotal === true) totalSummaryFieldIds.add(cf.id);
    }
    for (const fieldId of totalSummaryFieldIds) {
      totalSummaryValues[fieldId] = selectedItems.reduce(
        (sum, itm) => sum + getItemFieldValue(itm, fieldId),
        0,
      );
    }
  }

  return {
    ...data,
    rootGroups: recalculatedRootGroups,
    totalNet: showTotalSummary && valueNetDef ? totalNet : undefined,
    totalGross: showTotalSummary && valueGrossDef ? totalGross : undefined,
    totalVat: showTotalSummary && totalVatDef ? totalVat : undefined,
    lastCalculatedAt: new Date().toISOString(),
    summaryValues: showTotalSummary ? totalSummaryValues : undefined,
  } as CostEstimateDetailsWeb & { summaryValues?: Record<string, number> };
}
