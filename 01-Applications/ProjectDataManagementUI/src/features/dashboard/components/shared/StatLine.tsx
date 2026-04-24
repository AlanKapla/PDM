import React from 'react';
import { COLOR_PALETTE } from '../../utils/colors';

export interface StatLineProps {
  label: string;
  value: string;
  color?: string;
}

/** Wiersz etykieta: wartość — inline, fontSize 11px. */
export function StatLine({ label, value, color }: StatLineProps): React.ReactElement {
  return (
    <div style={{ fontSize: 11, lineHeight: '16px' }}>
      <span style={{ color: COLOR_PALETTE.gray400 }}>{label}: </span>
      <span style={{ color: color ?? '#1A1916', fontWeight: 500 }}>{value}</span>
    </div>
  );
}

export default StatLine;
