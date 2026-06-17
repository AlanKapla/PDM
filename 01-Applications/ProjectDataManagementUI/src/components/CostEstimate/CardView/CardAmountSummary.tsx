import React from 'react';
import { Box, HStack, Text, VStack } from '@chakra-ui/react';
import { formatCurrency } from '../../../utils/formatters';

export type CardAmountSummarySize = 'sm' | 'md' | 'lg';

interface CardAmountSummaryProps {
  net: number;
  gross: number;
  currencySymbol: string;
  size?: CardAmountSummarySize;
  layout?: 'columns' | 'stacked';
}

const SIZE_CONFIG: Record<
  CardAmountSummarySize,
  { label: string; net: string; gross: string; gap: number; colMinW: string }
> = {
  sm: { label: '2xs', net: 'sm', gross: 'xs', gap: 3, colMinW: '68px' },
  md: { label: '2xs', net: 'sm', gross: 'xs', gap: 4, colMinW: '76px' },
  lg: { label: 'xs', net: 'md', gross: 'sm', gap: 5.5, colMinW: '84px' },
};

export function CardAmountSummary({
  net,
  gross,
  currencySymbol,
  size = 'md',
  layout = 'columns',
}: CardAmountSummaryProps): React.ReactElement {
  const cfg = SIZE_CONFIG[size];
  const netLabel = formatCurrency(net, currencySymbol);
  const grossLabel = formatCurrency(gross, currencySymbol);

  if (layout === 'stacked') {
    return (
      <VStack align="flex-end" spacing={0.5} flexShrink={0} minW={cfg.colMinW}>
        <HStack spacing={2} justify="flex-end">
          <Text
            fontSize={cfg.label}
            fontWeight="semibold"
            color="neutral.500"
            textTransform="uppercase"
            letterSpacing="0.04em"
            flexShrink={0}
          >
            Netto
          </Text>
          <Text
            fontWeight="bold"
            fontSize={cfg.net}
            lineHeight="1.25"
            textAlign="right"
            sx={{ fontVariantNumeric: 'tabular-nums' }}
          >
            {netLabel}
          </Text>
        </HStack>
        <HStack spacing={2} justify="flex-end">
          <Text
            fontSize={cfg.label}
            fontWeight="semibold"
            color="neutral.500"
            textTransform="uppercase"
            letterSpacing="0.04em"
            flexShrink={0}
          >
            Brutto
          </Text>
          <Text
            fontWeight="medium"
            fontSize={cfg.gross}
            color="neutral.600"
            lineHeight="1.25"
            textAlign="right"
            sx={{ fontVariantNumeric: 'tabular-nums' }}
          >
            {grossLabel}
          </Text>
        </HStack>
      </VStack>
    );
  }

  return (
    <HStack spacing={cfg.gap} flexShrink={0} textAlign="right">
      <Box minW={cfg.colMinW}>
        <Text
          fontSize={cfg.label}
          fontWeight="semibold"
          color="neutral.500"
          textTransform="uppercase"
          letterSpacing="0.04em"
        >
          Netto
        </Text>
        <Text
          fontWeight="bold"
          fontSize={cfg.net}
          sx={{ fontVariantNumeric: 'tabular-nums' }}
        >
          {netLabel}
        </Text>
      </Box>
      <Box minW={cfg.colMinW}>
        <Text
          fontSize={cfg.label}
          fontWeight="semibold"
          color="neutral.500"
          textTransform="uppercase"
          letterSpacing="0.04em"
        >
          Brutto
        </Text>
        <Text
          fontWeight="semibold"
          fontSize={cfg.gross}
          color="neutral.600"
          sx={{ fontVariantNumeric: 'tabular-nums' }}
        >
          {grossLabel}
        </Text>
      </Box>
    </HStack>
  );
}

interface CardRowAsideProps {
  children: React.ReactNode;
}

export function CardRowAside({ children }: CardRowAsideProps): React.ReactElement {
  return (
    <HStack
      spacing={3}
      flexShrink={0}
      pl={3}
      ml={1}
      borderLeft="1px solid"
      borderColor="neutral.200"
      align="center"
      onClick={(e) => e.stopPropagation()}
    >
      {children}
    </HStack>
  );
}

interface CardRowDividerProps {
  height?: string;
}

export function CardRowDivider({ height = '28px' }: CardRowDividerProps): React.ReactElement {
  return (
    <Box
      w="1px"
      h={height}
      bg="neutral.200"
      flexShrink={0}
      aria-hidden="true"
    />
  );
}
