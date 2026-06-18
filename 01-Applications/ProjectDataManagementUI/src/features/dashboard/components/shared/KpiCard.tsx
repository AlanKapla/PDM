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
  colorScheme?: string;
  small?: boolean;
}

function resolveAccentScheme(accent?: string): string {
  if (!accent) {
    return 'primary';
  }
  if (accent.includes('.')) {
    return accent.split('.')[0];
  }
  return accent;
}

/** Kafel KPI — wariant stat (domyślny) lub embedded (wewnątrz paneli). */
export function KpiCard({
  label,
  value,
  netValue,
  grossValue,
  sub,
  accent,
  colorScheme,
  small = false,
}: KpiCardProps): React.ReactElement {
  const showAmounts = netValue !== undefined || grossValue !== undefined;
  const accentScheme = resolveAccentScheme(accent ?? colorScheme);
  const valueColor = accent ?? (colorScheme ? `${colorScheme}.700` : `${accentScheme}.600`);
  const labelColor = colorScheme ? `${colorScheme}.600` : `${accentScheme}.600`;
  const borderColor = colorScheme ? `${colorScheme}.300` : 'neutral.200';
  const bgColor = colorScheme ? `${colorScheme}.50` : 'white';

  if (small) {
    const smallBorderColor = colorScheme ? `${colorScheme}.300` : 'neutral.100';
    const smallBgColor = colorScheme ? `${colorScheme}.50` : 'neutral.50';
    const smallLabelColor = colorScheme ? `${colorScheme}.600` : 'neutral.500';
    const smallValueColor = accent ?? (colorScheme ? `${colorScheme}.700` : 'gray.800');

    return (
      <Box
        bg={smallBgColor}
        borderRadius="xl"
        borderWidth="2px"
        borderColor={smallBorderColor}
        px={3}
        py={2}
      >
        {showAmounts ? (
          <Box mb={1}>
            <NetGrossAmount
              net={netValue ?? null}
              gross={grossValue ?? null}
              size="sm"
              align="left"
              accentColor={smallValueColor}
              showLabels={false}
            />
          </Box>
        ) : (
          <Text fontSize="lg" fontWeight="bold" color={smallValueColor} lineHeight="short" mb={1}>
            {value}
          </Text>
        )}
        <Text
          fontSize="xs"
          textTransform={colorScheme ? 'uppercase' : undefined}
          letterSpacing={colorScheme ? 'wider' : undefined}
          color={smallLabelColor}
          fontWeight={colorScheme ? 'semibold' : 'normal'}
          lineHeight="shorter"
        >
          {label}
        </Text>
        {sub && (
          <Text fontSize="xs" color="neutral.400" lineHeight="shorter" mt={0.5}>
            {sub}
          </Text>
        )}
      </Box>
    );
  }

  return (
    <Box
      borderRadius="xl"
      borderWidth="2px"
      borderColor={borderColor}
      p={4}
      bg={bgColor}
    >
      {showAmounts ? (
        <Box mb={1}>
          <NetGrossAmount
            net={netValue ?? null}
            gross={grossValue ?? null}
            size="md"
            align="left"
            accentColor={valueColor}
            showLabels={false}
          />
        </Box>
      ) : (
        <Text
          fontSize="3xl"
          fontWeight="bold"
          color={valueColor}
          lineHeight={1}
          mb={1}
        >
          {value}
        </Text>
      )}
      <Text
        fontSize="xs"
        textTransform="uppercase"
        letterSpacing="wider"
        color={labelColor}
        fontWeight="semibold"
        lineHeight="short"
      >
        {label}
      </Text>
      {sub && (
        <Text fontSize="xs" color="neutral.400" lineHeight="shorter" mt={1}>
          {sub}
        </Text>
      )}
    </Box>
  );
}

export default KpiCard;
