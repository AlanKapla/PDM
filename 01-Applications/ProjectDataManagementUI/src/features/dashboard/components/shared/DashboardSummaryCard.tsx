import React from 'react';
import { Box, Text } from '@chakra-ui/react';

export interface DashboardSummaryCardProps {
  label: string;
  children: React.ReactNode;
  accentColor?: string;
}

/** Kafel KPI w nagłówku dashboardu — spójny ze stat tiles na „Zaplanowanych pracach”. */
export function DashboardSummaryCard({
  label,
  children,
  accentColor = 'primary.500',
}: DashboardSummaryCardProps): React.ReactElement {
  return (
    <Box
      borderRadius="xl"
      borderWidth="2px"
      borderColor="neutral.200"
      borderLeftWidth="4px"
      borderLeftColor={accentColor}
      p={4}
      bg="white"
      minH="108px"
      display="flex"
      flexDirection="column"
      justifyContent="center"
    >
      <Text
        fontSize="xs"
        textTransform="uppercase"
        letterSpacing="wider"
        color="neutral.600"
        fontWeight="semibold"
        mb={2}
        lineHeight="short"
      >
        {label}
      </Text>
      {children}
    </Box>
  );
}

export default DashboardSummaryCard;
