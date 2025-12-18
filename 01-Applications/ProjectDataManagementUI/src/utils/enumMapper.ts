import { GroupHeaderFieldType } from '../types/costEstimate.types';
import type { CostEstimateGroup, CostEstimateDataModel } from '../types/costEstimate.types';

/**
 * Mapper konwertujący numeryczne wartości enumów z backendu na stringi
 */

/**
 * Konwertuje numeryczny klucz GroupHeaderFieldType na nazwę enuma jako string
 * Backend zwraca liczby (0, 1, 2...), frontend potrzebuje stringów ("GroupName", "GroupDescription"...)
 */
export function mapGroupHeaderFieldTypeToString(numericValue: number): string {
  return GroupHeaderFieldType[numericValue] || String(numericValue);
}

/**
 * Konwertuje headerValues z numerycznych kluczy (z backendu) na stringowe nazwy enumów
 */
export function convertHeaderValuesFromBackend(headerValues: Record<string, any>): Record<string, any> {
  const converted: Record<string, any> = {};
  
  for (const [key, value] of Object.entries(headerValues)) {
    const numericKey = parseInt(key);
    
    if (!isNaN(numericKey)) {
      // Klucz numeryczny z backendu - konwertuj na nazwę enuma
      const enumName = mapGroupHeaderFieldTypeToString(numericKey);
      converted[enumName] = value;
    } else {
      // Już jest stringiem - pozostaw bez zmian
      converted[key] = value;
    }
  }
  
  return converted;
}

/**
 * Konwertuje headerValues ze stringowych nazw enumów na numeryczne klucze (dla backendu)
 * UWAGA: Backend walidator używa fieldDef.Type.ToString() więc oczekuje nazw enumów jako kluczy!
 * Funkcja pozostawia klucze jako stringi (nazwy enumów)
 */
export function convertHeaderValuesForBackend(headerValues: Record<string, any>): Record<string, any> {
  // Backend oczekuje nazw enumów jako kluczy, więc po prostu zwracamy bez zmian
  return headerValues;
}

/**
 * Rekurencyjnie konwertuje headerValues w całej strukturze grup z backendu (liczby -> stringi)
 */
export function convertGroupFromBackend(group: CostEstimateGroup): CostEstimateGroup {
  return {
    ...group,
    headerValues: convertHeaderValuesFromBackend(group.headerValues),
    subGroups: group.subGroups?.map(convertGroupFromBackend),
  };
}

/**
 * Rekurencyjnie konwertuje headerValues w całej strukturze grup dla backendu
 * Backend oczekuje nazw enumów, więc po prostu zwracamy bez zmian
 */
export function convertGroupForBackend(group: CostEstimateGroup): CostEstimateGroup {
  return {
    ...group,
    headerValues: convertHeaderValuesForBackend(group.headerValues),
    subGroups: group.subGroups?.map(convertGroupForBackend),
  };
}

/**
 * Konwertuje cały data model z backendu (liczby -> stringi)
 */
export function convertDataModelFromBackend(dataModel: CostEstimateDataModel): CostEstimateDataModel {
  return {
    ...dataModel,
    groups: dataModel.groups.map(convertGroupFromBackend),
  };
}

/**
 * Konwertuje cały data model dla backendu
 * Backend walidator używa fieldDef.Type.ToString() więc oczekuje nazw enumów jako kluczy
 */
export function convertDataModelForBackend(dataModel: CostEstimateDataModel): CostEstimateDataModel {
  // Backend oczekuje nazw enumów, więc po prostu zwracamy bez zmian
  return dataModel;
}
