import { describe, expect, it } from 'vitest';
import {
  formatDecimalInput,
  formatVatPercent,
  isPartialNumericInput,
  parseNumericInput,
  parseVatPercentInput,
  roundToDecimals,
  sanitizeNumericInput,
} from './numericInputUtils';

describe('numericInputUtils', () => {
  it('sanitizeNumericInput allows comma and limits decimal places', () => {
    expect(sanitizeNumericInput('12,5')).toBe('12,5');
    expect(sanitizeNumericInput('12.5.6')).toBe('12,56');
    expect(sanitizeNumericInput('12,555')).toBe('12,55');
  });

  it('isPartialNumericInput detects incomplete decimals', () => {
    expect(isPartialNumericInput('12,')).toBe(true);
    expect(isPartialNumericInput('12.')).toBe(true);
    expect(isPartialNumericInput('12,5')).toBe(false);
  });

  it('parseNumericInput parses and rounds to 2 decimals', () => {
    expect(parseNumericInput('12,5')).toBe(12.5);
    expect(parseNumericInput('12,555')).toBe(12.56);
    expect(parseNumericInput('12,')).toBeNull();
  });

  it('formatDecimalInput always shows 2 decimal places', () => {
    expect(formatDecimalInput(1)).toBe('1,00');
    expect(formatDecimalInput(12.5)).toBe('12,50');
    expect(formatDecimalInput(12.556)).toBe('12,56');
  });

  it('parseVatPercentInput converts percent to fraction', () => {
    expect(parseVatPercentInput('23')).toBe(0.23);
    expect(parseVatPercentInput('23,5')).toBe(0.235);
  });

  it('formatVatPercent displays stored fraction with 2 decimals', () => {
    expect(formatVatPercent(0.235)).toBe('23,50');
  });

  it('roundToDecimals rounds correctly', () => {
    expect(roundToDecimals(1.005)).toBe(1.01);
  });
});
