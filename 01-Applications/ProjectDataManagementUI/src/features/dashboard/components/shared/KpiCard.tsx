import React from 'react';
import { Box, Text } from '@chakra-ui/react';
import { NetGrossAmount } from './NetGrossAmount';

export interface KpiCardProps {
  label: string;
  value?: string;
  netValue?: number | null;
  grossValue?: number | null;
  sub?: string;
  accent?: string;
  small?: boolean;
}

/** Karta KPI z etykietą, wartością i opcjonalnym podtytułem. */
export function KpiCard({
  label,
  value,
  netValue,
  grossValue,
  sub,
  accent,
  small = false,
}: KpiCardProps): React.ReactElement {
  const showAmounts = netValue !== undefined || grossValue !== undefined;

  return (
    <Box bg="neutral.25" borderRadius="md" px={3} py={small ? 2 : 3}>
      <Text fontSize="xs" color="neutral.400" lineHeight="shorter">
        {label}
      </Text>
      {showAmounts ? (
        <Box mt={0.5}>
          <NetGrossAmount
            net={netValue ?? null}
            gross={grossValue ?? null}
            size={small ? 'sm' : 'md'}
            align="left"
            accentColor={accent}
          />
        </Box>
      ) : (
        <Text
          fontSize={small ? 'md' : { base: 'lg', md: 'xl' }}
          fontWeight="semibold"
          color={accent ?? 'gray.800'}
          lineHeight="short"
          mt={0.5}
        >
          {value}
        </Text>
      )}
      {sub && (
        <Text fontSize="xs" color="neutral.400" lineHeight="shorter" mt={0.5}>
          {sub}
        </Text>
      )}
    </Box>
  );
}

export default KpiCard;
