import React from 'react';
import { useToken } from '@chakra-ui/react';

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
  const [red400, neutral100] = useToken('colors', ['red.400', 'neutral.100']);
  const clampedPercent = Math.min(100, Math.max(0, percent ?? 0));
  const barColor = exceeded ? red400 : color;

  return (
    <div
      style={{
        width: '100%',
        height,
        background: neutral100,
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
