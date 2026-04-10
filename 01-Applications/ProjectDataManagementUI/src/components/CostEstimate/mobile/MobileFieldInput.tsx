import React from 'react';
import {
  Input,
  NumberInput,
  NumberInputField,
  Textarea,
  Switch,
  FormControl,
  FormLabel,
  Text,
  HStack,
} from '@chakra-ui/react';

interface MobileFieldInputProps {
  fieldDef: any;
  value: string | undefined;
  onChange: (v: string | undefined) => void;
  disabled?: boolean;
}

/**
 * Prosty renderer pola formularza dla modali mobilnych.
 * Obsługuje: tekst, liczba, boolean (Switch), data, textarea.
 * Pomija kolekcje i pliki (wyświetla je jako read-only).
 */
export const MobileFieldInput: React.FC<MobileFieldInputProps> = ({
  fieldDef,
  value,
  onChange,
  disabled,
}) => {
  const cfg = fieldDef?.fieldTypeConfig;

  if (!fieldDef) return null;

  // Pola kolekcji i pliki — tylko podgląd
  if (cfg?.isCollection || cfg?.isFile) {
    return (
      <Text fontSize="sm" color="gray.500" fontStyle="italic">
        {value ?? '—'}
      </Text>
    );
  }

  // Boolean → Switch
  if (cfg?.isBoolean) {
    const checked = value === 'true' || value === '1';
    return (
      <HStack>
        <Switch
          isChecked={checked}
          isDisabled={disabled}
          onChange={(e) => onChange(e.target.checked ? 'true' : 'false')}
          colorScheme="primary"
          size="lg"
        />
        <Text fontSize="sm" color={checked ? 'primary.700' : 'gray.500'}>
          {checked ? 'Tak' : 'Nie'}
        </Text>
      </HStack>
    );
  }

  // Data → input type="date"
  if (cfg?.isDate) {
    const dateVal = value ? value.substring(0, 10) : '';
    return (
      <Input
        type="date"
        size="lg"
        w="100%"
        value={dateVal}
        isDisabled={disabled}
        onChange={(e) => onChange(e.target.value || undefined)}
      />
    );
  }

  // Numeryczne → NumberInput
  if (cfg?.isNumeric) {
    return (
      <NumberInput
        value={value ?? ''}
        isDisabled={disabled}
        onChange={(valStr) => onChange(valStr || undefined)}
        size="lg"
        w="100%"
      >
        <NumberInputField placeholder="0" minW={0} />
      </NumberInput>
    );
  }

  // Tekst wieloliniowy — dla pól o nazwie zawierającej "opis", "komentarz", "uwagi", "comment", "description"
  const fn: string = (fieldDef.fieldName ?? '').toLowerCase();
  const isTextarea = fn.includes('opis') || fn.includes('komentarz') || fn.includes('uwagi') ||
    fn.includes('comment') || fn.includes('description') || fn.includes('note');

  if (isTextarea) {
    return (
      <Textarea
        size="lg"
        rows={3}
        w="100%"
        value={value ?? ''}
        isDisabled={disabled}
        onChange={(e) => onChange(e.target.value || undefined)}
        placeholder={fieldDef.label || fieldDef.customLabel || ''}
      />
    );
  }

  // Domyślnie → Input tekstowy
  return (
    <Input
      size="lg"
      w="100%"
      value={value ?? ''}
      isDisabled={disabled}
      onChange={(e) => onChange(e.target.value || undefined)}
      placeholder={fieldDef.label || fieldDef.customLabel || ''}
    />
  );
};

// ---------------------------------------------------------------------------
// Helpers eksportowane
// ---------------------------------------------------------------------------

/** Zwraca display name etapu na podstawie pierwszego pola tekstowego lub numeru */
export const getGroupDisplayName = (
  group: { fieldValues: Array<{ fieldDefinitionId: string; stringValue?: string }> },
  templateStructure: any,
  groupNumber: string
): string => {
  const textFields = (templateStructure?.groupHeaderFields ?? []).filter(
    (f: any) => !f.fieldTypeConfig?.isNumeric && !f.fieldTypeConfig?.isBoolean && !f.fieldTypeConfig?.isDate && !f.fieldTypeConfig?.isCollection && !f.fieldTypeConfig?.isFile
  );
  for (const field of textFields) {
    const fv = group.fieldValues.find((v) => v.fieldDefinitionId === field.id);
    if (fv?.stringValue) return fv.stringValue;
  }
  return `Etap ${groupNumber}`;
};

