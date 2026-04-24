import React from 'react';
import { COLOR_PALETTE } from '../../utils/colors';

export interface SectionLabelProps {
  text: string;
}

/** Etykieta sekcji — uppercase, 10px, gray400. */
export function SectionLabel({ text }: SectionLabelProps): React.ReactElement {
  return (
    <div
      style={{
        fontSize: 12,
        fontWeight: 600,
        color: COLOR_PALETTE.gray400,
        textTransform: 'uppercase',
        letterSpacing: '0.06em',
        margin: '16px 0 8px',
      }}
    >
      {text}
    </div>
  );
}

export default SectionLabel;
