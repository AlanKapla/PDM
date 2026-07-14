import type { ValueType } from 'recharts/types/component/DefaultTooltipContent';

function toNumber(value: ValueType | undefined): number {
  if (typeof value === 'number') {
    return value;
  }
  return Number(value) || 0;
}

/** Bezpieczny formatter dla recharts Tooltip — obsługuje undefined ValueType. */
export function chartTooltipAmount(
  formatter: (value: number) => string
): (value: ValueType | undefined) => string {
  return (value: ValueType | undefined) => formatter(toNumber(value));
}

export function chartTooltipPercent(value: ValueType | undefined): string {
  return `${Math.round(toNumber(value))}%`;
}