/** Zwraca display name pozycji na podstawie pierwszego pola tekstowego lub numeru */
export const getItemDisplayName = (
  item: { fieldValues: Array<{ fieldDefinitionId: string; stringValue?: string }> },
  templateStructure: any,
  itemNumber: number
): string => {
  const allFields = [
    ...(templateStructure?.systemFields ?? []),
    ...(templateStructure?.genericFields ?? []),
  ].filter(
    (f: any) => !f.fieldTypeConfig?.isNumeric && !f.fieldTypeConfig?.isBoolean && !f.fieldTypeConfig?.isDate && !f.fieldTypeConfig?.isCollection && !f.fieldTypeConfig?.isFile
  );
  for (const field of allFields) {
    const fv = item.fieldValues.find((v: any) => v.fieldDefinitionId === field.id);
    if (fv?.stringValue) return fv.stringValue;
  }
  return `Pozycja ${itemNumber}`;
};

/** Formatuje wartość numeryczną do wyświetlenia */
export const formatCurrencyValue = (val: number | undefined, symbol: string): string => {
  if (val === undefined || val === null) return '—';
  return `${val.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${symbol}`;
};

/** Zwraca listę wartości do wyświetlenia w podsumowaniu pozycji (te same pola co w grupie) */
export const getItemSummaryValues = (
  item: { fieldValues: Array<{ fieldDefinitionId: string; decimalValue?: number | null; stringValue?: string | null }> },
  templateStructure: any
): Array<{ label: string; value: number }> => {
  const summaryConfig = templateStructure?.summaryConfiguration;
  const showGroupSummary = summaryConfig?.showGroupSummary ?? true;
  const groupSummaryFields: Array<{ fieldId: string }> = summaryConfig?.groupSummaryFields || [];

  const allFields: any[] = [
    ...(templateStructure?.systemFields || []),
    ...(templateStructure?.calculatedFields || []),
    ...(templateStructure?.genericFields || []),
  ];

  const seen = new Set<string>();
  const results: Array<{ label: string; value: number }> = [];

  for (const fieldDef of allFields) {
    if (seen.has(fieldDef.id)) continue;
    const hasSumInGroupFlag = fieldDef.sumInGroup === true;
    const isInSummaryFields = groupSummaryFields.some((sf: { fieldId: string }) => sf.fieldId === fieldDef.id);
    const fn = fieldDef.fieldName;
    const ft = fieldDef.fieldType ?? fieldDef.fieldTypeConfig?.fieldType;
    const isDefaultSumField =
      showGroupSummary &&
      (fn === 'valueNet' || ft === 203 || fn === 'valueGross' || ft === 204 || fn === 'totalVat' || ft === 206);

    if (!(hasSumInGroupFlag || isInSummaryFields || isDefaultSumField)) continue;

    const fv = item.fieldValues.find((v) => v.fieldDefinitionId === fieldDef.id);
    if (!fv) continue;

    const value =
      fv.decimalValue !== undefined && fv.decimalValue !== null
        ? fv.decimalValue
        : fv.stringValue
          ? parseFloat(fv.stringValue)
          : undefined;

    if (value === undefined || isNaN(value)) continue;

    seen.add(fieldDef.id);
    results.push({ label: fieldDef.label || fieldDef.customLabel || fieldDef.fieldName, value });
  }

  return results;
};

