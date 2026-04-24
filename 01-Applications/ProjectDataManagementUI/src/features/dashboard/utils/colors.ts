export const COLOR_PALETTE = {
  purple50: '#EEEDFE', purple100: '#CECBF6', purple600: '#534AB7',
  teal50:   '#E1F5EE', teal400:   '#1D9E75', teal600:   '#0F6E56',
  coral50:  '#FAECE7', coral400:  '#D85A30', coral600:  '#993C1D',
  blue50:   '#E6F1FB', blue400:   '#378ADD', blue600:   '#185FA5',
  amber50:  '#FAEEDA', amber400:  '#BA7517', amber600:  '#854F0B',
  red50:    '#FCEBEB', red400:    '#E24B4A', red600:    '#A32D2D',
  gray50:   '#F1EFE8', gray100:   '#D3D1C7', gray400:   '#888780', gray600: '#5F5E5A',
  border:   '#E8E6DF', bg:        '#F8F7F4',
} as const;

import { FinancialStatus, TimelineStatus } from '../types/projectDashboard.types';

export type StatusConfig = { label: string; bg: string; color: string };

export const FINANCIAL_STATUS_MAP: Record<FinancialStatus, StatusConfig> = {
  [FinancialStatus.NoBudget]:   { bg: COLOR_PALETTE.gray50,  color: COLOR_PALETTE.gray600,  label: 'Brak budżetu' },
  [FinancialStatus.NoCosts]:    { bg: COLOR_PALETTE.blue50,  color: COLOR_PALETTE.blue600,  label: 'Brak kosztów' },
  [FinancialStatus.InProgress]: { bg: COLOR_PALETTE.teal50,  color: COLOR_PALETTE.teal600,  label: 'W budżecie' },
  [FinancialStatus.NearLimit]:  { bg: COLOR_PALETTE.amber50, color: COLOR_PALETTE.amber600, label: 'Blisko limitu' },
  [FinancialStatus.OverBudget]: { bg: COLOR_PALETTE.red50,   color: COLOR_PALETTE.red600,   label: 'Przekroczenie' },
};

export const TIMELINE_STATUS_MAP: Record<TimelineStatus, StatusConfig> = {
  [TimelineStatus.NoSchedule]:    { bg: COLOR_PALETTE.gray50,   color: COLOR_PALETTE.gray400,  label: 'Bez harmonogramu' },
  [TimelineStatus.NotStarted]:    { bg: COLOR_PALETTE.gray50,   color: COLOR_PALETTE.gray600,  label: 'Nie rozpoczęto' },
  [TimelineStatus.InProgress]:    { bg: COLOR_PALETTE.blue50,   color: COLOR_PALETTE.blue600,  label: 'W toku' },
  [TimelineStatus.Delayed]:       { bg: COLOR_PALETTE.coral50,  color: COLOR_PALETTE.coral600, label: 'Opóźnione' },
  [TimelineStatus.Completed]:     { bg: COLOR_PALETTE.teal50,   color: COLOR_PALETTE.teal600,  label: 'Ukończone' },
  [TimelineStatus.CompletedLate]: { bg: COLOR_PALETTE.amber50,  color: COLOR_PALETTE.amber600, label: 'Ukończone późno' },
  [TimelineStatus.NoWorkItems]:    { bg: COLOR_PALETTE.gray50,   color: COLOR_PALETTE.gray400,  label: 'Nie skonfigurowany' },
};
