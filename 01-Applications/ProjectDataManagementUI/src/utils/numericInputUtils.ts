/**
 * Pomocniki do pól numerycznych z separatorem dziesiętnym (`,` lub `.`).
 * Umożliwia wpisywanie wartości zmienno-przecinkowych bez utraty separatora w trakcie edycji.
 */

export const DECIMAL_SCALE = 2;

const PARTIAL_NUMERIC_PATTERN = /^-?\d+[.,]$/;
const VALID_NUMERIC_PATTERN = /^-?\d*([.,]\d*)?$/;

/** Zaokrągla do podanej liczby miejsc po przecinku (bez błędów reprezentacji float). */
export function roundToDecimals(value: number, decimals: number = DECIMAL_SCALE): number {
  return Number(`${Math.round(Number(`${value}e${decimals}`))}e-${decimals}`);
}

/** Formatuje liczbę do pola input — zawsze 2 miejsca, separator `,`, bez separatora tysięcy. */
export function formatDecimalInput(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return '';
  }
  return value.toLocaleString('pl-PL', {
    minimumFractionDigits: DECIMAL_SCALE,
    maximumFractionDigits: DECIMAL_SCALE,
    useGrouping: false,
  });
}

/** Wartość wyświetlana w polu numerycznym (poza trybem edycji). */
export function resolveNumericInputDisplayValue(value: string | number | undefined | null): string {
  if (value === undefined || value === null || value === '') {
    return '';
  }
  if (typeof value === 'number') {
    return formatDecimalInput(value);
  }
  const trimmed = value.trim();
  if (trimmed === '' || isPartialNumericInput(trimmed)) {
    return trimmed;
  }
  const parsed = parseNumericInput(trimmed);
  return parsed !== null ? formatDecimalInput(parsed) : trimmed;
}

/** Filtruje znak po znaku — dopuszcza jeden separator dziesiętny i max 2 cyfry po nim. */
export function sanitizeNumericInput(
  raw: string,
  maxDecimals: number = DECIMAL_SCALE,
): string {
  let result = '';
  let hasSeparator = false;
  let decimalCount = 0;

  for (let i = 0; i < raw.length; i++) {
    const char = raw[i];
    if (char >= '0' && char <= '9') {
      if (hasSeparator) {
        if (decimalCount >= maxDecimals) {
          continue;
        }
        decimalCount++;
      }
      result += char;
      continue;
    }
    if (char === '-' && result.length === 0) {
      result += char;
      continue;
    }
    if ((char === ',' || char === '.') && !hasSeparator) {
      hasSeparator = true;
      result += ',';
    }
  }

  return result;
}

/** Czy użytkownik wpisuje wartość w trakcie (np. "12," lub "12.") — nie parsuj jeszcze do number. */
export function isPartialNumericInput(value: string): boolean {
  const trimmed = value.trim();
  if (trimmed === '' || trimmed === '-' || trimmed === '.' || trimmed === ',') {
    return true;
  }
  return PARTIAL_NUMERIC_PATTERN.test(trimmed);
}

/** Czy użytkownik wpisuje wartość w trakcie — puste pole traktowane jako wyczyszczenie, nie wpisywanie. */
export function isInProgressNumericInput(value: string): boolean {
  const trimmed = value.trim();
  if (trimmed === '') {
    return false;
  }
  return isPartialNumericInput(trimmed);
}

/** Parsuje kompletną wartość numeryczną; `,` traktowane jak `.`, wynik zaokrąglony do 2 miejsc. */
export function parseNumericInput(value: string): number | null {
  const trimmed = value.trim();
  if (trimmed === '' || trimmed === '-' || isPartialNumericInput(trimmed)) {
    return null;
  }
  if (!VALID_NUMERIC_PATTERN.test(trimmed)) {
    return null;
  }
  const normalized = trimmed.replace(',', '.');
  const parsed = parseFloat(normalized);
  if (Number.isNaN(parsed)) {
    return null;
  }
  return roundToDecimals(parsed);
}

/** Formatuje stawkę VAT (ułamek) jako procent do wyświetlenia w polu — zawsze 2 miejsca. */
export function formatVatPercent(vatRate: number): string {
  return formatDecimalInput(roundToDecimals(vatRate * 100));
}

/** Parsuje procent VAT z pola; zwraca ułamek (0.23 dla "23,00"). */
export function parseVatPercentInput(percentText: string): number | null {
  const parsed = parseNumericInput(percentText);
  if (parsed === null) {
    return null;
  }
  return roundToDecimals(parsed / 100, 4);
}
