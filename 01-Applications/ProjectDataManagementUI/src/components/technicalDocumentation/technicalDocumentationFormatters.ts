export function hasNumericValue(value: number | null | undefined): value is number {
  return value !== null && value !== undefined && !Number.isNaN(value);
}

export function formatDecimal(
  value: number | null | undefined,
  digits: number,
  fallback = '—',
): string {
  if (!hasNumericValue(value)) {
    return fallback;
  }

  return value.toFixed(digits);
}

export function formatAreaLabel(value: number | null | undefined, fallback = '—'): string {
  if (!hasNumericValue(value)) {
    return fallback;
  }

  return `${value.toFixed(2)} m²`;
}

export function formatAreaOrUndefined(value: number | null | undefined): string | undefined {
  if (!hasNumericValue(value)) {
    return undefined;
  }

  return `${value.toFixed(2)} m²`;
}

export function formatVolumeM3(value: number | null | undefined, fallback = '—'): string {
  return formatDecimal(value, 3, fallback) === fallback
    ? fallback
    : `${formatDecimal(value, 3)} m³`;
}

export function formatVolumeM3OrUndefined(value: number | null | undefined): string | undefined {
  if (!hasNumericValue(value)) {
    return undefined;
  }

  return `${value.toFixed(3)} m³`;
}