/** Zwraca listę wartości do wyświetlenia w podsumowaniu grupy na podstawie szablonu */
export const getGroupSummaryValues = (
  group: { totalNet?: number; totalGross?: number; totalVat?: number; summaryValues?: Record<string, number> },
  templateStructure: any
): Array<{ label: string; value: number }> => {
  const summaryConfig = templateStructure?.summaryConfiguration;
  const showGroupSummary = summaryConfig?.showGroupSummary ?? true;
  const groupSummaryFields: Array<{ fieldId: string }> = summaryConfig?.groupSummaryFields || [];

  const allFields: any[] = [
    ...(templateStructure?.systemFields || []),
    ...(templateStructure?.calculatedFields || []),
    ...(templateStructure?.genericFields || []),
  ];

  const seen = new Set<string>();
  const results: Array<{ label: string; value: number }> = [];

  for (const fieldDef of allFields) {
    if (seen.has(fieldDef.id)) continue;
    const hasSumInGroupFlag = fieldDef.sumInGroup === true;
    const isInSummaryFields = groupSummaryFields.some((sf) => sf.fieldId === fieldDef.id);
    const fn = fieldDef.fieldName;
    const ft = fieldDef.fieldType ?? fieldDef.fieldTypeConfig?.fieldType;
    const isDefaultSumField =
      showGroupSummary &&
      (fn === 'valueNet' || ft === 203 || fn === 'valueGross' || ft === 204 || fn === 'totalVat' || ft === 206);

    if (!(hasSumInGroupFlag || isInSummaryFields || isDefaultSumField)) continue;

    let value: number | undefined;
    if (fn === 'valueNet' || ft === 203) {
      value = group.totalNet;
    } else if (fn === 'valueGross' || ft === 204) {
      value = group.totalGross;
    } else if (fn === 'totalVat' || ft === 206) {
      value = group.totalVat;
    } else {
      value = group.summaryValues?.[fieldDef.id];
    }

    if (value !== undefined) {
      seen.add(fieldDef.id);
      results.push({ label: fieldDef.label || fieldDef.customLabel || fieldDef.fieldName, value });
    }
  }

  return results;
};

/** Zwraca fieldSource dla pola na podstawie template structure */
export const getFieldSourceForDef = (
  fieldDef: any,
  templateStructure: any
): 'system' | 'calculated' | 'generic' => {
  if (templateStructure?.systemFields?.find((f: any) => f.id === fieldDef.id)) return 'system';
  if (templateStructure?.calculatedFields?.find((f: any) => f.id === fieldDef.id)) return 'calculated';
  return 'generic';
};

/**
 * Czyta wartość pola z item.fieldValues jako string.
 * Bezpieczna obsługa null dla boolValue/stringValue/dateTimeValue (API może zwrócić null zamiast undefined).
 */
export const readItemFieldValue = (
  item: { fieldValues: Array<{ fieldDefinitionId: string; stringValue?: string | null; decimalValue?: number | null; boolValue?: boolean | null; dateTimeValue?: string | null }> },
  fieldId: string
): string | undefined => {
  const fv = item.fieldValues.find((v) => v.fieldDefinitionId === fieldId);
  if (!fv) return undefined;
  return (
    (fv.stringValue ?? undefined) ??
    (fv.decimalValue !== undefined && fv.decimalValue !== null ? fv.decimalValue.toString() : undefined) ??
    (fv.boolValue !== undefined && fv.boolValue !== null ? fv.boolValue.toString() : undefined) ??
    (fv.dateTimeValue ?? undefined) ??
    undefined
  );
};

/**
 * Zwraca pola pozycji/komponentu w kolejności z uiConfiguration.columns.
 * Ta sama logika co kolumny tabeli — obie modalne sekcje pól są identyczne.
 */
export const getOrderedFields = (templateStructure: any): any[] => {
  const columns: any[] = templateStructure?.uiConfiguration?.columns || [];
  if (columns.length === 0) {
    return [
      ...(templateStructure?.systemFields ?? []),
      ...(templateStructure?.calculatedFields ?? []),
      ...(templateStructure?.genericFields ?? []),
    ].filter((f: any) => !(f.fieldTypeConfig?.isCollection && f.childFields?.length > 0));
  }
  const sorted = [...columns].sort((a: any, b: any) => a.order - b.order);
  const result: any[] = [];
  for (const col of sorted) {
    const isGroupField = templateStructure?.groupHeaderFields?.some(
      (f: any) => f.fieldName === col.fieldName
    );
    if (isGroupField) continue;
    const fieldDef: any =
      templateStructure?.systemFields?.find((f: any) => f.fieldName === col.fieldName) ??
      templateStructure?.calculatedFields?.find((f: any) => f.fieldName === col.fieldName) ??
      templateStructure?.genericFields?.find((f: any) => f.fieldName === col.fieldName);
    if (!fieldDef) continue;
    if (fieldDef.fieldTypeConfig?.isCollection && fieldDef.childFields?.length > 0) continue;
    result.push(fieldDef);
  }
  return result;
};
