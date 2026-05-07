import { FinancialStatus, TimelineStatus } from '../types/projectDashboard.types';

export interface StatusConfig {
  bg: string;
  color: string;
  label: string;
}

export const FINANCIAL_STATUS_MAP: Record<FinancialStatus, StatusConfig> = {
  [FinancialStatus.NoBudget]:   { bg: '#F1EFE8', color: '#5F5E5A', label: 'Brak budżetu' },    // neutral.50 / neutral.600
  [FinancialStatus.NoCosts]:    { bg: '#EBF8FF', color: '#2B6CB0', label: 'Brak kosztów' },   // primary.50 / primary.600
  [FinancialStatus.InProgress]: { bg: '#E6FFFA', color: '#276749', label: 'W budżecie' },     // action.50  / level1.700
  [FinancialStatus.NearLimit]:  { bg: '#FAEEDA', color: '#854F0B', label: 'Blisko limitu' },  // amber.50   / amber.600
  [FinancialStatus.OverBudget]: { bg: '#FFF5F5', color: '#C53030', label: 'Przekroczenie' }, // red.50     / red.600
};

export const TIMELINE_STATUS_MAP: Record<TimelineStatus, StatusConfig> = {
  [TimelineStatus.NoSchedule]:    { bg: '#F1EFE8', color: '#888780', label: 'Bez harmonogramu' },    // neutral.50 / neutral.400
  [TimelineStatus.NotStarted]:    { bg: '#F1EFE8', color: '#5F5E5A', label: 'Nie rozpoczęto' },      // neutral.50 / neutral.600
  [TimelineStatus.InProgress]:    { bg: '#EBF8FF', color: '#2B6CB0', label: 'W toku' },              // primary.50 / primary.600
  [TimelineStatus.Delayed]:       { bg: '#FFFAF0', color: '#652B19', label: 'Opóźnione' },          // orange.50  / orange.800
  [TimelineStatus.Completed]:     { bg: '#E6FFFA', color: '#276749', label: 'Ukończone' },          // action.50  / level1.700
  [TimelineStatus.CompletedLate]: { bg: '#FAEEDA', color: '#854F0B', label: 'Ukończone późno' },   // amber.50   / amber.600
  [TimelineStatus.NoWorkItems]:   { bg: '#F1EFE8', color: '#888780', label: 'Nie skonfigurowany' },  // neutral.50 / neutral.400
};

/** Formatuje kwotę pieniężną z dynamicznym symbolem waluty. null/undefined → "—" */
export function PLN(
  v: number | null | undefined,
  currencySymbol: string = 'zł'
): string {
  if (v == null) return '—';
  return (
    new Intl.NumberFormat('pl-PL', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(v) + ' ' + currencySymbol
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
    return '#FC8181'; // red.400
  }
  if (deviationNet != null && deviationNet > 0) {
    return '#276749'; // level1.700
  }
  return '#888780'; // neutral.400
}

/** Zwraca konfigurację wizualną dla FinancialStatus. */
export function FINANCIAL_STATUS_CONFIG(s: FinancialStatus): StatusConfig {
  return FINANCIAL_STATUS_MAP[s] ?? FINANCIAL_STATUS_MAP[FinancialStatus.NoBudget];
}

/** Zwraca konfigurację wizualną dla TimelineStatus. */
export function TIMELINE_STATUS_CONFIG(s: TimelineStatus): StatusConfig {
  return TIMELINE_STATUS_MAP[s] ?? TIMELINE_STATUS_MAP[TimelineStatus.NoSchedule];
}
