import React from 'react';
import { Flex, Text, useBreakpointValue } from '@chakra-ui/react';
import { CardAmountSummary, type CardAmountSummarySize } from './CardAmountSummary';

interface CardViewGrandTotalBarProps {
  variant: 'top' | 'bottom';
  net: number;
  gross: number;
  currencySymbol: string;
}

/** Pasek „Razem” — płaski, na krawędzi kontenera (jak wiersz podsumowania w tree view). */
export function CardViewGrandTotalBar({
  variant,
  net,
  gross,
  currencySymbol,
}: CardViewGrandTotalBarProps): React.ReactElement {
  const isTop: boolean = variant === 'top';
  const bg: string = isTop ? 'neutral.25' : 'neutral.50';
  const summarySize = useBreakpointValue<CardAmountSummarySize>({ base: 'md', md: 'lg' }) ?? 'md';

  return (
    <Flex
      flexShrink={0}
      align="center"
      justify="space-between"
      gap={3}
      px={{ base: 3, md: 4 }}
      py={3}
      minH="52px"
      bg={bg}
      borderTop={isTop ? undefined : '2px solid'}
      borderBottom={isTop ? '1px solid' : undefined}
      borderColor="neutral.300"
      boxShadow={
        isTop
          ? '0 2px 4px rgba(20,33,47,0.06)'
          : '0 -4px 6px -1px rgba(20,33,47,0.08)'
      }
    >
      <Text
        fontSize="xs"
        fontWeight="bold"
        color="neutral.700"
        textTransform="uppercase"
        letterSpacing="0.05em"
        flexShrink={0}
      >
        Razem
      </Text>

      <CardAmountSummary
        net={net}
        gross={gross}
        currencySymbol={currencySymbol}
        size={summarySize}
        layout="stacked"
      />
    </Flex>
  );
}
