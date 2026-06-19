import { describe, expect, it } from 'vitest';
import type { CostEstimateItemWeb } from '../types/costEstimate.types.new';
import { deriveItemFinancialState, resolveCalculationQuantity } from './costEstimateItemFinancial';

describe('costEstimateItemFinancial', () => {
  it('resolveCalculationQuantity defaults to 1 when price is set', () => {
    expect(resolveCalculationQuantity({ unitPriceNet: 100 })).toBe(1);
    expect(resolveCalculationQuantity({ quantity: 3, unitPriceNet: 100 })).toBe(3);
    expect(resolveCalculationQuantity({ quantity: null, unitPriceNet: 100 })).toBeUndefined();
    expect(resolveCalculationQuantity({})).toBeUndefined();
  });

  it('deriveItemFinancialState keeps net/vat/gross empty when quantity is cleared', () => {
    const derived = deriveItemFinancialState({
      quantity: null,
      unitPriceNet: 100,
      vatRate: 0.23,
      netValue: 100,
      vatValue: 23,
      grossValue: 123,
    } as CostEstimateItemWeb);

    expect(derived.netValue).toBeUndefined();
    expect(derived.vatValue).toBeUndefined();
    expect(derived.grossValue).toBeUndefined();
  });

  it('deriveItemFinancialState computes net/vat/gross from unit price and default quantity', () => {
    const derived = deriveItemFinancialState({
      unitPriceNet: 100,
      vatRate: 0.23,
    } as CostEstimateItemWeb);

    expect(derived.netValue).toBe(100);
    expect(derived.vatValue).toBe(23);
    expect(derived.grossValue).toBe(123);
    expect(derived.unitPriceGross).toBe(123);
  });

  it('deriveItemFinancialState aggregates selected components', () => {
    const derived = deriveItemFinancialState({
      components: [
        {
          unitPriceNet: 100,
          quantity: 1,
          vatRate: 0.23,
          isSelected: true,
        } as CostEstimateItemWeb,
        {
          unitPriceNet: 50,
          quantity: 2,
          vatRate: 0.23,
          isSelected: true,
        } as CostEstimateItemWeb,
      ],
    } as CostEstimateItemWeb);

    expect(derived.netValue).toBe(200);
    expect(derived.vatValue).toBe(46);
    expect(derived.grossValue).toBe(246);
  });

  it('deriveItemFinancialState uses stored totals when locked by children without data', () => {
    const derived = deriveItemFinancialState(
      {
        unitPriceNet: 50,
        quantity: 1,
        netValue: 200,
        vatValue: 46,
        grossValue: 246,
        components: [],
      } as unknown as CostEstimateItemWeb,
      true,
    );

    expect(derived.netValue).toBe(200);
    expect(derived.vatValue).toBe(46);
    expect(derived.grossValue).toBe(246);
  });
});
