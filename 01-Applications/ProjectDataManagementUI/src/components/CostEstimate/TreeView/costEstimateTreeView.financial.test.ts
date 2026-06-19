import { describe, expect, it } from 'vitest';
import { BASE_COLUMNS } from './CostEstimateTreeView';
import { getFinancialValueColumnKind } from './costEstimateColumnTypes';
import { buildMockFieldSchemas } from '../../../api/mock/costEstimateMockData';
import { resolveTreeViewSchemaColumns } from '../../../utils/costEstimateFieldSchema';
import { deriveItemFinancialState } from '../../../utils/costEstimateItemFinancial';
import type { CostEstimateItemWeb } from '../../../types/costEstimate.types.new';

describe('tree view financial display integration', () => {
  const schemas = buildMockFieldSchemas('ce-test', '2025-01-01T00:00:00Z');
  const columns = resolveTreeViewSchemaColumns({ fieldSchemas: schemas }, BASE_COLUMNS);

  const baseFinancialCols = columns.filter(
    (col) => !col.isAdditional && getFinancialValueColumnKind(col) !== null,
  );

  it('schema includes basic financial columns', () => {
    expect(baseFinancialCols.map((col) => col.fieldKey)).toEqual(
      expect.arrayContaining(['netValue', 'grossValue', 'vatValue']),
    );
  });

  it('deriveItemFinancialState returns values for typical mock position', () => {
    const item = {
      relationType: 0,
      quantity: 2850,
      unitPriceNet: 95.5,
      vatRate: 0.23,
    } as CostEstimateItemWeb;

    const derived = deriveItemFinancialState(item);
    expect(derived.netValue).toBe(272_175);
    expect(derived.vatValue).toBe(62_600.25);
    expect(derived.grossValue).toBe(334_789.5);
  });

  it('deriveItemFinancialState defaults quantity to 1 when only unit price is set', () => {
    const derived = deriveItemFinancialState({
      relationType: 0,
      unitPriceNet: 100,
      vatRate: 0.23,
    } as CostEstimateItemWeb);

    expect(derived.netValue).toBe(100);
    expect(derived.grossValue).toBe(123);
  });
});
