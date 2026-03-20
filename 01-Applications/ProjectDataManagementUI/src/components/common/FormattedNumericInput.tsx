import React, { useState, useEffect } from 'react';
import { Input } from '@chakra-ui/react';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Formatuje wartość liczbową: min 2 miejsca po przecinku, trimuje zbędne zera powyżej 2.
 * Przykłady: 12 → "12,00", 12.3 → "12,30", 12.333 → "12,333", 12.33300 → "12,333"
 */
export function formatNumericDisplay(val: string): string {
  if (!val || val === '' || val === '-') return val;
  const dotVal = val.replace(',', '.');
  const num = parseFloat(dotVal);
  if (isNaN(num)) return val;
  const parts = dotVal.split('.');
  const rawDecimals = parts[1]?.length || 0;
  // Limit precyzji do 20 miejsc po przecinku — to max sensowna precyzja w UI
  // (JS Number.EPSILON ~2.2e-16, wyświetlanie >20 miejsc nie ma wartości dla użytkownika)
  const decimals = Math.min(rawDecimals, 20);
  if (decimals <= 2) {
    return num.toFixed(2).replace('.', ',');
  }
  let formatted = num.toFixed(decimals);
  // Trimuj trailing zeros, ale zostaw min 2
  while (formatted.endsWith('0') && formatted.split('.')[1].length > 2) {
    formatted = formatted.slice(0, -1);
  }
  return formatted.replace('.', ',');
}

// ---------------------------------------------------------------------------
// Komponent
// ---------------------------------------------------------------------------

export interface FormattedNumericInputProps {
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  disabled?: boolean;
}

/**
 * Input numeryczny z lokalnym stanem i polskim formatowaniem (przecinek jako separator).
 * Przy blur formatuje wartość do min 2 miejsc po przecinku.
 */
export const FormattedNumericInput: React.FC<FormattedNumericInputProps> = ({
  value,
  onChange,
  disabled,
}) => {
  // Lokalny stan — przechowuje dokładnie to co user wpisuje (z przecinkiem, z trailing zeros)
  const [localValue, setLocalValue] = useState(() => {
    if (!value || value === '') return '';
    return formatNumericDisplay(value);
  });
  const [isFocused, setIsFocused] = useState(false);

  // Sync z parent TYLKO gdy input nie jest aktywny
  useEffect(() => {
    if (!isFocused) {
      if (!value || value === '') {
        setLocalValue('');
      } else {
        setLocalValue(formatNumericDisplay(value));
      }
    }
  }, [value, isFocused]);

  return (
    <Input
      type="text"
      inputMode="decimal"
      value={localValue}
      onChange={(e) => {
        const v = e.target.value;
        // Pozwól na puste, minus, albo poprawny format liczbowy z przecinkiem
        if (v === '' || v === '-' || /^-?\d*,?\d*$/.test(v)) {
          setLocalValue(v);
          // Wyślij do parenta z kropką (format wewnętrzny)
          const dotVal = v.replace(',', '.');
          onChange(dotVal || undefined);
        }
      }}
      onFocus={() => setIsFocused(true)}
      onBlur={() => {
        setIsFocused(false);
        // Sformatuj wartość: min 2 miejsca po przecinku
        if (localValue && localValue !== '' && localValue !== '-') {
          const formatted = formatNumericDisplay(localValue);
          setLocalValue(formatted);
          onChange(formatted.replace(',', '.') || undefined);
        }
      }}
      isDisabled={disabled}
      size="sm"
      textAlign="right"
      variant="outline"
      bg="white"
      borderColor="gray.300"
      _hover={{ borderColor: 'blue.400' }}
      _focus={{
        borderColor: 'blue.500',
        boxShadow: '0 0 0 1px var(--chakra-colors-blue-500)',
      }}
    />
  );
};
