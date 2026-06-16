import { describe, expect, it } from 'vitest';
import {
  formatDateTimeLocal,
  formatTimeLocal,
  parseApiDateTime,
} from '../dateTimeUtils';

describe('parseApiDateTime', () => {
  it('datetime_bez_strefy_traktuje_jako_utc', () => {
    const parsed = parseApiDateTime('2024-06-16T10:00:00');
    const expected = new Date('2024-06-16T10:00:00Z');

    expect(parsed?.getTime()).toBe(expected.getTime());
  });

  it('datetime_ze_strefa_zachowuje_instant', () => {
    const parsed = parseApiDateTime('2024-06-16T10:00:00Z');
    const expected = new Date('2024-06-16T10:00:00Z');

    expect(parsed?.getTime()).toBe(expected.getTime());
  });

  it('date_only_zwraca_lokalna_date_kalendarzowa', () => {
    const parsed = parseApiDateTime('2024-06-16');

    expect(parsed?.getFullYear()).toBe(2024);
    expect(parsed?.getMonth()).toBe(5);
    expect(parsed?.getDate()).toBe(16);
  });

  it('pusty_string_zwraca_null', () => {
    expect(parseApiDateTime('')).toBeNull();
    expect(parseApiDateTime(null)).toBeNull();
  });
});

describe('formatDateTimeLocal', () => {
  it('formatuje_datetime_z_api_w_lokalnej_strefie', () => {
    const formatted = formatDateTimeLocal('2024-06-16T10:00:00Z');
    const expected = new Intl.DateTimeFormat('pl-PL', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date('2024-06-16T10:00:00Z'));

    expect(formatted).toBe(expected);
  });
});

describe('formatTimeLocal', () => {
  it('formatuje_godzine_w_lokalnej_strefie', () => {
    const formatted = formatTimeLocal('2024-06-16T10:00:00Z');
    const expected = new Intl.DateTimeFormat('pl-PL', {
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date('2024-06-16T10:00:00Z'));

    expect(formatted).toBe(expected);
  });
});
