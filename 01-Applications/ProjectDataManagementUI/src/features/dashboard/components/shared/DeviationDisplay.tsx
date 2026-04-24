import React from 'react';
import { PLN, PCT, DEVIATION_COLOR } from '../../utils/formatters';
import { COLOR_PALETTE } from '../../utils/colors';

export interface DeviationDisplayProps {
  deviationNet: number | null;
  deviationPercent: number | null;
  isBudgetExceeded: boolean;
}

/** Wyświetla odchylenie budżetowe (kwota + procent). Ujemna wartość = przekroczenie (kolor czerwony). */
export function DeviationDisplay({
  deviationNet,
  deviationPercent,
  isBudgetExceeded,
}: DeviationDisplayProps): React.ReactElement {
  const color = DEVIATION_COLOR(deviationNet, isBudgetExceeded);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
      <span style={{ fontSize: 13, fontWeight: 500, color }}>
        {PLN(deviationNet)}
      </span>
      <span style={{ fontSize: 12, color: COLOR_PALETTE.gray400 }}>
        {PCT(deviationPercent)}
      </span>
    </div>
  );
}

export default DeviationDisplay;
