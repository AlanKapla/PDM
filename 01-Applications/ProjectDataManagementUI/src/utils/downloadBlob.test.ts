import { describe, expect, it } from 'vitest';
import {
  parseContentDispositionFileName,
  sanitizeDownloadFileName,
} from './downloadBlob';

describe('parseContentDispositionFileName', () => {
  it('parsuje filename w cudzysłowach', () => {
    const result: string | null = parseContentDispositionFileName(
      'attachment; filename="Kosztorys_20260721.xlsx"'
    );
    expect(result).toBe('Kosztorys_20260721.xlsx');
  });

  it('parsuje filename bez cudzysłowów', () => {
    const result: string | null = parseContentDispositionFileName(
      'attachment; filename=report.pdf'
    );
    expect(result).toBe('report.pdf');
  });

  it('parsuje filename* UTF-8', () => {
    const result: string | null = parseContentDispositionFileName(
      "attachment; filename*=UTF-8''Kosztorys%20ABC.pdf"
    );
    expect(result).toBe('Kosztorys ABC.pdf');
  });

  it('zwraca null dla pustego nagłówka', () => {
    expect(parseContentDispositionFileName(undefined)).toBeNull();
    expect(parseContentDispositionFileName('')).toBeNull();
  });
});

describe('sanitizeDownloadFileName', () => {
  it('usuwa niedozwolone znaki i rozszerzenie eksportu', () => {
    expect(sanitizeDownloadFileName('A/B:C*.xlsx')).toBe('A_B_C_');
  });

  it('zwraca fallback dla pustej nazwy', () => {
    expect(sanitizeDownloadFileName('   ')).toBe('kosztorys');
  });
});
