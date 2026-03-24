import type {
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  GroupHeaderFieldDefinition,
} from '../types/costEstimate.types';
import { CalculatedFieldType } from '../types/costEstimate.types';

export interface ValidationError {
  fieldName: string;
  message: string;
}

/**
 * Walidacja pola calculated zgodnie z definicją
 */
export function validateCalculatedField(
  field: CalculatedFieldDefinition,
  value: any,
  allValues: Record<string, any>
): ValidationError | null {
  const fieldValue = value as number | null | undefined;

  // Required validation
  if (field.required && (fieldValue === null || fieldValue === undefined)) {
    return {
      fieldName: field.name,
      message: `${field.label} jest wymagane`,
    };
  }

  // Skip further validation if value is empty and not required
  if (fieldValue === null || fieldValue === undefined) {
    return null;
  }

  // Numeric validation for calculated fields (always numeric)
  if (typeof fieldValue !== 'number' || isNaN(fieldValue)) {
    return {
      fieldName: field.name,
      message: `${field.label} musi być liczbą`,
    };
  }

  // VatRate przechowywany jako ułamek dziesiętny (0–1), czyli 23% = 0.23
  if (field.type === CalculatedFieldType.VatRate) {
    if (fieldValue < 0) {
      return { fieldName: field.name, message: `${field.label} nie może być ujemna` };
    }
    if (fieldValue > 1) {
      return { fieldName: field.name, message: `${field.label} nie może przekraczać 100%` };
    }
  }

  return null;
}

/**
 * Walidacja pola generic zgodnie z definicją
 */
export function validateGenericField(
  field: GenericFieldDefinition,
  value: any,
  allValues: Record<string, any>
): ValidationError | null {
  // Required validation
  if (field.required) {
    if (value === null || value === undefined || value === '') {
      return {
        fieldName: field.name,
        message: `${field.label} jest wymagane`,
      };
    }
  }

  // Skip further validation if value is empty and not required
  if (value === null || value === undefined || value === '') {
    return null;
  }

  // Type-specific validation
  switch (field.type) {
    case 0: // Integer
    case 1: // Decimal
      const numValue = typeof value === 'number' ? value : parseFloat(value);
      if (isNaN(numValue)) {
        return {
          fieldName: field.name,
          message: `${field.label} musi być liczbą`,
        };
      }

      if (field.minValue !== null && field.minValue !== undefined && numValue < field.minValue) {
        return {
          fieldName: field.name,
          message: `${field.label} nie może być mniejsze niż ${field.minValue}`,
        };
      }

      if (field.maxValue !== null && field.maxValue !== undefined && numValue > field.maxValue) {
        return {
          fieldName: field.name,
          message: `${field.label} nie może być większe niż ${field.maxValue}`,
        };
      }
      break;

    case 2: // String
      const strValue = String(value);

      if (field.minLength && strValue.length < field.minLength) {
        return {
          fieldName: field.name,
          message: `${field.label} musi mieć co najmniej ${field.minLength} znaków`,
        };
      }

      if (field.maxLength && strValue.length > field.maxLength) {
        return {
          fieldName: field.name,
          message: `${field.label} nie może mieć więcej niż ${field.maxLength} znaków`,
        };
      }

      if (field.pattern) {
        try {
          const regex = new RegExp(field.pattern);
          if (!regex.test(strValue)) {
            return {
              fieldName: field.name,
              message: `${field.label} ma nieprawidłowy format`,
            };
          }
        } catch (e) {
        }
      }

      if (field.allowedValues && field.allowedValues.length > 0) {
        if (!field.allowedValues.includes(strValue)) {
          return {
            fieldName: field.name,
            message: `${field.label} musi być jedną z dozwolonych wartości`,
          };
        }
      }
      break;

    case 3: // Boolean
      if (typeof value !== 'boolean') {
        return {
          fieldName: field.name,
          message: `${field.label} musi być wartością logiczną`,
        };
      }
      break;

    case 4: // Date
    case 5: // DateTime
      const dateValue = value instanceof Date ? value : new Date(value);
      if (isNaN(dateValue.getTime())) {
        return {
          fieldName: field.name,
          message: `${field.label} ma nieprawidłowy format daty`,
        };
      }
      break;

    case 10: // Collection
      if (!Array.isArray(value)) {
        return {
          fieldName: field.name,
          message: `${field.label} musi być listą`,
        };
      }

      if (field.nestedFields?.minItems && value.length < field.nestedFields.minItems) {
        return {
          fieldName: field.name,
          message: `${field.label} musi zawierać co najmniej ${field.nestedFields.minItems} elementów`,
        };
      }

      if (field.nestedFields?.maxItems && value.length > field.nestedFields.maxItems) {
        return {
          fieldName: field.name,
          message: `${field.label} nie może zawierać więcej niż ${field.nestedFields.maxItems} elementów`,
        };
      }
      break;
  }

  return null;
}

