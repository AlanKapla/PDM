import { describe, expect, it } from 'vitest';
import {
  formatAreaLabel,
  formatAreaOrUndefined,
  formatDecimal,
  hasNumericValue,
} from '../technicalDocumentationFormatters';

describe('technicalDocumentationFormatters', () => {
  it('hasNumericValue_rejectsNull', () => {
    expect(hasNumericValue(null)).toBe(false);
    expect(hasNumericValue(undefined)).toBe(false);
    expect(hasNumericValue(12.5)).toBe(true);
  });

  it('formatAreaLabel_handlesNull', () => {
    expect(formatAreaLabel(null)).toBe('—');
    expect(formatAreaLabel(42)).toBe('42.00 m²');
  });

  it('formatAreaOrUndefined_handlesNull', () => {
    expect(formatAreaOrUndefined(null)).toBeUndefined();
    expect(formatAreaOrUndefined(10)).toBe('10.00 m²');
  });

  it('formatDecimal_handlesNull', () => {
    expect(formatDecimal(null, 2)).toBe('—');
    expect(formatDecimal(1.234, 2)).toBe('1.23');
  });
});
