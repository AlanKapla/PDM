import React from 'react';
import { Box, HStack, Text, Badge } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { DATE } from '../utils/formatters';
import { FinancialStatusBadge } from './shared/FinancialStatusBadge';
import { TimelineStatusBadge } from './shared/TimelineStatusBadge';
import { NetGrossAmount } from './shared/NetGrossAmount';

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
  const additionalGross = financialSummary.additionalCostsGross;

  return (
    <Box mb={5}>
      <Text fontSize={{ base: 'md', md: 'lg' }} fontWeight="semibold" color="neutral.800" mb={1}>
        {projectName}
      </Text>
      <Text fontSize="xs" color="neutral.400" mb={3}>
        Dashboard · data ref: {DATE(data.referenceDate)} · wygenerowano: {DATE(data.generatedAt)}
      </Text>
      <HStack wrap="wrap" spacing={2} gap={1} align="center">
        <Badge colorScheme="gray" px={2} py={1} borderRadius="full" fontSize="xs" fontWeight="normal" display="inline-flex" alignItems="center" gap={1}>
          <Text as="span">Budżet:</Text>
          <NetGrossAmount
            net={financialSummary.totalBudgetNet}
            gross={financialSummary.totalBudgetGross}
            size="sm"
            align="left"
          />
        </Badge>
        <Badge colorScheme="gray" px={2} py={1} borderRadius="full" fontSize="xs" fontWeight="normal" display="inline-flex" alignItems="center" gap={1}>
          <Text as="span">Koszty:</Text>
          <NetGrossAmount
            net={financialSummary.totalCostsNet}
            gross={financialSummary.totalCostsGross}
            size="sm"
            align="left"
          />
        </Badge>
        {additionalNet != null && additionalNet > 0 && (
          <Badge colorScheme="orange" px={2} py={1} borderRadius="full" fontSize="xs" fontWeight="normal" display="inline-flex" alignItems="center" gap={1}>
            <Text as="span">Dodatkowe:</Text>
            <NetGrossAmount
              net={additionalNet}
              gross={additionalGross}
              size="sm"
              align="left"
            />
          </Badge>
        )}
        <FinancialStatusBadge status={financialSummary.financialStatus} small />
        <TimelineStatusBadge status={timelineSummary.overallStatus} small />
      </HStack>
    </Box>
  );
}

export default DashboardHeader;
