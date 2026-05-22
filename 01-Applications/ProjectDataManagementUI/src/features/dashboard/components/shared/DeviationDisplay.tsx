import React from 'react';
import { useToken } from '@chakra-ui/react';
import { PLN, PCT, DEVIATION_COLOR } from '../../utils/formatters';
import { useDashboardCurrency } from '../../context/DashboardCurrencyContext';

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
  const [neutral400] = useToken('colors', ['neutral.400']);
  const currencySymbol = useDashboardCurrency();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
      <span style={{ fontSize: "sm", fontWeight: "medium", color }}>
        {PLN(deviationNet, currencySymbol)}
      </span>
      <span style={{ fontSize: "xs", color: neutral400 }}>
        {PCT(deviationPercent)}
      </span>
    </div>
  );
}

export default DeviationDisplay;
