import { describe, expect, it } from 'vitest';
import { CostEstimateFieldType } from '../../../types/costEstimate.types.new';
import { dedupeFinancialSchemaColumns, getFinancialValueColumnKind, type ColumnDef } from './costEstimateColumnTypes';

function makeCol(overrides: Partial<ColumnDef> & Pick<ColumnDef, 'id' | 'label'>): ColumnDef {
  return {
    fieldType: 'numeric',
    appliesTo: ['item'],
    ...overrides,
  };
}

describe('getFinancialValueColumnKind', () => {
  it('recognizes basic columns by fieldKey', () => {
    expect(getFinancialValueColumnKind(makeCol({ id: 'netValue', label: 'Wartość netto', fieldKey: 'netValue' }))).toBe('net');
    expect(getFinancialValueColumnKind(makeCol({ id: 'grossValue', label: 'Wartość brutto', fieldKey: 'grossValue' }))).toBe('gross');
    expect(getFinancialValueColumnKind(makeCol({ id: 'vatValue', label: 'Wartość VAT', fieldKey: 'vatValue' }))).toBe('vat');
  });

  it('recognizes columns by schemaFieldType', () => {
    expect(
      getFinancialValueColumnKind(
        makeCol({
          id: 'legacy-uuid',
          label: 'Custom label',
          fieldKey: 'legacy-uuid',
          schemaFieldType: CostEstimateFieldType.NetValue,
          isAdditional: true,
        }),
      ),
    ).toBe('net');
  });

  it('recognizes legacy additional columns by Polish label', () => {
    expect(
      getFinancialValueColumnKind(
        makeCol({
          id: 'af-net',
          label: 'Wartość netto',
          fieldKey: 'af-net',
          isAdditional: true,
        }),
      ),
    ).toBe('net');
  });

  it('recognizes columns when schemaFieldType is a numeric string from JSON', () => {
    expect(
      getFinancialValueColumnKind(
        makeCol({
          id: 'legacy-uuid',
          label: 'Custom label',
          fieldKey: 'legacy-uuid',
          schemaFieldType: '106' as unknown as number,
          isAdditional: true,
        }),
      ),
    ).toBe('net');
  });

  it('returns null for unrelated columns', () => {
    expect(
      getFinancialValueColumnKind(
        makeCol({ id: 'quantity', label: 'Ilość', fieldKey: 'quantity' }),
      ),
    ).toBeNull();
  });
});

describe('dedupeFinancialSchemaColumns', () => {
  it('removes legacy additional duplicates when basic financial columns exist', () => {
    const columns = dedupeFinancialSchemaColumns([
      makeCol({ id: 'netValue', label: 'Wartość netto', fieldKey: 'netValue', schemaFieldType: CostEstimateFieldType.NetValue }),
      makeCol({
        id: 'legacy-net',
        label: 'Wartość netto',
        fieldKey: 'legacy-net',
        isAdditional: true,
      }),
    ]);

    expect(columns).toHaveLength(1);
    expect(columns[0]?.id).toBe('netValue');
  });
});
