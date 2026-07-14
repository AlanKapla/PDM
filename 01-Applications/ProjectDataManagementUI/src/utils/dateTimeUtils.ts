/**
 * Parsowanie i formatowanie dat z API (UTC) do lokalnej strefy czasowej użytkownika.
 */

export const APP_LOCALE = 'pl-PL';

const DATE_ONLY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
const HAS_TIMEZONE_SUFFIX = /(?:Z|[+-]\d{2}:\d{2})$/i;

/**
 * Parsuje wartość daty/czasu z API.
 * - DateTime bez strefy czasowej traktujemy jako UTC (backend używa DateTime.UtcNow).
 * - Same daty kalendarzowe (YYYY-MM-DD) traktujemy jako lokalną datę bez przesunięcia.
 */
export function parseApiDateTime(value: string | Date | null | undefined): Date | null {
  if (value === null || value === undefined) {
    return null;
  }

  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }

  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }

  if (DATE_ONLY_PATTERN.test(trimmed)) {
    const [year, month, day] = trimmed.split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  const normalized = HAS_TIMEZONE_SUFFIX.test(trimmed) ? trimmed : `${trimmed}Z`;
  const parsed = new Date(normalized);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

const dateTimeFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

const dateFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
});

const dateShortFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

const timeFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
  hour: '2-digit',
  minute: '2-digit',
});

const dateTimeCompactFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
});

const dateCompactFormatter = new Intl.DateTimeFormat(APP_LOCALE, {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

/** Format daty i czasu w lokalnej strefie użytkownika. */
export function formatDateTimeLocal(value: string | Date | null | undefined): string {
  const date = parseApiDateTime(value);
  if (!date) {
    return '-';
  }
  return dateTimeFormatter.format(date);
}

/** Format samej daty w lokalnej strefie użytkownika. */
export function formatDateLocal(value: string | Date | null | undefined): string {
  const date = parseApiDateTime(value);
  if (!date) {
    return '-';
  }
  return dateFormatter.format(date);
}

/** Format daty DD.MM.YYYY w lokalnej strefie użytkownika. */
export function formatDateShortLocal(value: string | Date | null | undefined): string {
  const date = parseApiDateTime(value);
  if (!date) {
    return '-';
  }
  return dateShortFormatter.format(date);
}

/** Format godziny HH:MM w lokalnej strefie użytkownika. */
export function formatTimeLocal(value: string | Date | null | undefined): string {
  const date = parseApiDateTime(value);
  if (!date) {
    return '-';
  }
  return timeFormatter.format(date);
}

/** Kompaktowy format daty i czasu (np. komentarze). */
export function formatDateTimeCompactLocal(value: string | Date | null | undefined): string {
  const date = parseApiDateTime(value);
  if (!date) {
    return '-';
  }
  return dateTimeCompactFormatter.format(date);
}

/** Kompaktowy format samej daty. */
export function formatDateCompactLocal(value: string | Date | null | undefined): string {
  const date = parseApiDateTime(value);
  if (!date) {
    return '-';
  }
  return dateCompactFormatter.format(date);
}
