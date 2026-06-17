import React from 'react';
import { Text, VStack } from '@chakra-ui/react';
import { PCT, DEVIATION_COLOR } from '../../utils/formatters';
import { NetGrossAmount } from './NetGrossAmount';

export interface DeviationDisplayProps {
  deviationNet: number | null;
  deviationGross?: number | null;
  deviationPercent: number | null;
  isBudgetExceeded: boolean;
}

/** Wyświetla odchylenie budżetowe (netto + brutto + procent). Ujemna wartość = przekroczenie (kolor czerwony). */
export function DeviationDisplay({
  deviationNet,
  deviationGross,
  deviationPercent,
  isBudgetExceeded,
}: DeviationDisplayProps): React.ReactElement {
  const color = DEVIATION_COLOR(deviationNet, isBudgetExceeded);

  return (
    <VStack align="flex-end" spacing={0}>
      <NetGrossAmount
        net={deviationNet}
        gross={deviationGross ?? null}
        size="sm"
        align="right"
        accentColor={color}
      />
      <Text fontSize="xs" color="neutral.400" lineHeight="1.25">
        {PCT(deviationPercent)}
      </Text>
    </VStack>
  );
}

export default DeviationDisplay;
