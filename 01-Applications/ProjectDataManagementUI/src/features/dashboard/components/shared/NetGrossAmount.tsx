import React from 'react';
import { HStack, Text, VStack } from '@chakra-ui/react';
import { PLN } from '../../utils/formatters';
import { useDashboardCurrency } from '../../context/DashboardCurrencyContext';

export type NetGrossAmountSize = 'sm' | 'md' | 'lg';

export interface NetGrossAmountProps {
  net: number | null;
  gross: number | null;
  size?: NetGrossAmountSize;
  align?: 'left' | 'right';
  accentColor?: string;
  showLabels?: boolean;
}

const SIZE_CONFIG: Record<
  NetGrossAmountSize,
  { label: string; net: string | Record<string, string>; gross: string }
> = {
  sm: { label: '2xs', net: 'sm', gross: 'xs' },
  md: { label: 'xs', net: { base: 'lg', md: 'xl' }, gross: 'sm' },
  lg: { label: 'sm', net: 'xl', gross: 'md' },
};

interface AmountRowProps {
  label: string;
  amount: string;
  labelSize: string;
  amountSize: string | Record<string, string>;
  amountColor: string;
  align: 'left' | 'right';
}

function AmountRow({
  label,
  amount,
  labelSize,
  amountSize,
  amountColor,
  align,
}: AmountRowProps): React.ReactElement {
  return (
    <HStack
      spacing={label ? 1.5 : 0}
      align="baseline"
      justify={align === 'right' ? 'flex-end' : 'flex-start'}
      w={align === 'right' ? 'full' : undefined}
    >
      {label && (
        <Text
          fontSize={labelSize}
          fontWeight="semibold"
          color="neutral.500"
          flexShrink={0}
          lineHeight="1.25"
        >
          {label}
        </Text>
      )}
      <Text
        fontWeight="semibold"
        fontSize={amountSize}
        color={amountColor}
        lineHeight="1.25"
        sx={{ fontVariantNumeric: 'tabular-nums' }}
      >
        {amount}
      </Text>
    </HStack>
  );
}

/** Wyświetla kwotę netto (większa) i brutto (mniejsza) w układzie pionowym. */
export function NetGrossAmount({
  net,
  gross,
  size = 'md',
  align = 'right',
  accentColor,
  showLabels = true,
}: NetGrossAmountProps): React.ReactElement {
  const currencySymbol = useDashboardCurrency();
  const cfg = SIZE_CONFIG[size];
  const vStackAlign = align === 'right' ? 'flex-end' : 'flex-start';
  const netLabel = showLabels ? 'Netto' : '';
  const grossLabel = showLabels ? 'Brutto' : '';

  return (
    <VStack align={vStackAlign} spacing={0}>
      <AmountRow
        label={netLabel}
        amount={PLN(net, currencySymbol)}
        labelSize={cfg.label}
        amountSize={cfg.net}
        amountColor={accentColor ?? 'gray.800'}
        align={align}
      />
      <AmountRow
        label={grossLabel}
        amount={PLN(gross, currencySymbol)}
        labelSize={cfg.label}
        amountSize={cfg.gross}
        amountColor="neutral.500"
        align={align}
      />
    </VStack>
  );
}

export default NetGrossAmount;
