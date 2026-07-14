import React from 'react';
import { Box, Text } from '@chakra-ui/react';

export interface ChartCardProps {
  title: string;
  children: React.ReactNode;
  emptyMessage?: string;
  isEmpty?: boolean;
  footer?: React.ReactNode;
  ariaLabel: string;
  fullWidth?: boolean;
}

export function ChartCard({
  title,
  children,
  emptyMessage = 'Brak danych do wyświetlenia',
  isEmpty = false,
  footer,
  ariaLabel,
  fullWidth = false,
}: ChartCardProps): React.ReactElement {
  return (
    <Box
      className={fullWidth ? 'dashboard-chart-card dashboard-chart-card--full' : 'dashboard-chart-card'}
      bg="white"
      borderWidth="2px"
      borderColor="neutral.200"
      borderRadius="xl"
      p={4}
      role="img"
      aria-label={ariaLabel}
    >
      <Text fontSize="sm" fontWeight="medium" color="neutral.800" mb={3}>
        {title}
      </Text>
      {isEmpty ? (
        <Text fontSize="sm" color="neutral.400" fontStyle="italic" py={8} textAlign="center">
          {emptyMessage}
        </Text>
      ) : (
        children
      )}
      {footer}
    </Box>
  );
}

export default ChartCard;
