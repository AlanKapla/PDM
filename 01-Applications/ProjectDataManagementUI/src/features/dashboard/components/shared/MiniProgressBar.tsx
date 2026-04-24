import React from 'react';
import { COLOR_PALETTE } from '../../utils/colors';

export interface MiniProgressBarProps {
  percent: number | null;
  color: string;
  exceeded?: boolean;
  height?: number;
}

/** Minimalistyczny pasek postępu. exceeded=true wyświetla kolor czerwony. */
export function MiniProgressBar({
  percent,
  color,
  exceeded = false,
  height = 4,
}: MiniProgressBarProps): React.ReactElement {
  const clampedPercent = Math.min(100, Math.max(0, percent ?? 0));
  const barColor = exceeded ? COLOR_PALETTE.red400 : color;

  return (
    <div
      style={{
        width: '100%',
        height,
        background: COLOR_PALETTE.gray100,
        borderRadius: height,
        overflow: 'hidden',
      }}
    >
      <div
        style={{
          width: `${clampedPercent}%`,
          height: '100%',
          background: barColor,
          borderRadius: height,
          transition: 'width 0.3s ease',
        }}
      />
    </div>
  );
}

export default MiniProgressBar;
