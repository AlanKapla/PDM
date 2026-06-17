import { describe, expect, it } from 'vitest';
import {
  calculateGrossFromNet,
  calculateNetFromGross,
  syncCostAmounts,
  DEFAULT_COST_VAT_RATE,
} from '../costAmountCalculations';

describe('calculateGrossFromNet', () => {
  it('oblicza_brutto_z_23_procent_vat', () => {
    expect(calculateGrossFromNet(187500)).toBe(230625);
  });

  it('zaokragla_do_dwoch_miejsc_po_przecinku', () => {
    expect(calculateGrossFromNet(100)).toBe(123);
    expect(calculateGrossFromNet(10.01)).toBe(12.31);
  });

  it('uzywa_niestandardowej_stawki_vat', () => {
    expect(calculateGrossFromNet(100, 0.08)).toBe(108);
  });
});

describe('calculateNetFromGross', () => {
  it('oblicza_netto_z_23_procent_vat', () => {
    expect(calculateNetFromGross(230625)).toBe(187500);
  });

  it('zaokragla_do_dwoch_miejsc_po_przecinku', () => {
    expect(calculateNetFromGross(123)).toBe(100);
  });
});

describe('syncCostAmounts', () => {
  it('edycja_netto_uzupelnia_brutto', () => {
    expect(syncCostAmounts(100, undefined, 'net')).toEqual({ net: 100, gross: 123 });
  });

  it('edycja_brutto_uzupelnia_netto', () => {
    expect(syncCostAmounts(undefined, 123, 'gross')).toEqual({ net: 100, gross: 123 });
  });

  it('wyczyszczenie_netto_czysci_brutto', () => {
    expect(syncCostAmounts(undefined, 123, 'net')).toEqual({ net: undefined, gross: undefined });
    expect(syncCostAmounts(null, 123, 'net')).toEqual({ net: undefined, gross: undefined });
  });

  it('wyczyszczenie_brutto_czysci_netto', () => {
    expect(syncCostAmounts(100, undefined, 'gross')).toEqual({ net: undefined, gross: undefined });
    expect(syncCostAmounts(100, null, 'gross')).toEqual({ net: undefined, gross: undefined });
  });

  it('gdy_obie_kwoty_podane_nie_nadpisuje', () => {
    expect(syncCostAmounts(187500, 230625, 'net')).toEqual({ net: 187500, gross: 230625 });
    expect(syncCostAmounts(187500, 230625, 'gross')).toEqual({ net: 187500, gross: 230625 });
  });

  it('uzywa_domyslnej_stawki_vat', () => {
    expect(DEFAULT_COST_VAT_RATE).toBe(0.23);
    expect(syncCostAmounts(187500, undefined, 'net').gross).toBe(230625);
  });
});
