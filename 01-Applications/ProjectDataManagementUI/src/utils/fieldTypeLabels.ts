/**
 * Etykiety i funkcje pomocnicze dla typów pól kosztorysu
 */

import { 
  FieldType, 
  FieldScope, 
  GroupHeaderFieldType, 
  SystemFieldType, 
  CalculatedFieldType, 
  GenericFieldType 
} from '../types/costEstimate.types';

/**
 * Etykiety dla wszystkich typów pól (FieldType)
 */
export const fieldTypeLabels: Record<FieldType, string> = {
  // GROUP HEADER FIELDS (0-9)
  [FieldType.GroupName]: 'Nazwa grupy',
  [FieldType.GroupDescription]: 'Opis grupy',
  [FieldType.GroupNumber]: 'Numer grupy',
  [FieldType.GroupStartDate]: 'Data rozpoczęcia',
  [FieldType.GroupEndDate]: 'Data zakończenia',
  [FieldType.GroupStatus]: 'Status',
  [FieldType.GroupNotes]: 'Notatki',
  [FieldType.GroupResponsible]: 'Odpowiedzialny',
  [FieldType.GroupBudget]: 'Budżet',
  [FieldType.GroupPriority]: 'Priorytet',

  // ITEM SYSTEM FIELDS (100-199)
  [FieldType.ItemSystemName]: 'Nazwa pozycji',
  [FieldType.ItemSystemQuantity]: 'Ilość',
  [FieldType.ItemSystemUnit]: 'Jednostka miary',
  [FieldType.ItemSystemOptions]: 'Opcje',
  [FieldType.ItemSystemSelected]: 'Zaznaczenie',

  // ITEM CALCULATED FIELDS (200-299)
  [FieldType.ItemCalculatedUnitPriceNet]: 'Cena jednostkowa netto',
  [FieldType.ItemCalculatedVatRate]: 'Stawka VAT',
  [FieldType.ItemCalculatedUnitPriceGross]: 'Cena jednostkowa brutto',
  [FieldType.ItemCalculatedValueNet]: 'Wartość netto',
  [FieldType.ItemCalculatedValueGross]: 'Wartość brutto',
  [FieldType.ItemCalculatedUnitVat]: 'VAT jednostkowy',
  [FieldType.ItemCalculatedTotalVat]: 'VAT całkowity',

  // ITEM GENERIC FIELDS (300-399)
  [FieldType.ItemGenericInteger]: 'Liczba całkowita',
  [FieldType.ItemGenericDecimal]: 'Liczba dziesiętna',
  [FieldType.ItemGenericString]: 'Tekst',
  [FieldType.ItemGenericBoolean]: 'Tak/Nie',
  [FieldType.ItemGenericDate]: 'Data',
  [FieldType.ItemGenericDateTime]: 'Data i czas',
};

/**
 * Etykiety dla zakresów pól (FieldScope)
 */
export const fieldScopeLabels: Record<number, string> = {
  [FieldScope.Group]: 'Nagłówek grupy',
  [FieldScope.ItemSystem]: 'Pole systemowe',
  [FieldScope.ItemCalculated]: 'Pole obliczeniowe',
  [FieldScope.ItemGeneric]: 'Pole generyczne',
};

/**
 * Sprawdza czy pole jest sumowalne (ValueNet, ValueGross, TotalVat)
 */
export function isSummableField(fieldType: FieldType): boolean {
  return (
    fieldType === FieldType.ItemCalculatedValueNet ||
    fieldType === FieldType.ItemCalculatedValueGross ||
    fieldType === FieldType.ItemCalculatedTotalVat
  );
}

/**
 * Zwraca scope dla danego FieldType
 */
export function getFieldScope(fieldType: FieldType): FieldScope {
  if (fieldType >= 0 && fieldType <= 9) {
    return FieldScope.Group;
  }
  if (fieldType >= 100 && fieldType <= 199) {
    return FieldScope.ItemSystem;
  }
  if (fieldType >= 200 && fieldType <= 299) {
    return FieldScope.ItemCalculated;
  }
  if (fieldType >= 300 && fieldType <= 399) {
    return FieldScope.ItemGeneric;
  }
  // Domyślnie zwróć ItemGeneric dla nieznanych typów
  return FieldScope.ItemGeneric;
}

/**
 * Konwertuje nowy FieldType na legacy typ (GroupHeaderFieldType, SystemFieldType, CalculatedFieldType, GenericFieldType)
 * 
 * @param fieldType - FieldType z zakresu 0-9, 100-199, 200-299, 300-399
 * @returns Legacy typ odpowiadający danemu FieldType (wartość 0-9 dla każdej kategorii)
 */
export function convertFieldTypeToLegacy(fieldType: number): number {
  // Group Header Fields (0-9) → GroupHeaderFieldType (0-9)
  if (fieldType >= 0 && fieldType <= 9) {
    return fieldType as GroupHeaderFieldType;
  }
  // Item System Fields (100-199) → SystemFieldType (0-4)
  if (fieldType >= 100 && fieldType <= 199) {
    return (fieldType - 100) as SystemFieldType;
  }
  // Item Calculated Fields (200-299) → CalculatedFieldType (0-6)
  if (fieldType >= 200 && fieldType <= 299) {
    return (fieldType - 200) as CalculatedFieldType;
  }
  // Item Generic Fields (300-399) → GenericFieldType (0-5)
  if (fieldType >= 300 && fieldType <= 399) {
    return (fieldType - 300) as GenericFieldType;
  }
  // Fallback - zwróć oryginalną wartość
  return fieldType;
}

/**
 * Konwertuje legacy typ na nowy FieldType
 * 
 * @param legacyType - Wartość legacy enum (0-9)
 * @param scope - FieldScope określający kategorię pola
 * @returns FieldType z odpowiedniego zakresu
 */
export function convertLegacyToFieldType(legacyType: number, scope: FieldScope): FieldType {
  switch (scope) {
    case FieldScope.Group:
      return legacyType as FieldType; // 0-9 → 0-9
    case FieldScope.ItemSystem:
      return (legacyType + 100) as FieldType; // 0-4 → 100-104
    case FieldScope.ItemCalculated:
      return (legacyType + 200) as FieldType; // 0-6 → 200-206
    case FieldScope.ItemGeneric:
      return (legacyType + 300) as FieldType; // 0-5 → 300-305
    default:
      return legacyType as FieldType;
  }
}

/**
 * Pobiera etykietę dla dowolnego typu pola (FieldType lub legacy)
 */
export function getFieldTypeLabel(fieldType: number, scope?: FieldScope): string {
  // Jeśli to już FieldType (wartość z odpowiedniego zakresu)
  if (fieldType in fieldTypeLabels) {
    return fieldTypeLabels[fieldType as FieldType];
  }
  
  // Jeśli to legacy typ i mamy scope
  if (scope !== undefined) {
    const newFieldType = convertLegacyToFieldType(fieldType, scope);
    return fieldTypeLabels[newFieldType] ?? `Nieznany typ (${fieldType})`;
  }
  
  return `Nieznany typ (${fieldType})`;
}