/**
 * Walidacja pola nagłówka grupy
 */
export function validateGroupHeaderField(
  field: GroupHeaderFieldDefinition,
  value: any
): ValidationError | null {
  // GroupName (type 0) jest zawsze wymagane niezależnie od ustawienia w szablonie
  const isRequired = field.required || field.type === 0;
  
  // Required validation
  if (isRequired && (value === null || value === undefined || value === '')) {
    return {
      fieldName: field.customLabel || field.type.toString(),
      message: `${field.customLabel || getGroupHeaderFieldLabel(field.type)} jest wymagane`,
    };
  }

  // Skip further validation if value is empty and not required
  if (value === null || value === undefined || value === '') {
    return null;
  }

  // Type-specific validation based on GroupHeaderFieldType
  switch (field.type) {
    case 3: // StartDate
    case 4: // EndDate
      const dateValue = value instanceof Date ? value : new Date(value);
      if (isNaN(dateValue.getTime())) {
        return {
          fieldName: field.customLabel || field.type.toString(),
          message: `${field.customLabel || getGroupHeaderFieldLabel(field.type)} ma nieprawidłowy format daty`,
        };
      }
      break;

    case 8: // Budget
      const numValue = typeof value === 'number' ? value : parseFloat(value);
      if (isNaN(numValue)) {
        return {
          fieldName: field.customLabel || field.type.toString(),
          message: `${field.customLabel || getGroupHeaderFieldLabel(field.type)} musi być liczbą`,
        };
      }
      if (numValue < 0) {
        return {
          fieldName: field.customLabel || field.type.toString(),
          message: `${field.customLabel || getGroupHeaderFieldLabel(field.type)} nie może być ujemne`,
        };
      }
      break;

    case 9: // Priority
      const priorityValue = typeof value === 'number' ? value : parseInt(value, 10);
      if (isNaN(priorityValue)) {
        return {
          fieldName: field.customLabel || field.type.toString(),
          message: `${field.customLabel || getGroupHeaderFieldLabel(field.type)} musi być liczbą całkowitą`,
        };
      }
      break;
  }

  // AllowedValues validation
  if (field.allowedValues && field.allowedValues.length > 0) {
    const strValue = String(value);
    if (!field.allowedValues.includes(strValue)) {
      return {
        fieldName: field.customLabel || field.type.toString(),
        message: `${field.customLabel || getGroupHeaderFieldLabel(field.type)} musi być jedną z dozwolonych wartości`,
      };
    }
  }

  return null;
}

/**
 * Pobiera domyślną etykietę dla typu pola nagłówka grupy
 */
function getGroupHeaderFieldLabel(type: number): string {
  const labels: Record<number, string> = {
    0: 'Nazwa grupy',
    1: 'Opis grupy',
    2: 'Numer grupy',
    3: 'Data rozpoczęcia',
    4: 'Data zakończenia',
    5: 'Status',
    6: 'Uwagi',
    7: 'Odpowiedzialny',
    8: 'Budżet',
    9: 'Priorytet',
  };
  return labels[type] || 'Pole';
}

/**
 * Sprawdza czy pole powinno być widoczne na podstawie warunku
 */
export function evaluateVisibilityCondition(
  condition: string | undefined,
  allValues: Record<string, any>
): boolean {
  if (!condition) return true;

  try {
    // Prosta ewaluacja warunku (można rozszerzyć o bardziej zaawansowaną logikę)
    // Format: "fieldName == 'value'" lub "fieldName > 10"
    const func = new Function(...Object.keys(allValues), `return ${condition}`);
    return func(...Object.values(allValues));
  } catch (e) {
    return true; // W przypadku błędu pokazuj pole
  }
}

/**
 * Sprawdza czy pole powinno być wymagane na podstawie warunku
 */
export function evaluateRequiredCondition(
  condition: string | undefined,
  allValues: Record<string, any>
): boolean {
  if (!condition) return false;

  try {
    const func = new Function(...Object.keys(allValues), `return ${condition}`);
    return func(...Object.values(allValues));
  } catch (e) {
    return false;
  }
}
