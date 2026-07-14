export const DEFAULT_COST_VAT_RATE = 0.23;

function roundToTwoDecimals(value: number): number {
  return Math.round(value * 100) / 100;
}

function isValidAmount(value: number | undefined | null): value is number {
  return value !== undefined && value !== null && !Number.isNaN(value);
}

export function calculateGrossFromNet(net: number, vatRate: number = DEFAULT_COST_VAT_RATE): number {
  return roundToTwoDecimals(net * (1 + vatRate));
}

export function calculateNetFromGross(gross: number, vatRate: number = DEFAULT_COST_VAT_RATE): number {
  return roundToTwoDecimals(gross / (1 + vatRate));
}

export function syncCostAmounts(
  net: number | undefined | null,
  gross: number | undefined | null,
  lastEdited: 'net' | 'gross'
): { net: number | undefined; gross: number | undefined } {
  const hasNet = isValidAmount(net);
  const hasGross = isValidAmount(gross);

  if (lastEdited === 'net') {
    if (!hasNet) {
      return { net: undefined, gross: undefined };
    }

    if (hasGross) {
      return { net, gross };
    }

    return { net, gross: calculateGrossFromNet(net) };
  }

  if (!hasGross) {
    return { net: undefined, gross: undefined };
  }

  if (hasNet) {
    return { net, gross };
  }

  return { net: calculateNetFromGross(gross), gross };
}

export function parseAmountString(value: string): number | undefined {
  if (value.trim() === '') {
    return undefined;
  }

  const parsed = parseFloat(value);
  return Number.isNaN(parsed) ? undefined : parsed;
}

export function formatAmountString(value: number | undefined): string {
  return value !== undefined ? String(value) : '';
}
