import React from 'react';
import { Box, HStack, Text, Badge } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { PLN, DATE } from '../utils/formatters';
import { FinancialStatusBadge } from './shared/FinancialStatusBadge';
import { TimelineStatusBadge } from './shared/TimelineStatusBadge';

export interface DashboardHeaderProps {
  data: ProjectDashboardWeb;
  projectName: string;
}

/**
 * Nagłówek dashboardu z nazwą projektu, datą referencyjną i podsumowaniem budżetu.
 * Źródło danych: ProjectDashboardWeb.
 */
export function DashboardHeader({ data, projectName }: DashboardHeaderProps): React.ReactElement {
  const { financialSummary, timelineSummary } = data;
  const additionalNet = financialSummary.additionalCostsNet;

  return (
    <Box mb={5}>
      <Text fontSize={{ base: 'md', md: 'lg' }} fontWeight="semibold" color="neutral.800" mb={1}>
        {projectName}
      </Text>
      <Text fontSize="xs" color="neutral.400" mb={3}>
        Dashboard · data ref: {DATE(data.referenceDate)} · wygenerowano: {DATE(data.generatedAt)}
      </Text>
      <HStack wrap="wrap" spacing={2} gap={1} align="center">
        <Badge colorScheme="gray" px={2} py={1} borderRadius="full" fontSize="xs" fontWeight="normal">
          Budżet: <strong>{PLN(financialSummary.totalBudgetNet)}</strong>
        </Badge>
        <Badge colorScheme="gray" px={2} py={1} borderRadius="full" fontSize="xs" fontWeight="normal">
          Koszty: <strong>{PLN(financialSummary.totalCostsNet)}</strong>
        </Badge>
        {additionalNet != null && additionalNet > 0 && (
          <Badge colorScheme="orange" px={2} py={1} borderRadius="full" fontSize="xs" fontWeight="normal">
            Dodatkowe: <strong>{PLN(additionalNet)}</strong>
          </Badge>
        )}
        <FinancialStatusBadge status={financialSummary.financialStatus} small />
        <TimelineStatusBadge status={timelineSummary.overallStatus} small />
      </HStack>
    </Box>
  );
}

export default DashboardHeader;
