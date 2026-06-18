import React from 'react';
import { Flex, Text, useBreakpointValue } from '@chakra-ui/react';
import { CardAmountSummary, type CardAmountSummarySize } from './CardAmountSummary';

interface CardViewGrandTotalBarProps {
  net: number;
  gross: number;
  currencySymbol: string;
}

/** Pasek „Razem” — płaski, na krawędzi kontenera (jak wiersz podsumowania w tree view). */
export function CardViewGrandTotalBar({
  net,
  gross,
  currencySymbol,
}: CardViewGrandTotalBarProps): React.ReactElement {
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
      bg="neutral.25"
      borderBottom="1px solid"
      borderColor="neutral.300"
      boxShadow="0 2px 4px rgba(20,33,47,0.06)"
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
