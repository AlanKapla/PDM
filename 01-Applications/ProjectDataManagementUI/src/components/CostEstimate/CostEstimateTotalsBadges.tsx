import React, { useMemo } from 'react';
import { HStack, Text } from '@chakra-ui/react';
import type { CostEstimateDetailsWeb } from '../../types/costEstimate.types.new';
import { getCostEstimateTotals, resolveCostEstimateCurrencySymbol } from '../../utils/costEstimateUtils';
import { formatCurrency } from '../../utils/formatters';

export interface CostEstimateTotalsBadgesProps {
  details: CostEstimateDetailsWeb;
}

interface TotalBadgeProps {
  label: string;
  value: string;
}

function TotalBadge({ label, value }: TotalBadgeProps): React.ReactElement {
  return (
    <HStack
      spacing={2}
      bg="white"
      border="1px solid"
      borderColor="neutral.200"
      borderRadius="11px"
      px={3.5}
      py={2}
      boxShadow="0 1px 2px rgba(20,33,47,.05)"
    >
      <Text
        fontSize="xs"
        fontWeight="bold"
        color="neutral.500"
        textTransform="uppercase"
        letterSpacing="0.04em"
      >
        {label}
      </Text>
      <Text
        fontSize="sm"
        fontWeight="bold"
        color="neutral.800"
        sx={{ fontVariantNumeric: 'tabular-nums' }}
      >
        {value}
      </Text>
    </HStack>
  );
}

export function CostEstimateTotalsBadges({
  details,
}: CostEstimateTotalsBadgesProps): React.ReactElement {
  const totals = useMemo(() => getCostEstimateTotals(details), [details]);
  const currency = resolveCostEstimateCurrencySymbol(details);

  return (
    <HStack spacing={{ base: 2, md: 3 }} mb={4} flexWrap="wrap">
      <TotalBadge label="Netto" value={formatCurrency(totals.net, currency)} />
      <TotalBadge label="Brutto" value={formatCurrency(totals.gross, currency)} />
    </HStack>
  );
}
