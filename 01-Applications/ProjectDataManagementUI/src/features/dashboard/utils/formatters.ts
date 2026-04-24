import { FinancialStatus, TimelineStatus } from '../types/projectDashboard.types';
import {
  COLOR_PALETTE,
  FINANCIAL_STATUS_MAP,
  TIMELINE_STATUS_MAP,
} from './colors';
import type { StatusConfig } from './colors';

/** Formatuje kwotę PLN. null/undefined → "—" */
export function PLN(v: number | null | undefined): string {
  if (v == null) return '—';
  return (
    new Intl.NumberFormat('pl-PL', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(v) + ' zł'
  );
}

/** Formatuje procent ze znakiem. null → "—" */
export function PCT(v: number | null | undefined, showSign = true): string {
  if (v == null) return '—';
  const rounded = Math.round(v);
  if (!showSign) return `${rounded}%`;
  if (rounded > 0) return `+${rounded}%`;
  if (rounded < 0) return `${rounded}%`;
  return '0%';
}

/** Formatuje liczbę dni. null → "—" */
export function DAYS(v: number | null | undefined): string {
  if (v == null) return '—';
  return `${Math.round(v)} dni`;
}

/** Formatuje datę ISO → dd.mm.yyyy. null → "—" */
export function DATE(v: string | null | undefined): string {
  if (!v) return '—';
  try {
    return new Date(v).toLocaleDateString('pl-PL', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  } catch {
    return '—';
  }
}

/** Formatuje postęp jako %. null → "—" */
export function PROG(v: number | null | undefined): string {
  if (v == null) return '—';
  return `${Math.round(v)}%`;
}

/** Zwraca kolor dla odchylenia budżetowego.
 * Backend liczy deviationNet = budget - costs:
 *   > 0  → w budżecie            → zielony
 *   < 0  → przekroczenie budżetu → czerwony
 */
export function DEVIATION_COLOR(
  deviationNet: number | null,
  _isBudgetExceeded: boolean
): string {
  if (deviationNet != null && deviationNet < 0) {
    return COLOR_PALETTE.red400;
  }
  if (deviationNet != null && deviationNet > 0) {
    return COLOR_PALETTE.teal600;
  }
  return COLOR_PALETTE.gray400;
}

/** Zwraca konfigurację wizualną dla FinancialStatus. */
export function FINANCIAL_STATUS_CONFIG(s: FinancialStatus): StatusConfig {
  return FINANCIAL_STATUS_MAP[s] ?? FINANCIAL_STATUS_MAP[FinancialStatus.NoBudget];
}

/** Zwraca konfigurację wizualną dla TimelineStatus. */
export function TIMELINE_STATUS_CONFIG(s: TimelineStatus): StatusConfig {
  return TIMELINE_STATUS_MAP[s] ?? TIMELINE_STATUS_MAP[TimelineStatus.NoSchedule];
}
